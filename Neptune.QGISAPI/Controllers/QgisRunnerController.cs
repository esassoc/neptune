using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.QGISAPI.Services;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.QGISAPI.Controllers;

[ApiController]
public class QgisRunnerController : ControllerBase
{
    private readonly ILogger<QgisRunnerController> _logger;
    private readonly NeptuneDbContext _dbContext;
    private readonly QgisService _qgisService;
    private readonly IAzureStorage _azureStorage;

    public QgisRunnerController(ILogger<QgisRunnerController> logger, NeptuneDbContext dbContext, QgisService qgisService,
        IAzureStorage azureStorage)
    {
        _logger = logger;
        _dbContext = dbContext;
        _qgisService = qgisService;
        _azureStorage = azureStorage;
    }

    [HttpGet("/")]
    public ActionResult Get()
    {
        return Ok("Hello from the QGIS API!");
    }

    [HttpPost("qgis/generate-plgus")]
    public async Task<IActionResult> GenerateProjectLoadGeneratingUnits([FromBody] GenerateProjectLoadGeneratingUnitRequestDto requestDto)
    {
        var projectID = requestDto.ProjectID;
        var project = _dbContext.Projects.AsNoTracking().SingleOrDefault(x => x.ProjectID == projectID);
        if (project == null)
        {
            return NotFound($"Project with ID {projectID} does not exist!");
        }

        var regionalSubbasinIDs = requestDto.RegionalSubbasinIDs;
        var regionalSubbasinInputFeatures = _dbContext.vPyQgisRegionalSubbasinLGUInputs.AsNoTracking()
            .Where(x => regionalSubbasinIDs.Contains(x.RSBID)).Select(x =>
                new Feature(x.CatchmentGeometry, new AttributesTable { { "RSBID", x.RSBID }, { "ModelID", x.ModelID } }))
            .ToList();
        var lguInputs = _dbContext.vPyQgisProjectDelineationLGUInputs.AsNoTracking()
            .Where(x => x.ProjectID == null || x.ProjectID == projectID).Select(x =>
                new Feature(x.DelineationGeometry, new AttributesTable { { "DelinID", x.DelinID } })).ToList();
        var outputFolder = Path.GetTempPath();
        var outputLayerPrefix = $"{"PLGU"}{DateTime.Now.Ticks}";
        var featureCollection = await GenerateLGUsImpl(regionalSubbasinInputFeatures, lguInputs, outputFolder, outputLayerPrefix, regionalSubbasinIDs, null);
        var projectLoadGeneratingUnits = new List<ProjectLoadGeneratingUnit>();

        foreach (var feature in featureCollection.Where(x => x.Geometry != null).ToList())
        {
            var loadGeneratingUnitResult = GeoJsonSerializer.DeserializeFromFeature<LoadGeneratingUnitResult>(feature, GeoJsonSerializer.DefaultSerializerOptions);

            // we should only get Polygons from the Pyqgis rodeo overlay, but when we convert geojson to Geometry, they can result in invalid geometries
            // however, when we run makevalid, it can potentially change the geometry type from Polygon to a MultiPolygon or GeometryCollection
            // so we need to explode them if that happens since we are only expecting polygons for LGUs
            var geometries = GeometryHelper.MakeValidAndExplodeIfNeeded(loadGeneratingUnitResult.Geometry);

            projectLoadGeneratingUnits.AddRange(geometries.Select(geometry =>
            {
                geometry.SRID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;
                return new ProjectLoadGeneratingUnit
                {
                    ProjectLoadGeneratingUnitGeometry = geometry,
                    ProjectID = projectID,
                    DelineationID = loadGeneratingUnitResult.DelineationID,
                    WaterQualityManagementPlanID = loadGeneratingUnitResult.WaterQualityManagementPlanID,
                    ModelBasinID = loadGeneratingUnitResult.ModelBasinID,
                    RegionalSubbasinID = loadGeneratingUnitResult.RegionalSubbasinID
                };
            }));
        }

        await _dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pDeleteProjectLoadGeneratingUnitsPriorToRefreshForProject @ProjectID = {projectID}");

        if (projectLoadGeneratingUnits.Any())
        {
            await _dbContext.ProjectLoadGeneratingUnits.AddRangeAsync(projectLoadGeneratingUnits);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pProjectLoadGeneratingUnitMakeValid");
        }

        DeleteTempFiles(outputFolder, outputLayerPrefix);
        return Ok();
    }

    [HttpPost("qgis/generate-lgus")]
    public async Task<IActionResult> GenerateLoadGeneratingUnits([FromBody] GenerateLoadGeneratingUnitRequestDto requestDto)
    {
        var outputFolder = Path.GetTempPath();
        var outputLayerPrefix = $"LGU{DateTime.Now.Ticks}";
        var lguInputFeatures = _dbContext.vPyQgisDelineationLGUInputs.AsNoTracking().Select(x =>
            new Feature(x.DelineationGeometry, new AttributesTable { { "DelinID", x.DelinID } })).ToList();
        var regionalSubbasinInputFeatures = _dbContext.vPyQgisRegionalSubbasinLGUInputs.AsNoTracking().Select(x =>
            new Feature(x.CatchmentGeometry, new AttributesTable { { "RSBID", x.RSBID }, { "ModelID", x.ModelID } })).ToList();
        var filterLayerPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}_inputFilter.geojson";
        var loadGeneratingUnitRefreshArea = await CreateLoadGeneratingUnitRefreshAreaIfProvided(requestDto, filterLayerPath);
        var featureCollection = await GenerateLGUsImpl(regionalSubbasinInputFeatures, lguInputFeatures, outputFolder, outputLayerPrefix, new List<int>(), loadGeneratingUnitRefreshArea != null ? filterLayerPath : null);

        if (loadGeneratingUnitRefreshArea != null)
        {
            await _dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pDeleteLoadGeneratingUnitsPriorToDeltaRefresh @LoadGeneratingUnitRefreshAreaID = {loadGeneratingUnitRefreshArea.LoadGeneratingUnitRefreshAreaID}");
        }
        else
        {
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteLoadGeneratingUnitsPriorToTotalRefresh");
        }

        var loadGeneratingUnits = new List<LoadGeneratingUnit>();

        foreach (var feature in featureCollection.Where(x => x.Geometry != null).ToList())
        {
            var loadGeneratingUnitResult = GeoJsonSerializer.DeserializeFromFeature<LoadGeneratingUnitResult>(feature, GeoJsonSerializer.DefaultSerializerOptions);

            // we should only get Polygons from the Pyqgis rodeo overlay, but when we convert geojson to Geometry, they can result in invalid geometries
            // however, when we run makevalid, it can potentially change the geometry type from Polygon to a MultiPolygon or GeometryCollection
            // so we need to explode them if that happens since we are only expecting polygons for LGUs
            var geometries = GeometryHelper.MakeValidAndExplodeIfNeeded(loadGeneratingUnitResult.Geometry);

            foreach (var geometry in geometries)
            {
                geometry.SRID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;
                var loadGeneratingUnit = new LoadGeneratingUnit()
                {
                    LoadGeneratingUnitGeometry = geometry,
                    DelineationID = loadGeneratingUnitResult.DelineationID,
                    WaterQualityManagementPlanID = loadGeneratingUnitResult.WaterQualityManagementPlanID,
                    ModelBasinID = loadGeneratingUnitResult.ModelBasinID,
                    RegionalSubbasinID = loadGeneratingUnitResult.RegionalSubbasinID,
                    LoadGeneratingUnitGeometry4326 = geometry.ProjectTo4326(),
                };
                loadGeneratingUnits.Add(loadGeneratingUnit);
            }
        }

        // NPT-981: the delineation input set is snapshotted at the top of this action, but the QGIS
        // overlay above runs long enough that a user can delete a delineation on the Delineation Map
        // before we reach this insert. That would leave an LGU pointing at a now-deleted DelineationID
        // and trip FK_LoadGeneratingUnit_Delineation_DelineationID. Drop any orphaned rows just before
        // the insert (a null DelineationID is valid — those are regional-subbasin-only LGUs).
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

        DeleteTempFiles(outputFolder, outputLayerPrefix);
        return Ok();
    }

    [HttpPost("qgis/generate-tgus")]
    public async Task<IActionResult> GenerateTrashGeneratingUnits([FromBody] GenerateTrashGeneratingUnitRequestDto requestDto)
    {
        var outputFolder = Path.GetTempPath();
        var outputLayerPrefix = $"TGU{DateTime.Now.Ticks}";
        var outputFolderAndPrefix = Path.Combine(outputFolder, outputLayerPrefix);
        var outputLayerPath = $"{outputFolderAndPrefix}.geojson";
        var tguInputPath = $"{outputFolderAndPrefix}delineationLayer.geojson";
        var ovtaInputPath = $"{outputFolderAndPrefix}ovtaLayer.geojson";
        var wqmpInputPath = $"{outputFolderAndPrefix}wqmpLayer.geojson";
        var landUseBlockInputPath = $"{outputFolderAndPrefix}landUseBlockLayer.geojson";

        var tguInputFeatures = _dbContext.vPyQgisDelineationTGUInputs.AsNoTracking().Select(x =>
            new Feature(x.DelineationGeometry, new AttributesTable { { "DelinID", x.DelinID }, { "SJID", x.SJID }, { "TCEffect", x.TCEffect } })).ToList();
        await WriteFeaturesToGeoJsonFile(tguInputPath, tguInputFeatures);

        var ovtaInputFeatures = _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Select(x => new Feature(x.OnlandVisualTrashAssessmentAreaGeometry, new AttributesTable { { "OVTAID", x.OVTAID }, { "AssessDate", x.AssessDate } })).ToList();
        await WriteFeaturesToGeoJsonFile(ovtaInputPath, ovtaInputFeatures);

        var wqmpInputFeatures = _dbContext.vPyQgisWaterQualityManagementPlanTGUInputs.AsNoTracking().Select(x =>
            new Feature(x.WaterQualityManagementPlanBoundary, new AttributesTable { { "WQMPID", x.WQMPID }, { "TCEffect", x.TCEffect } })).ToList();
        await WriteFeaturesToGeoJsonFile(wqmpInputPath, wqmpInputFeatures);

        var landUseBlockInputFeatures = _dbContext.vPyQgisLandUseBlockTGUInputs.AsNoTracking().Select(x =>
            new Feature(x.LandUseBlockGeometry, new AttributesTable { { "LUBID", x.LUBID }, { "SJID", x.SJID } })).ToList();
        await WriteFeaturesToGeoJsonFile(landUseBlockInputPath, landUseBlockInputFeatures);

        var commandLineArguments = new List<string>
        {
            "ComputeTrashGeneratingUnits.py", outputFolder, outputLayerPrefix, tguInputPath, ovtaInputPath, wqmpInputPath, landUseBlockInputPath
        };
        _qgisService.Run(commandLineArguments);

        FeatureCollection featureCollection;
        await using (var openStream = System.IO.File.OpenRead(outputLayerPath))
        {
            featureCollection = await GeoJsonSerializer.GetFeatureCollectionFromGeoJsonStream(openStream,
                GeoJsonSerializer.DefaultSerializerOptions);
        }

        await _dbContext.Database.ExecuteSqlRawAsync($"EXEC dbo.pTrashGeneratingUnitDelete");
        var trashGeneratingUnits = new List<TrashGeneratingUnit>();
        var trashGeneratingUnit4326s = new List<TrashGeneratingUnit4326>();

        foreach (var feature in featureCollection.Where(x => x.Geometry != null && x.Attributes["LUBID"] != null && x.Attributes["SJID"] != null).ToList())
        {
            var trashGeneratingUnitResult = GeoJsonSerializer.DeserializeFromFeature<TrashGeneratingUnitResult>(feature, GeoJsonSerializer.DefaultSerializerOptions);

            // we should only get Polygons from the Pyqgis rodeo overlay, but when we convert geojson to Geometry, they can result in invalid geometries
            // however, when we run makevalid, it can potentially change the geometry type from Polygon to a MultiPolygon or GeometryCollection
            // so we need to explode them if that happens since we are only expecting polygons for TGUs
            var geometries = GeometryHelper.MakeValidAndExplodeIfNeeded(trashGeneratingUnitResult.Geometry);
            var stormwaterJurisdictionID = trashGeneratingUnitResult.StormwaterJurisdictionID;
            var delineationID = trashGeneratingUnitResult.DelineationID;
            var waterQualityManagementPlanID = trashGeneratingUnitResult.WaterQualityManagementPlanID;
            var landUseBlockID = trashGeneratingUnitResult.LandUseBlockID;
            var onlandVisualTrashAssessmentAreaID = trashGeneratingUnitResult.OnlandVisualTrashAssessmentAreaID;

            foreach (var geometry in geometries)
            {
                geometry.SRID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;
                var trashGeneratingUnit = new TrashGeneratingUnit
                {
                    StormwaterJurisdictionID = stormwaterJurisdictionID,
                    TrashGeneratingUnitGeometry = geometry,
                    DelineationID = delineationID,
                    WaterQualityManagementPlanID = waterQualityManagementPlanID,
                    LandUseBlockID = landUseBlockID,
                    OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentAreaID,
                    LastUpdateDate = DateTime.UtcNow
                };
                trashGeneratingUnits.Add(trashGeneratingUnit);

                var trashGeneratingUnit4326 = new TrashGeneratingUnit4326
                {
                    StormwaterJurisdictionID = stormwaterJurisdictionID,
                    TrashGeneratingUnit4326Geometry = geometry.ProjectTo4326(),
                    DelineationID = delineationID,
                    WaterQualityManagementPlanID = waterQualityManagementPlanID,
                    LandUseBlockID = landUseBlockID,
                    OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentAreaID,
                    LastUpdateDate = DateTime.UtcNow,
                    TrashGeneratingUnit = trashGeneratingUnit
                };
                trashGeneratingUnit4326s.Add(trashGeneratingUnit4326);
            }
        }

        if (trashGeneratingUnits.Any())
        {
            await _dbContext.TrashGeneratingUnits.AddRangeAsync(trashGeneratingUnits);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitMakeValid");
        }

        if (trashGeneratingUnit4326s.Any())
        {
            await _dbContext.TrashGeneratingUnit4326s.AddRangeAsync(trashGeneratingUnit4326s);
            await _dbContext.SaveChangesAsync();
            await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnit4326MakeValid");
        }

        DeleteTempFiles(outputFolder, outputLayerPrefix);
        return Ok();
    }

    private async Task<FeatureCollection> GenerateLGUsImpl(IEnumerable<Feature> regionalSubbasinInputFeatures, IEnumerable<Feature> lguInputFeatures, string outputFolder, string outputLayerPrefix, List<int> regionalSubbasinIDs, string? filterLayerPath)
    {
        var outputLayerPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}.geojson";
        var lguInputPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}delineationLayer.geojson";
        var modelBasinInputPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}modelBasinLayer.geojson";
        var regionalSubbasinInputPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}regionalSubbasinLayer.geojson";
        var wqmpInputPath = $"{Path.Combine(outputFolder, outputLayerPrefix)}wqmpLayer.geojson";
        var commandLineArguments = new List<string>
        {
            "ModelingOverlayAnalysis.py", outputFolder, outputLayerPrefix, lguInputPath, modelBasinInputPath, regionalSubbasinInputPath, wqmpInputPath
        };
        if (regionalSubbasinIDs.Any())
        {
            commandLineArguments.AddRange(new List<string> { "--rsb_ids", string.Join(", ", regionalSubbasinIDs) });
        }
        if (!string.IsNullOrWhiteSpace(filterLayerPath))
        {
            commandLineArguments.AddRange(new List<string> { "--filter", filterLayerPath });
        }

        await WriteFeaturesToGeoJsonFile(lguInputPath, lguInputFeatures);

        var modelBasinInputFeatures = _dbContext.vPyQgisModelBasinLGUInputs.AsNoTracking().Select(x =>
            new Feature(x.ModelBasinGeometry, new AttributesTable { { "ModelID", x.ModelID } })).ToList();
        await WriteFeaturesToGeoJsonFile(modelBasinInputPath, modelBasinInputFeatures);

        await WriteFeaturesToGeoJsonFile(regionalSubbasinInputPath, regionalSubbasinInputFeatures);

        var wqmpInputFeatures = _dbContext.vPyQgisWaterQualityManagementPlanLGUInputs.AsNoTracking().Select(x =>
            new Feature(x.WaterQualityManagementPlanBoundary, new AttributesTable { { "WQMPID", x.WQMPID } })).ToList();
        await WriteFeaturesToGeoJsonFile(wqmpInputPath, wqmpInputFeatures);

        _qgisService.Run(commandLineArguments);

        await using var openStream = System.IO.File.OpenRead(outputLayerPath);
        var featureCollection =
            await GeoJsonSerializer.GetFeatureCollectionFromGeoJsonStream(openStream,
                GeoJsonSerializer.DefaultSerializerOptions);
        return featureCollection;
    }

    private async Task<LoadGeneratingUnitRefreshArea?> CreateLoadGeneratingUnitRefreshAreaIfProvided(GenerateLoadGeneratingUnitRequestDto requestDto,
        string clipLayerPath)
    {
        LoadGeneratingUnitRefreshArea? loadGeneratingUnitRefreshArea = null;
        var loadGeneratingUnitRefreshAreaID = requestDto.LoadGeneratingUnitRefreshAreaID;
        if (loadGeneratingUnitRefreshAreaID != null)
        {
            await _dbContext.Database.ExecuteSqlRawAsync($"EXEC dbo.pLoadGeneratingUnitRefreshAreaMakeValid @LoadGeneratingUnitRefreshAreaID = {loadGeneratingUnitRefreshAreaID.Value}");
            loadGeneratingUnitRefreshArea =
                await _dbContext.LoadGeneratingUnitRefreshAreas.FindAsync(loadGeneratingUnitRefreshAreaID.Value);
            var loadGeneratingUnitRefreshAreaGeometry = loadGeneratingUnitRefreshArea
                .LoadGeneratingUnitRefreshAreaGeometry;
            //var lguRefreshAreaGeometry = _dbContext.LoadGeneratingUnits.AsNoTracking()
            //    .Where(x => x.LoadGeneratingUnitGeometry.Intersects(loadGeneratingUnitRefreshAreaGeometry)).Select(x => x.LoadGeneratingUnitGeometry).ToList().UnionListGeometries();
            var lguInputClipFeatures = new Feature(loadGeneratingUnitRefreshAreaGeometry, new AttributesTable());
            await WriteFeaturesToGeoJsonFile(clipLayerPath, new[] { lguInputClipFeatures }, loadGeneratingUnitRefreshAreaGeometry);
        }

        return loadGeneratingUnitRefreshArea;
    }

    private static async Task WriteFeaturesToGeoJsonFile(string outputFilePath, IEnumerable<Feature> features)
    {
        await WriteFeaturesToGeoJsonFile(outputFilePath, features, null);
    }

    private static async Task WriteFeaturesToGeoJsonFile(string outputFilePath, IEnumerable<Feature> features, Geometry? extraGeometryToAppend)
    {
        var featureCollection = new FeatureCollection();
        foreach (var feature in features)
        {
            featureCollection.Add(feature);
        }

        if (extraGeometryToAppend != null)
        {
            featureCollection.Add(new Feature(extraGeometryToAppend, new AttributesTable()));
        }

        var geoJsonString = GeoJsonSerializer.Serialize(featureCollection);
        await System.IO.File.WriteAllTextAsync(outputFilePath, geoJsonString);
    }

    private static void DeleteTempFiles(string outputFolder, string outputLayerPrefix)
    {
        // clean up temp files if not running in a local environment
        foreach (var fileToDelete in Directory.EnumerateFiles(outputFolder, outputLayerPrefix + "*"))
        {
            System.IO.File.Delete(fileToDelete);
        }
    }
}