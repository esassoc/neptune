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

        var regionalSubbasinIDs = requestDto.RegionalSubbasinIDs;
        var regionalSubbasins = OverlayEngine.Clean(_dbContext.vPyQgisRegionalSubbasinLGUInputs.AsNoTracking()
            .Where(x => regionalSubbasinIDs.Contains(x.RSBID)).Select(x =>
                new OverlayFeature { Geometry = x.CatchmentGeometry, RegionalSubbasinID = x.RSBID, ModelBasinID = x.ModelID }).ToList());
        var delineations = OverlayEngine.Clean(_dbContext.vPyQgisProjectDelineationLGUInputs.AsNoTracking()
            .Where(x => x.ProjectID == null || x.ProjectID == projectID).Select(x =>
                new OverlayFeature { Geometry = x.DelineationGeometry, DelineationID = x.DelinID }).ToList());

        var pieces = RunLoadGeneratingUnitOverlay(regionalSubbasins, delineations, null);

        var projectLoadGeneratingUnits = new List<ProjectLoadGeneratingUnit>();
        foreach (var (feature, polygon) in OverlayEngine.ExplodeAndDropSlivers(pieces))
        {
            if (feature.RegionalSubbasinID == null) continue;
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

        var delineations = OverlayEngine.Clean(_dbContext.vPyQgisDelineationLGUInputs.AsNoTracking().Select(x =>
            new OverlayFeature { Geometry = x.DelineationGeometry, DelineationID = x.DelinID }).ToList());
        var regionalSubbasins = OverlayEngine.Clean(_dbContext.vPyQgisRegionalSubbasinLGUInputs.AsNoTracking().Select(x =>
            new OverlayFeature { Geometry = x.CatchmentGeometry, RegionalSubbasinID = x.RSBID, ModelBasinID = x.ModelID }).ToList());

        var pieces = RunLoadGeneratingUnitOverlay(regionalSubbasins, delineations,
            loadGeneratingUnitRefreshArea?.LoadGeneratingUnitRefreshAreaGeometry);

        if (loadGeneratingUnitRefreshArea != null)
        {
            await _dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pDeleteLoadGeneratingUnitsPriorToDeltaRefresh @LoadGeneratingUnitRefreshAreaID = {loadGeneratingUnitRefreshArea.LoadGeneratingUnitRefreshAreaID}");
        }
        else
        {
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteLoadGeneratingUnitsPriorToTotalRefresh");
        }

        var loadGeneratingUnits = new List<LoadGeneratingUnit>();
        var exportFeatures = _configuration.OverlayDebugExportFolder != null ? new FeatureCollection() : null;
        foreach (var (feature, polygon) in OverlayEngine.ExplodeAndDropSlivers(pieces))
        {
            if (feature.RegionalSubbasinID == null) continue;
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
        // insert (a null DelineationID is valid — those are regional-subbasin-only LGUs). The in-process
        // overlay shrank the window from ~minutes to ~seconds, but it hasn't closed.
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
            loadGeneratingUnitRefreshArea.ProcessDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
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

    // The shared LGU/PLGU overlay, replacing ModelingOverlayAnalysis.py: clip the inputs to the modeling
    // extent (union of model basins — the basins contribute clip geometry only, no attributes), union
    // RSB+WQMP then delineations, and for a delta refresh keep only pieces touching the refresh area.
    // Note: the python's --rsb_ids branch was dead code (missing `global`), so PLGU runs always clipped
    // to the model-basin extent too — this port replicates that actual behavior.
    private List<OverlayFeature> RunLoadGeneratingUnitOverlay(List<OverlayFeature> regionalSubbasins,
        List<OverlayFeature> delineations, Geometry? refreshAreaFilter)
    {
        var modelBasinGeometries = OverlayEngine.Clean(_dbContext.vPyQgisModelBasinLGUInputs.AsNoTracking().Select(x =>
            new OverlayFeature { Geometry = x.ModelBasinGeometry }).ToList());
        var wqmps = OverlayEngine.Clean(_dbContext.vPyQgisWaterQualityManagementPlanLGUInputs.AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanBoundary != null).Select(x =>
                new OverlayFeature { Geometry = x.WaterQualityManagementPlanBoundary!, WaterQualityManagementPlanID = x.WQMPID }).ToList());

        var modelingExtent = OverlayEngine.ToPolygonal2D(
            NetTopologySuite.Operation.Union.UnaryUnionOp.Union(modelBasinGeometries.Select(x => x.Geometry).ToList()));
        var clippedRegionalSubbasins = OverlayEngine.Clip(regionalSubbasins, modelingExtent);
        var clippedDelineations = OverlayEngine.Clip(delineations, modelingExtent);
        var clippedWqmps = OverlayEngine.Clip(wqmps, modelingExtent);

        var rsbWqmp = OverlayEngine.UnionLayers(clippedRegionalSubbasins, clippedWqmps);
        var master = OverlayEngine.UnionLayers(rsbWqmp, clippedDelineations);

        return refreshAreaFilter != null ? OverlayEngine.FilterIntersecting(master, refreshAreaFilter) : master;
    }

    // NPT-1105 Part 2: TGU generation runs in-process on NetTopologySuite (OverlayEngine) — no python
    // subprocess, no temp GeoJSON. Winner rules match the retired ComputeTrashGeneratingUnits.py
    // (delineations/WQMPs: higher TCEffect wins; OVTAs: later AssessDate wins) with one deliberate
    // change: ties break deterministically on ID (higher wins) where QGIS was iteration-order-dependent.
    private async Task<IActionResult> GenerateTrashGeneratingUnitsImpl(GenerateTrashGeneratingUnitRequestDto requestDto)
    {
        var stopwatch = Stopwatch.StartNew();

        var delineations = OverlayEngine.Clean(_dbContext.vPyQgisDelineationTGUInputs.AsNoTracking().Select(x =>
            new OverlayFeature
            {
                Geometry = x.DelineationGeometry,
                DelineationID = x.DelinID,
                StormwaterJurisdictionID = x.SJID,
                TrashCaptureEffectiveness = x.TCEffect,
            }).ToList());
        var ovtas = OverlayEngine.Clean(_dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Select(x =>
            new OverlayFeature
            {
                Geometry = x.OnlandVisualTrashAssessmentAreaGeometry,
                OnlandVisualTrashAssessmentAreaID = x.OVTAID,
                AssessmentDate = x.AssessDate,
            }).ToList());
        var wqmps = OverlayEngine.Clean(_dbContext.vPyQgisWaterQualityManagementPlanTGUInputs.AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanBoundary != null).Select(x =>
            new OverlayFeature
            {
                Geometry = x.WaterQualityManagementPlanBoundary!,
                WaterQualityManagementPlanID = x.WQMPID,
                TrashCaptureEffectiveness = x.TCEffect,
            }).ToList());
        var landUseBlocks = OverlayEngine.Clean(_dbContext.vPyQgisLandUseBlockTGUInputs.AsNoTracking().Select(x =>
            new OverlayFeature
            {
                Geometry = x.LandUseBlockGeometry,
                LandUseBlockID = x.LUBID,
                StormwaterJurisdictionID = x.SJID,
            }).ToList());
        _logger.LogInformation("TGU overlay: loaded+cleaned delin={DelinCount} ovta={OvtaCount} wqmp={WqmpCount} lub={LubCount} in {Elapsed}s",
            delineations.Count, ovtas.Count, wqmps.Count, landUseBlocks.Count, stopwatch.Elapsed.TotalSeconds);

        // de-overlap each layer by its winner rule; ties break on ID (higher wins) for determinism
        delineations = OverlayEngine.Flatten(delineations, x => x.DelineationID!.Value,
            (a, b) => LosesByTrashCaptureEffectiveness(a, b, x => x.DelineationID!.Value));
        ovtas = OverlayEngine.Flatten(ovtas, x => x.OnlandVisualTrashAssessmentAreaID!.Value,
            (a, b) =>
            {
                var (dateA, dateB) = (a.AssessmentDate ?? DateTime.MinValue, b.AssessmentDate ?? DateTime.MinValue);
                return dateA != dateB
                    ? dateA < dateB
                    : a.OnlandVisualTrashAssessmentAreaID!.Value < b.OnlandVisualTrashAssessmentAreaID!.Value;
            });
        wqmps = OverlayEngine.Flatten(wqmps, x => x.WaterQualityManagementPlanID!.Value,
            (a, b) => LosesByTrashCaptureEffectiveness(a, b, x => x.WaterQualityManagementPlanID!.Value));

        // union order matches the python: (OVTA ∪ Delin) ∪ WQMP, then LUB first so its SJID wins the merge
        var odw = OverlayEngine.UnionLayers(ovtas, delineations);
        odw = OverlayEngine.UnionLayers(odw, wqmps);
        var pieces = OverlayEngine.UnionLayers(landUseBlocks, odw);
        _logger.LogInformation("TGU overlay: flatten+union produced {PieceCount} pieces at {Elapsed}s", pieces.Count, stopwatch.Elapsed.TotalSeconds);

        var trashGeneratingUnits = new List<TrashGeneratingUnit>();
        var trashGeneratingUnit4326s = new List<TrashGeneratingUnit4326>();
        var exportFeatures = _configuration.OverlayDebugExportFolder != null ? new FeatureCollection() : null;
        var lastUpdateDate = DateTime.UtcNow;
        foreach (var (feature, polygon) in OverlayEngine.ExplodeAndDropSlivers(pieces))
        {
            // pieces without a land use block (delineation remainders over unmapped ground) are not TGUs
            if (feature.LandUseBlockID == null || feature.StormwaterJurisdictionID == null) continue;

            var trashGeneratingUnit = new TrashGeneratingUnit
            {
                StormwaterJurisdictionID = feature.StormwaterJurisdictionID.Value,
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
                StormwaterJurisdictionID = feature.StormwaterJurisdictionID.Value,
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

        await _dbContext.Database.ExecuteSqlRawAsync($"EXEC dbo.pTrashGeneratingUnitDelete");
        await InsertTrashGeneratingUnitsChunked(trashGeneratingUnits, trashGeneratingUnit4326s);
        if (trashGeneratingUnits.Count > 0)
        {
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitMakeValid");
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnit4326MakeValid");
        }
        _logger.LogInformation("TGU overlay: inserted {RowCount} rows (x2 with 4326); total {Elapsed}s", trashGeneratingUnits.Count, stopwatch.Elapsed.TotalSeconds);

        if (exportFeatures != null)
        {
            var exportPath = Path.Combine(_configuration.OverlayDebugExportFolder!, $"TGU{DateTime.Now.Ticks}_nts.geojson");
            Directory.CreateDirectory(_configuration.OverlayDebugExportFolder!);
            await GeoJsonSerializer.SerializeToFileAsync(exportFeatures, exportPath, GeoJsonSerializer.DefaultSerializerOptions);
            _logger.LogInformation("TGU overlay: parity export written to {ExportPath}", exportPath);
        }

        return Ok();
    }

    // Winner rule for delineations and WQMPs: higher TCEffect wins; TCEffect tie → higher ID wins.
    private static bool LosesByTrashCaptureEffectiveness(OverlayFeature a, OverlayFeature b, Func<OverlayFeature, int> idOf)
    {
        var (effectA, effectB) = (a.TrashCaptureEffectiveness ?? double.MinValue, b.TrashCaptureEffectiveness ?? double.MinValue);
        return effectA != effectB ? effectA < effectB : idOf(a) < idOf(b);
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

        await _dbContext.Database.ExecuteSqlRawAsync($"EXEC dbo.pLoadGeneratingUnitRefreshAreaMakeValid @LoadGeneratingUnitRefreshAreaID = {loadGeneratingUnitRefreshAreaID.Value}");
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