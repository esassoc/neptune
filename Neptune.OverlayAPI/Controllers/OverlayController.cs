using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.OverlayAPI.Services;
using Neptune.OverlayAPI.Services.Overlay;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.OverlayAPI.Controllers;

[ApiController]
public class OverlayController : ControllerBase
{
    private readonly ILogger<OverlayController> _logger;
    private readonly NeptuneDbContext _dbContext;
    private readonly OverlayAPIConfiguration _configuration;

    public OverlayController(ILogger<OverlayController> logger, NeptuneDbContext dbContext,
        IOptions<OverlayAPIConfiguration> configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration.Value;
    }

    [HttpGet("/")]
    public ActionResult Get()
    {
        return Ok("Hello from the Overlay API!");
    }

    // The in-process overlay is fast enough that direct/manual endpoint calls can realistically overlap
    // (Hangfire's single worker only serializes the job path). Overlapping runs deadlock on the
    // TRUNCATE + constraint-drop delete procs — observed locally — so one service-wide gate serializes
    // all three overlay endpoints; a second caller gets 409 instead of corrupting the target tables.
    private static readonly SemaphoreSlim OverlayGate = new(1, 1);

    private async Task<IActionResult> RunGated(Func<Task<IActionResult>> overlayAction)
    {
        if (!await OverlayGate.WaitAsync(TimeSpan.Zero))
        {
            return Conflict("Another overlay run is already in progress; retry after it completes.");
        }
        try
        {
            return await overlayAction();
        }
        finally
        {
            OverlayGate.Release();
        }
    }

    [HttpPost("overlay/generate-plgus")]
    public Task<IActionResult> GenerateProjectLoadGeneratingUnits([FromBody] GenerateProjectLoadGeneratingUnitRequestDto requestDto)
        => RunGated(() => GenerateProjectLoadGeneratingUnitsImpl(requestDto));

    [HttpPost("overlay/generate-lgus")]
    public Task<IActionResult> GenerateLoadGeneratingUnits([FromBody] GenerateLoadGeneratingUnitRequestDto requestDto)
        => RunGated(() => GenerateLoadGeneratingUnitsImpl(requestDto));

    [HttpPost("overlay/generate-tgus")]
    public Task<IActionResult> GenerateTrashGeneratingUnits([FromBody] GenerateTrashGeneratingUnitRequestDto requestDto)
        => RunGated(() => GenerateTrashGeneratingUnitsImpl(requestDto));

    private async Task<IActionResult> GenerateProjectLoadGeneratingUnitsImpl(GenerateProjectLoadGeneratingUnitRequestDto requestDto)
    {
        var projectID = requestDto.ProjectID;
        var project = _dbContext.Projects.AsNoTracking().SingleOrDefault(x => x.ProjectID == projectID);
        if (project == null)
        {
            return NotFound($"Project with ID {projectID} does not exist!");
        }

        var inputs = await LoadGeneratingUnitOverlay.LoadProjectInputsAsync(_dbContext, projectID, requestDto.RegionalSubbasinIDs);
        var pieces = LoadGeneratingUnitOverlay.Run(inputs);

        var projectLoadGeneratingUnits = new List<ProjectLoadGeneratingUnit>();
        foreach (var (feature, polygon) in LoadGeneratingUnitOverlay.ToUnits(pieces))
        {
            projectLoadGeneratingUnits.Add(new ProjectLoadGeneratingUnit
            {
                ProjectLoadGeneratingUnitGeometry = polygon,
                ProjectID = projectID,
                DelineationID = feature.DelineationID,
                WaterQualityManagementPlanID = feature.WaterQualityManagementPlanID,
                ModelBasinID = feature.ModelBasinID,
                RegionalSubbasinID = feature.RegionalSubbasinID
            });
        }

        await _dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pDeleteProjectLoadGeneratingUnitsPriorToRefreshForProject @ProjectID = {projectID}");

        if (projectLoadGeneratingUnits.Any())
        {
            await _dbContext.ProjectLoadGeneratingUnits.AddRangeAsync(projectLoadGeneratingUnits);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pProjectLoadGeneratingUnitMakeValid");
        }

        return Ok();
    }

    private async Task<IActionResult> GenerateLoadGeneratingUnitsImpl(GenerateLoadGeneratingUnitRequestDto requestDto)
    {
        var loadGeneratingUnitRefreshArea = await GetLoadGeneratingUnitRefreshAreaIfProvided(requestDto);

        var inputs = await LoadGeneratingUnitOverlay.LoadInputsAsync(_dbContext);

        var refreshAreaGeometry = loadGeneratingUnitRefreshArea?.LoadGeneratingUnitRefreshAreaGeometry;
        var pieces = LoadGeneratingUnitOverlay.Run(inputs,
            refreshAreaGeometry != null ? OverlayEngine.DeltaRefreshCandidateZone(refreshAreaGeometry) : null);

        IEnumerable<(OverlayFeature Feature, Polygon Polygon)> units;
        if (refreshAreaGeometry != null)
        {
            units = LoadGeneratingUnitOverlay.InARegionalSubbasin(await DeleteLoadGeneratingUnitsForDeltaRefresh(refreshAreaGeometry, pieces));
        }
        else
        {
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteLoadGeneratingUnitsPriorToTotalRefresh");
            units = LoadGeneratingUnitOverlay.ToUnits(pieces);
        }

        var loadGeneratingUnits = new List<LoadGeneratingUnit>();
        var exportFeatures = _configuration.OverlayDebugExportFolder != null ? new FeatureCollection() : null;
        foreach (var (feature, polygon) in units)
        {
            loadGeneratingUnits.Add(new LoadGeneratingUnit
            {
                LoadGeneratingUnitGeometry = polygon,
                DelineationID = feature.DelineationID,
                WaterQualityManagementPlanID = feature.WaterQualityManagementPlanID,
                ModelBasinID = feature.ModelBasinID,
                RegionalSubbasinID = feature.RegionalSubbasinID,
                LoadGeneratingUnitGeometry4326 = polygon.ProjectTo4326(),
            });
            exportFeatures?.Add(new Feature(polygon, new AttributesTable
            {
                { "DelinID", feature.DelineationID }, { "WQMPID", feature.WaterQualityManagementPlanID },
                { "ModelID", feature.ModelBasinID }, { "RSBID", feature.RegionalSubbasinID },
            }));
        }

        // NPT-981: the delineation input set is snapshotted at the top of this action; a user can delete a
        // delineation while the overlay runs. That would leave an LGU pointing at a now-deleted DelineationID
        // and trip FK_LoadGeneratingUnit_Delineation_DelineationID. Drop any orphaned rows just before the
        // insert (a null DelineationID is valid — those are regional-subbasin-only LGUs). The run takes
        // seconds, so the window is short, but it is not closed.
        var referencedDelineationIDs = loadGeneratingUnits
            .Where(x => x.DelineationID.HasValue)
            .Select(x => x.DelineationID!.Value)
            .Distinct()
            .ToList();
        if (referencedDelineationIDs.Any())
        {
            var existingDelineationIDs = (await _dbContext.Delineations.AsNoTracking()
                    .Where(x => referencedDelineationIDs.Contains(x.DelineationID))
                    .Select(x => x.DelineationID)
                    .ToListAsync())
                .ToHashSet();
            loadGeneratingUnits = loadGeneratingUnits
                .Where(x => !x.DelineationID.HasValue || existingDelineationIDs.Contains(x.DelineationID.Value))
                .ToList();
        }

        if (loadGeneratingUnits.Any())
        {
            await _dbContext.LoadGeneratingUnits.AddRangeAsync(loadGeneratingUnits);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pLoadGeneratingUnitMakeValid");
        }

        if (loadGeneratingUnitRefreshArea != null)
        {
            // NPT-1158: the only place ProcessDate is set, so an empty one means the refresh is still
            // queued, is running, or failed (a failure also emails support and shows as a failed
            // Hangfire job).
            loadGeneratingUnitRefreshArea.ProcessDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Housekeeping only: the refresh has already completed and been recorded, so a failure
            // here must not turn it into a failed job and a support email.
            try
            {
                var prunedCount = await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(_dbContext, DateTime.UtcNow);
                if (prunedCount > 0)
                {
                    _logger.LogInformation("LGU overlay: removed {PrunedCount} refresh areas older than {RetentionDays} days",
                        prunedCount, LoadGeneratingUnitRefreshAreas.RetentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LGU overlay: could not remove old refresh areas; the refresh itself completed");
            }
        }

        if (exportFeatures != null)
        {
            var exportPath = Path.Combine(_configuration.OverlayDebugExportFolder!, $"LGU{DateTime.Now.Ticks}_nts.geojson");
            Directory.CreateDirectory(_configuration.OverlayDebugExportFolder!);
            await GeoJsonSerializer.SerializeToFileAsync(exportFeatures, exportPath, GeoJsonSerializer.DefaultSerializerOptions);
            _logger.LogInformation("LGU overlay: parity export written to {ExportPath}", exportPath);
        }

        return Ok();
    }

    // Delta refresh delete (NPT-1158): OverlayEngine.PlanDeltaRefresh decides, in one place, which
    // stored LGUs go and which new polygons replace them. This deletes those rows, and their HRU
    // characteristics, and returns the polygons to insert. SQL Server only narrows the candidates.
    private async Task<List<(OverlayFeature Feature, Polygon Polygon)>> DeleteLoadGeneratingUnitsForDeltaRefresh(
        Geometry refreshAreaGeometry, List<OverlayFeature> pieces)
    {
        var candidateZone = OverlayEngine.DeltaRefreshCandidateZone(refreshAreaGeometry);
        var stored = (await _dbContext.LoadGeneratingUnits.AsNoTracking()
                .Where(x => x.LoadGeneratingUnitGeometry.Intersects(candidateZone))
                .Select(x => new
                {
                    x.LoadGeneratingUnitID, x.LoadGeneratingUnitGeometry, x.RegionalSubbasinID, x.DelineationID, x.WaterQualityManagementPlanID,
                })
                .ToListAsync())
            .Select(x => (x.LoadGeneratingUnitID, new OverlayFeature
            {
                Geometry = x.LoadGeneratingUnitGeometry, RegionalSubbasinID = x.RegionalSubbasinID,
                DelineationID = x.DelineationID, WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
            }))
            .ToList();

        var (loadGeneratingUnitIDs, polygonsToInsert, matchedBeyondCandidateZone) = OverlayEngine.PlanDeltaRefresh(stored, pieces, refreshAreaGeometry);
        if (matchedBeyondCandidateZone > 0)
        {
            _logger.LogWarning(
                "LGU overlay: delta refresh matched {MatchedCount} stored row(s) or polygon(s) that extend past the area it checks around the refresh area. A duplicate or a gap may remain beyond it; a total LGU refresh clears either.",
                matchedBeyondCandidateZone);
        }
        if (loadGeneratingUnitIDs.Count > 0)
        {
            await _dbContext.HRUCharacteristics.Where(x => loadGeneratingUnitIDs.Contains(x.LoadGeneratingUnitID)).ExecuteDeleteAsync();
            await _dbContext.LoadGeneratingUnits.Where(x => loadGeneratingUnitIDs.Contains(x.LoadGeneratingUnitID)).ExecuteDeleteAsync();
        }
        _logger.LogInformation("LGU overlay: delta refresh deletes {DeletedRowCount} stored LGUs and inserts {InsertedPolygonCount} polygons",
            loadGeneratingUnitIDs.Count, polygonsToInsert.Count);
        return polygonsToInsert;
    }

    // The rules (what goes in, who wins an overlap, which pieces are units) are in
    // TrashGeneratingUnitOverlay; this action only loads, runs and stores.
    private async Task<IActionResult> GenerateTrashGeneratingUnitsImpl(GenerateTrashGeneratingUnitRequestDto requestDto)
    {
        var stopwatch = Stopwatch.StartNew();

        var inputs = await TrashGeneratingUnitOverlay.LoadInputsAsync(_dbContext);
        _logger.LogInformation("TGU overlay: loaded delin={DelinCount} ovta={OvtaCount} wqmp={WqmpCount} lub={LubCount} in {Elapsed}s",
            inputs.Delineations.Count, inputs.OnlandVisualTrashAssessmentAreas.Count, inputs.WaterQualityManagementPlans.Count,
            inputs.LandUseBlocks.Count, stopwatch.Elapsed.TotalSeconds);

        var result = TrashGeneratingUnitOverlay.Run(inputs);
        _logger.LogInformation("TGU overlay: produced {UnitCount} units at {Elapsed}s", result.Units.Count, stopwatch.Elapsed.TotalSeconds);

        var trashGeneratingUnits = new List<TrashGeneratingUnit>();
        var trashGeneratingUnit4326s = new List<TrashGeneratingUnit4326>();
        var exportFeatures = _configuration.OverlayDebugExportFolder != null ? new FeatureCollection() : null;
        var lastUpdateDate = DateTime.UtcNow;
        foreach (var (feature, polygon) in result.Units)
        {
            var trashGeneratingUnit = new TrashGeneratingUnit
            {
                StormwaterJurisdictionID = feature.StormwaterJurisdictionID!.Value,
                TrashGeneratingUnitGeometry = polygon,
                DelineationID = feature.DelineationID,
                WaterQualityManagementPlanID = feature.WaterQualityManagementPlanID,
                LandUseBlockID = feature.LandUseBlockID,
                OnlandVisualTrashAssessmentAreaID = feature.OnlandVisualTrashAssessmentAreaID,
                LastUpdateDate = lastUpdateDate
            };
            trashGeneratingUnits.Add(trashGeneratingUnit);
            trashGeneratingUnit4326s.Add(new TrashGeneratingUnit4326
            {
                StormwaterJurisdictionID = feature.StormwaterJurisdictionID!.Value,
                TrashGeneratingUnit4326Geometry = polygon.ProjectTo4326(),
                DelineationID = feature.DelineationID,
                WaterQualityManagementPlanID = feature.WaterQualityManagementPlanID,
                LandUseBlockID = feature.LandUseBlockID,
                OnlandVisualTrashAssessmentAreaID = feature.OnlandVisualTrashAssessmentAreaID,
                LastUpdateDate = lastUpdateDate,
                TrashGeneratingUnit = trashGeneratingUnit
            });
            exportFeatures?.Add(new Feature(polygon, new AttributesTable
            {
                { "DelinID", feature.DelineationID }, { "OVTAID", feature.OnlandVisualTrashAssessmentAreaID },
                { "WQMPID", feature.WaterQualityManagementPlanID }, { "LUBID", feature.LandUseBlockID },
                { "SJID", feature.StormwaterJurisdictionID },
            }));
        }

        await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitDelete");
        await InsertTrashGeneratingUnitsChunked(trashGeneratingUnits, trashGeneratingUnit4326s);
        if (trashGeneratingUnits.Count > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitMakeValid");
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnit4326MakeValid");
        }
        _logger.LogInformation("TGU overlay: inserted {RowCount} rows (x2 with 4326); total {Elapsed}s", trashGeneratingUnits.Count, stopwatch.Elapsed.TotalSeconds);
        LogLandUseBlocksWithMoreTrashGeneratingUnitAreaThanTheirOwn(result.LandUseBlockAreas, trashGeneratingUnits);

        if (exportFeatures != null)
        {
            var exportPath = Path.Combine(_configuration.OverlayDebugExportFolder!, $"TGU{DateTime.Now.Ticks}_nts.geojson");
            Directory.CreateDirectory(_configuration.OverlayDebugExportFolder!);
            await GeoJsonSerializer.SerializeToFileAsync(exportFeatures, exportPath, GeoJsonSerializer.DefaultSerializerOptions);
            _logger.LogInformation("TGU overlay: parity export written to {ExportPath}", exportPath);
        }

        return Ok();
    }

    // ≈ 0.005 ac, the tolerance of the NPT-1143 detection query. A correct run is over by square
    // millimetres at most; the defect this guards against counted whole blocks twice.
    private const double LandUseBlockOverCountToleranceSquareMeters = 20;
    private const int MaximumOverCountedLandUseBlocksToName = 50;

    // NPT-1143 guard: TGUs are cut from land use blocks, so a block's TGUs can never total more than
    // the block. The run still completes (the rows are already written); the error makes a
    // recurrence visible in the logs at block level instead of waiting for someone to compare exports.
    private void LogLandUseBlocksWithMoreTrashGeneratingUnitAreaThanTheirOwn(IReadOnlyDictionary<int, double> landUseBlockAreas,
        List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        var overCounted = OverlayEngine.FindOverCountedSources(
            trashGeneratingUnits.Select(x => (x.LandUseBlockID!.Value, x.TrashGeneratingUnitGeometry.Area)),
            landUseBlockAreas, LandUseBlockOverCountToleranceSquareMeters);
        if (overCounted.Count == 0)
        {
            _logger.LogInformation("TGU overlay: no land use block has more TGU area than its own area");
            return;
        }

        var named = string.Join("; ", overCounted.Take(MaximumOverCountedLandUseBlocksToName).Select(x =>
            $"{x.SourceID} (block {x.SourceArea:F1} m², TGUs {x.PieceArea:F1} m²)"));
        _logger.LogError(
            "TGU overlay: {OverCountedBlockCount} land use block(s) have more TGU area than their own area (tolerance {Tolerance} m²), {ExcessArea} m² in total — the overlay counted ground twice. First {NamedCount} LandUseBlockIDs: {OverCountedBlocks}",
            overCounted.Count, LandUseBlockOverCountToleranceSquareMeters, Math.Round(overCounted.Sum(x => x.PieceArea - x.SourceArea), 1),
            Math.Min(overCounted.Count, MaximumOverCountedLandUseBlocksToName), named);
    }

    // Chunked inserts: ~300K entities in one SaveChanges was the pipeline's real memory peak (5.5 GiB
    // measured). Each TGU chunk carries its 4326 twins so EF's navigation fixup resolves the FK in-chunk.
    private async Task InsertTrashGeneratingUnitsChunked(List<TrashGeneratingUnit> trashGeneratingUnits, List<TrashGeneratingUnit4326> trashGeneratingUnit4326s, int chunkSize = 2000)
    {
        for (var offset = 0; offset < trashGeneratingUnits.Count; offset += chunkSize)
        {
            var count = Math.Min(chunkSize, trashGeneratingUnits.Count - offset);
            _dbContext.TrashGeneratingUnits.AddRange(trashGeneratingUnits.GetRange(offset, count));
            _dbContext.TrashGeneratingUnit4326s.AddRange(trashGeneratingUnit4326s.GetRange(offset, count));
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }
    }

    private async Task<LoadGeneratingUnitRefreshArea?> GetLoadGeneratingUnitRefreshAreaIfProvided(GenerateLoadGeneratingUnitRequestDto requestDto)
    {
        var loadGeneratingUnitRefreshAreaID = requestDto.LoadGeneratingUnitRefreshAreaID;
        if (loadGeneratingUnitRefreshAreaID == null)
        {
            return null;
        }

        await _dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pLoadGeneratingUnitRefreshAreaMakeValid @LoadGeneratingUnitRefreshAreaID = {loadGeneratingUnitRefreshAreaID.Value}");
        var loadGeneratingUnitRefreshArea = await _dbContext.LoadGeneratingUnitRefreshAreas.FindAsync(loadGeneratingUnitRefreshAreaID.Value);
        if (loadGeneratingUnitRefreshArea == null)
        {
            // A nonexistent ID (bad caller input, or the area was deleted between enqueue and processing)
            // previously surfaced as a NullReferenceException here.
            throw new ApplicationException($"LoadGeneratingUnitRefreshArea {loadGeneratingUnitRefreshAreaID.Value} does not exist; cannot run a delta LGU refresh against it.");
        }
        return loadGeneratingUnitRefreshArea;
    }
}