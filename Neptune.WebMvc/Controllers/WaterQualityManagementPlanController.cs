using System.Data;
using System.Net.Mail;
using ClosedXML.Excel;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Models;
using Neptune.WebMvc.Security;
using Neptune.WebMvc.Views.Shared;
using Neptune.WebMvc.Views.Shared.HRUCharacteristics;
using Neptune.WebMvc.Views.Shared.ModeledPerformance;
using Neptune.WebMvc.Views.WaterQualityManagementPlan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Hangfire;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;
using Neptune.Jobs.Hangfire;
using Neptune.Models.DataTransferObjects;
using Neptune.WebMvc.Common.Models;
using Neptune.WebMvc.Common.MvcResults;
using Neptune.WebMvc.Services;
using Neptune.WebMvc.Services.Filters;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Neptune.Common.Email;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Neptune.WebMvc.Controllers
{
    public class WaterQualityManagementPlanController(
        NeptuneDbContext dbContext,
        ILogger<WaterQualityManagementPlanController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        FileResourceService fileResourceService,
        SitkaSmtpClientService sitkaSmtpClientService,
        AzureBlobStorageService azureBlobStorageService)
        : NeptuneBaseController<WaterQualityManagementPlanController>(dbContext, logger, linkGenerator,
            webConfiguration)
    {
        [HttpGet]
        public ViewResult FindAWQMP()
        {
            var stormwaterJurisdictions = StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson);
            var stormwaterJurisdictionIDsPersonCanView = stormwaterJurisdictions.Select(x => x.StormwaterJurisdictionID);
            var wqmps = CurrentPerson.GetWQMPPersonCanView(_dbContext, stormwaterJurisdictionIDsPersonCanView).ToList();
            var jurisdictions = stormwaterJurisdictions.Select(x => x.AsDisplayDto()).ToList();
            var jurisdictionMapLayers = MapInitJsonHelpers.GetJurisdictionMapLayers(_dbContext);
            var mapInitJson = new SearchMapInitJson("StormwaterIndexMap", jurisdictionMapLayers,
                StormwaterMapInitJson.MakeWQMPLayerGeoJson(wqmps, false, false, _linkGenerator))
            {
                JurisdictionLayerGeoJson = jurisdictionMapLayers.Single(x => x.LayerName == MapInitJsonHelpers.CountyCityLayerName)
            };
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WQMPMap);
            var viewData = new FindAWQMPViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, mapInitJson, neptunePage, jurisdictions, wqmps, _webConfiguration.MapServiceUrl);
            return RazorView<FindAWQMP, FindAWQMPViewData>(viewData);
        }

        [HttpGet]
        public ContentResult FindByName()
        {
            return new ContentResult();
        }

        [HttpPost]
        public JsonResult FindByName(FindAWQMPViewModel viewModel)
        {
            var searchString = viewModel.SearchTerm.Trim().ToLower();
            var stormwaterJurisdictionIDs = viewModel.StormwaterJurisdictionIDs;
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(_dbContext, CurrentPerson);
            var allWQMPSearchString = CurrentPerson
                .GetWQMPPersonCanView(_dbContext, stormwaterJurisdictionIDsPersonCanView)
                .Where(x => 
                            stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID) &&
                            x.WaterQualityManagementPlanName.ToLower().Contains(searchString)).ToList();

            var mapSummaryUrlTemplate = new UrlTemplate<int>(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, t => t.SummaryForMap(UrlTemplate.Parameter1Int)));
            var listItems = allWQMPSearchString.OrderBy(x => x.WaterQualityManagementPlanName).Take(20).Select(x =>
            {
                var locationPoint4326 = x.WaterQualityManagementPlanBoundary?.Geometry4326;
                var treatmentBMPMapSummaryData = new SearchMapSummaryData(
                    mapSummaryUrlTemplate.ParameterReplace(x.WaterQualityManagementPlanID), locationPoint4326,
                    locationPoint4326?.Coordinate.Y,
                    locationPoint4326?.Coordinate.X,
                    x.WaterQualityManagementPlanID);
                var listItem = new SelectListItem(x.WaterQualityManagementPlanName, GeoJsonSerializer.Serialize(treatmentBMPMapSummaryData));
                return listItem;
            }).ToList();

            return Json(listItems);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public PartialViewResult SummaryForMap([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var viewData = new SummaryForMapViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan);
            return RazorPartialView<SummaryForMap, SummaryForMapViewData>(viewData);
        }

        [HttpGet]
        public ViewResult Index()
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WaterQualityMaintenancePlan);
            var gridSpec = new IndexGridSpec(_linkGenerator, CurrentPerson);
            var verificationNeptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WaterQualityMaintenancePlanOandMVerifications);
            var verificationGridSpec = new WaterQualityManagementPlanVerificationGridSpec(_linkGenerator, CurrentPerson);
            var viewData = new IndexViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, gridSpec, verificationNeptunePage, verificationGridSpec);
            return RazorView< Views.WaterQualityManagementPlan.Index, IndexViewData>(viewData);
        }

        [HttpGet]
        public GridJsonNetJObjectResult<WaterQualityManagementPlanDetailedWithTreatmentBMPsAndQuickBMPs> WaterQualityManagementPlanIndexGridData()
        {
            var gridSpec = new IndexGridSpec(_linkGenerator, CurrentPerson);
            var treatmentBmps = _dbContext.TreatmentBMPs.Include(x => x.TreatmentBMPType).AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID != null).ToList();
            var treatmentBMPModelingAttributes = vTreatmentBMPModelingAttributes.ListAsDictionary(_dbContext);
            var treatmentBMPDetaileds = treatmentBmps.Join(
                    vTreatmentBMPUpstreams.ListWithDelineationAsDictionary(_dbContext),
                    x => x.TreatmentBMPID,
                    y => y.Key,
                    (x, y) => new TreatmentBMPWithModelingAttributesAndDelineation(x, y.Value, treatmentBMPModelingAttributes.TryGetValue(x.TreatmentBMPID, out var attribute) ? attribute : null)).ToList();
            var quickBMPs = QuickBMPs.List(_dbContext);
            var waterQualityManagementPlans =
                vWaterQualityManagementPlanDetaileds.ListViewableByPerson(_dbContext, CurrentPerson).OrderBy(x => x.WaterQualityManagementPlanName)
                    .ToList();
            var query = waterQualityManagementPlans
                .GroupJoin(treatmentBMPDetaileds,
                    waterQualityManagementPlanDetailed =>
                        waterQualityManagementPlanDetailed.WaterQualityManagementPlanID,
                    treatmentBMPWithModelingAttributesAndDelineation => treatmentBMPWithModelingAttributesAndDelineation
                        .TreatmentBMP.WaterQualityManagementPlanID,
                    (waterQualityManagementPlanDetailed, gj) => new { waterQualityManagementPlanDetailed, gj })
                .GroupJoin(quickBMPs, wqmp => wqmp.waterQualityManagementPlanDetailed.WaterQualityManagementPlanID,
                    quickBMP => quickBMP.WaterQualityManagementPlanID,
                    (wqmp, gj2) =>
                        new WaterQualityManagementPlanDetailedWithTreatmentBMPsAndQuickBMPs(
                            wqmp.waterQualityManagementPlanDetailed, wqmp.gj, gj2)).ToList();
            var gridJsonNetJObjectResult = new GridJsonNetJObjectResult<WaterQualityManagementPlanDetailedWithTreatmentBMPsAndQuickBMPs>(query, gridSpec);
            return gridJsonNetJObjectResult;
        }

        [HttpGet]
        [NeptuneViewFeature]
        public GridJsonNetJObjectResult<WaterQualityManagementPlanVerify> WaterQualityManagementPlanVerificationGridData()
        {
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(_dbContext, CurrentPerson);
            var waterQualityManagementPlanVerifications = WaterQualityManagementPlanVerifies.ListViewable(_dbContext, stormwaterJurisdictionIDsPersonCanView);
            var gridSpec = new WaterQualityManagementPlanVerificationGridSpec(_linkGenerator, CurrentPerson);
            return new GridJsonNetJObjectResult<WaterQualityManagementPlanVerify>(waterQualityManagementPlanVerifications, gridSpec);
        }

        [HttpGet]
        [SitkaAdminFeature]
        public ViewResult LGUAudit()
        {
            var wqmpGridSpec = new WaterQualityManagementPlanLGUAuditGridSpec(_linkGenerator);
            var viewData = new LGUAuditViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, wqmpGridSpec);
            return RazorView<LGUAudit, LGUAuditViewData>(viewData);
        }

        [HttpGet]
        [SitkaAdminFeature]
        public GridJsonNetJObjectResult<vWaterQualityManagementPlanLGUAudit> WaterQualityManagementPlanLGUAuditGridData()
        {
            var gridSpec = new WaterQualityManagementPlanLGUAuditGridSpec(_linkGenerator);
            return new GridJsonNetJObjectResult<vWaterQualityManagementPlanLGUAudit>(
                _dbContext.vWaterQualityManagementPlanLGUAudits.ToList(), gridSpec);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [AnonymousUnclassifiedFeature] // intentionally put here to bypass having to login
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult Detail([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);

            var wqmpBMPs = TreatmentBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var treatmentBMPs = CurrentPerson.GetInventoriedBMPsForWQMP(waterQualityManagementPlan, wqmpBMPs);
            var treatmentBMPModelingAttributes =
                vTreatmentBMPModelingAttributes.ListAsDictionary(_dbContext);
            var treatmentBmpGeoJsonFeatureCollection = treatmentBMPs.ToGeoJsonFeatureCollection(_linkGenerator);
            var boundingBoxGeometries = new List<Geometry>();

            foreach (var feature in treatmentBmpGeoJsonFeatureCollection)
            {
                var treatmentBmpID = feature.Attributes.Exists("TreatmentBMPID")
                    ? (int) feature.Attributes["TreatmentBMPID"]
                    : (int?)null;
                if (treatmentBmpID != null)
                {
                    feature.Attributes.Add("PopupUrl", SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(_linkGenerator, x => x.MapPopup(treatmentBmpID)));
                }
                boundingBoxGeometries.Add(feature.Geometry);
            }

            var boundaryAreaFeatureCollection = new FeatureCollection();

            var waterQualityManagementPlanBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var wqmpGeometry = waterQualityManagementPlanBoundary?.Geometry4326;
            var hasWaterQualityManagementPlanBoundary = wqmpGeometry != null;
            if (hasWaterQualityManagementPlanBoundary)
            {
                var feature = new Feature(wqmpGeometry, new AttributesTable());
                boundaryAreaFeatureCollection.Add(feature);
                boundingBoxGeometries.Add(wqmpGeometry);
            }

            var layerGeoJsons = new List<LayerGeoJson>
            {
                new("wqmpBoundary", boundaryAreaFeatureCollection, "#fb00be",
                    1,
                    LayerInitialVisibility.Show),
                new(FieldDefinitionType.TreatmentBMP.GetFieldDefinitionLabelPluralized(),
                    treatmentBmpGeoJsonFeatureCollection,
                    "#935f59",
                    1,
                    LayerInitialVisibility.Show),

            };

            var wqmpJurisdiction = waterQualityManagementPlan.StormwaterJurisdiction;
            var boundingBoxDto = hasWaterQualityManagementPlanBoundary ? new BoundingBoxDto(boundingBoxGeometries) : new BoundingBoxDto(wqmpJurisdiction.StormwaterJurisdictionGeometry?.Geometry4326);
            var mapInitJson = new MapInitJson("waterQualityManagementPlanMap", 0, layerGeoJsons,
                boundingBoxDto);

            var treatmentBMPIDList = treatmentBMPs.Select(x => x.TreatmentBMPID).ToList();
            var delineationsDict = vTreatmentBMPUpstreams.ListWithDelineationAsDictionaryForTreatmentBMPIDList(_dbContext, treatmentBMPIDList);

            var treatmentBMPDelineations = delineationsDict.Where(x => x.Value != null).Select(x => x.Value).ToList();
            if (treatmentBMPDelineations.Any())
            {
                mapInitJson.Layers.Add(StormwaterMapInitJson.MakeDelineationLayerGeoJson(treatmentBMPDelineations));
            }

            var waterQualityManagementPlanVerifies = WaterQualityManagementPlanVerifies.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.PrimaryKey);
            var waterQualityManagementPlanVerifyDraft = waterQualityManagementPlanVerifies.SingleOrDefault(x => x.IsDraft);

            var waterQualityManagementPlanVerifyQuickBMP =
                WaterQualityManagementPlanVerifyQuickBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var waterQualityManagementPlanVerifyTreatmentBMP =
                WaterQualityManagementPlanVerifyTreatmentBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);

            var waterQualityManagementPlanModelingApproaches = WaterQualityManagementPlanModelingApproach.All;

            var hruCharacteristics = vHRUCharacteristics.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var hruCharacteristicsViewData = new HRUCharacteristicsViewData(hruCharacteristics);
            var sourceControlBMPs = SourceControlBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID)
                .Where(x => x.SourceControlBMPNote != null || (x.IsPresent == true))
                .OrderBy(x => x.SourceControlBMPAttributeID)
                .GroupBy(x => x.SourceControlBMPAttribute.SourceControlBMPAttributeCategoryID);
            var quickBmps = QuickBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var isSitkaAdmin = new SitkaAdminFeature().HasPermissionByPerson(CurrentPerson);
            var modelingResultsUrl = SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.GetModelResults(waterQualityManagementPlan));
            var nereidLog = NereidLogs.GetByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var modeledPerformanceViewData = new ModeledPerformanceViewData(_linkGenerator, modelingResultsUrl, "Site Runoff", isSitkaAdmin, nereidLog?.NereidRequest, nereidLog?.NereidResponse);
            var parcelGridSpec = new ParcelGridSpec();
            var quickBMPGridSpec = new QuickBMPGridSpec();

            var waterQualityManagementPlanDocuments = WaterQualityManagementPlanDocuments.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var calculateTotalAcreage = (waterQualityManagementPlanBoundary?.GeometryNative?.Area ?? 0) * Constants.SquareMetersToAcres;
            var viewData = new DetailViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan,
                waterQualityManagementPlanVerifyDraft, mapInitJson, treatmentBMPs, parcelGridSpec,
                waterQualityManagementPlanVerifies, waterQualityManagementPlanVerifyQuickBMP,
                waterQualityManagementPlanVerifyTreatmentBMP,
                hruCharacteristicsViewData, waterQualityManagementPlanModelingApproaches, modeledPerformanceViewData, sourceControlBMPs, quickBmps, quickBMPGridSpec, hasWaterQualityManagementPlanBoundary, delineationsDict, waterQualityManagementPlanDocuments, calculateTotalAcreage, treatmentBMPModelingAttributes);

            return RazorView<Detail, DetailViewData>(viewData);
        }


        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [AnonymousUnclassifiedFeature] // intentionally put here to bypass having to login
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public GridJsonNetJObjectResult<Parcel> ParcelsForWaterQualityManagementPlanGridData([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var parcels = WaterQualityManagementPlanParcels.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlanPrimaryKey.PrimaryKeyValue).Select(x => x.Parcel).OrderBy(x => x.ParcelNumber).ToList();
            var gridSpec = new ParcelGridSpec();
            return new GridJsonNetJObjectResult<Parcel>(parcels, gridSpec);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [AnonymousUnclassifiedFeature] // intentionally put here to bypass having to login
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public GridJsonNetJObjectResult<QuickBMP> SimplifiedStructuralBMPsForWaterQualityManagementPlanGridData([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var quickBmps =
                QuickBMPs.ListByWaterQualityManagementPlanID(_dbContext,
                    waterQualityManagementPlanPrimaryKey.PrimaryKeyValue);
            var gridSpec = new QuickBMPGridSpec();
            return new GridJsonNetJObjectResult<QuickBMP>(quickBmps, gridSpec);
        }

        #region CRUD Water Quality Management Plan
        [HttpGet]
        [WaterQualityManagementPlanCreateFeature]
        public PartialViewResult New()
        {
            var viewModel = new NewViewModel()
            {
                WaterQualityManagementPlanID = ModelObjectHelpers.NotYetAssignedID
            };
            return ViewNew(viewModel);
        }

        [HttpPost]
        [WaterQualityManagementPlanCreateFeature]
        public async Task<IActionResult> New(NewViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewNew(viewModel);
            }

            var waterQualityManagementPlan = new WaterQualityManagementPlan
            {
                TrashCaptureStatusTypeID = TrashCaptureStatusType.NotProvided.TrashCaptureStatusTypeID,
                // KE 5/13/26 decision: default to Simplified rather than Detailed when creating a
                // new WQMP — most new entries don't justify a full detailed-modeling parameterization.
                WaterQualityManagementPlanModelingApproachID = WaterQualityManagementPlanModelingApproach.Simplified
                    .WaterQualityManagementPlanModelingApproachID
            };

            viewModel.UpdateModel(waterQualityManagementPlan);
            await _dbContext.WaterQualityManagementPlans.AddAsync(waterQualityManagementPlan);
            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay($"Successfully created \"{waterQualityManagementPlan.WaterQualityManagementPlanName}\".");

            return new ModalDialogFormJsonResult(
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        private PartialViewResult ViewNew(NewViewModel viewModel)
        {
            var stormwaterJurisdictions = new List<Role> {Role.Admin, Role.SitkaAdmin}.Contains(CurrentPerson.Role)
                ? StormwaterJurisdictions.List(_dbContext)
                : StormwaterJurisdictions.ListViewableByPersonForWQMPs(_dbContext, CurrentPerson);
            var hydrologicSubareas = _dbContext.HydrologicSubareas.ToList();
            var viewData = new NewViewData(stormwaterJurisdictions, hydrologicSubareas, TrashCaptureStatusType.All);
            return RazorPartialView<New, NewViewData, NewViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public PartialViewResult Edit([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var viewModel = new EditViewModel(waterQualityManagementPlan);
            return ViewEdit(viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> Edit([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEdit(viewModel);
            }

            // NPT-1051: Status transitions across the Active boundary need cleanup + re-solve;
            // legacy Edit form is the primary path admins use to flip Active <-> Inactive.
            var oldStatusID = waterQualityManagementPlan.WaterQualityManagementPlanStatusID;
            viewModel.UpdateModel(waterQualityManagementPlan);
            SetMessageForDisplay($"Successfully updated \"{waterQualityManagementPlan.WaterQualityManagementPlanName}\".");
            await _dbContext.SaveChangesAsync();

            if (await WaterQualityManagementPlans.HandleStatusTransitionAsync(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID, oldStatusID))
            {
                BackgroundJob.Enqueue<DeltaSolveJob>(x => x.RunJob());
            }

            return new ModalDialogFormJsonResult(
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        private PartialViewResult ViewEditNotes(EditNotesViewModel viewModel)
        {
            var viewData = new EditNotesViewData();
            return RazorPartialView<EditNotes, EditNotesViewData, EditNotesViewModel>(viewData, viewModel);
        }

        private PartialViewResult ViewEdit(EditViewModel viewModel)
        {
            var hydrologicSubareas = _dbContext.HydrologicSubareas.ToList();
            var viewData = new EditViewData(hydrologicSubareas, TrashCaptureStatusType.All);
            return RazorPartialView<Edit, EditViewData, EditViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public PartialViewResult EditNotes([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var viewModel = new EditNotesViewModel(waterQualityManagementPlan);
            return ViewEditNotes(viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditNotes([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditNotesViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditNotes(viewModel);
            }

            viewModel.UpdateModel(waterQualityManagementPlan);
            SetMessageForDisplay($"Successfully updated \"{waterQualityManagementPlan.WaterQualityManagementPlanName}\".");
            await _dbContext.SaveChangesAsync();

            return new ModalDialogFormJsonResult(
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public PartialViewResult Delete([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = waterQualityManagementPlanPrimaryKey.EntityObject;
            var viewModel = new ConfirmDialogFormViewModel(waterQualityManagementPlan.WaterQualityManagementPlanID);
            return ViewDelete(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> Delete([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewDelete(waterQualityManagementPlan, viewModel);
            }

            await NereidUtilities.MarkDownstreamNodeDirty(waterQualityManagementPlan, _dbContext);

            await waterQualityManagementPlan.DeleteFull(_dbContext);

            SetMessageForDisplay($"Successfully delete \"{waterQualityManagementPlan.WaterQualityManagementPlanName}\".");

            return new ModalDialogFormJsonResult();
        }

        private PartialViewResult ViewDelete(WaterQualityManagementPlan waterQualityManagementPlan,
            ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData(
                $"Are you sure you want to delete \"{waterQualityManagementPlan.WaterQualityManagementPlanName}\"?");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }

        #endregion

        #region WQMP Treatment BMPs
        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult EditTreatmentBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var treatmentBmpIDs = TreatmentBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID).Select(x => x.TreatmentBMPID).ToList();
            var viewModel = new EditTreatmentBMPsViewModel(treatmentBmpIDs);
            return ViewEditTreatmentBMPs(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditTreatmentBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditTreatmentBMPsViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditTreatmentBMPs(waterQualityManagementPlan, viewModel);
            }

            var treatmentBMPs = TreatmentBMPs.ListByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            viewModel.UpdateModel(waterQualityManagementPlan, _dbContext, treatmentBMPs);
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay($"Successfully updated BMPs for {waterQualityManagementPlan.WaterQualityManagementPlanName}");

            await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator, x => x.Detail(waterQualityManagementPlanPrimaryKey)));
        }

        private ViewResult ViewEditTreatmentBMPs(WaterQualityManagementPlan waterQualityManagementPlan,
            EditTreatmentBMPsViewModel viewModel)
        {
            var treatmentBmps = TreatmentBMPs.ListByStormwaterJurisdictionID(_dbContext, waterQualityManagementPlan.StormwaterJurisdictionID)
                .Where(x => x.WaterQualityManagementPlanID == null ||
                            x.WaterQualityManagementPlanID == waterQualityManagementPlan.WaterQualityManagementPlanID);
            var treatmentBMPModelingAttributes = vTreatmentBMPModelingAttributes.ListAsDictionary(_dbContext);
            var viewData = new EditTreatmentBMPsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan, treatmentBmps
                .Select(x => x.AsDisplayDto(treatmentBMPModelingAttributes.TryGetValue(x.TreatmentBMPID, out var attribute) ? attribute : null))
                .OrderBy(x => x.DisplayName)
                .ToList());
            return RazorView<EditTreatmentBMPs, EditTreatmentBMPsViewData, EditTreatmentBMPsViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult EditSimplifiedStructuralBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var quickBMPSimpleDtos = QuickBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID).Select(x => x.AsUpsertDto()).ToList();
            var viewModel = new EditSimplifiedStructuralBMPsViewModel(quickBMPSimpleDtos);
            return ViewEditSimplifiedStructuralBMPs(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditSimplifiedStructuralBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditSimplifiedStructuralBMPsViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditSimplifiedStructuralBMPs(waterQualityManagementPlan, viewModel);
            }

            var existingQuickBMPs = QuickBMPs.ListByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            viewModel.UpdateModel(waterQualityManagementPlan, _dbContext, existingQuickBMPs);
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay(
                $"Successfully updated BMPs for {waterQualityManagementPlan.WaterQualityManagementPlanName}");

            await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator, x => x.Detail(waterQualityManagementPlanPrimaryKey)));
        }

        private ViewResult ViewEditSimplifiedStructuralBMPs(WaterQualityManagementPlan waterQualityManagementPlan,
            EditSimplifiedStructuralBMPsViewModel viewModel)
        {
            var treatmentBMPTypes = TreatmentBMPTypes.List(_dbContext).Select(x => x.AsSimpleDto());
            var dryWeatherFlowOverrides = DryWeatherFlowOverride.All;
            var dryWeatherFlowOverrideDefaultID = DryWeatherFlowOverride.No.DryWeatherFlowOverrideID;
            var dryWeatherFlowOverrideYesID = DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideID;
            var viewData = new EditSimplifiedStructuralBMPsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan, treatmentBMPTypes, dryWeatherFlowOverrides, dryWeatherFlowOverrideDefaultID, dryWeatherFlowOverrideYesID);
            return RazorView<EditSimplifiedStructuralBMPs, EditSimplifiedStructuralBMPsViewData, EditSimplifiedStructuralBMPsViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult EditSourceControlBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var sourceControlBMPAttributes = SourceControlBMPAttributes.List(_dbContext);
            var sourceControlBMPs = SourceControlBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var sourceControlBMPUpsertDtos = new List<SourceControlBMPUpsertDto>();
            if (!sourceControlBMPs.Any())
            {
                sourceControlBMPUpsertDtos.AddRange(sourceControlBMPAttributes.OrderBy(x => x.SourceControlBMPAttributeID).Select(sourceControlBMPAttribute => sourceControlBMPAttribute.AsUpsertDto()));
            }
            else
            {
                sourceControlBMPUpsertDtos = sourceControlBMPs.Select(x => x.AsUpsertDto()).ToList();
                foreach (var sourceControlBmpAttributeCategoryID in SourceControlBMPAttributeCategory.All.Select(x => x.SourceControlBMPAttributeCategoryID))
                {
                    if (!sourceControlBMPUpsertDtos.Select(x => x.SourceControlBMPAttributeCategoryID).ToList()
                            .Contains(sourceControlBmpAttributeCategoryID))
                    {
                        sourceControlBMPUpsertDtos.AddRange(sourceControlBMPAttributes
                            .Where(x => x.SourceControlBMPAttributeCategoryID ==
                                        sourceControlBmpAttributeCategoryID)
                            .Select(x => x.AsUpsertDto()));
                    }
                }
            }
            var viewModel = new EditSourceControlBMPsViewModel(sourceControlBMPUpsertDtos);
            return ViewEditSourceControlBMPs(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditSourceControlBMPs([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditSourceControlBMPsViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditSourceControlBMPs(waterQualityManagementPlan, viewModel);
            }

            var existingSourceControlBMPs = SourceControlBMPs.ListByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            viewModel.UpdateModel(_dbContext, waterQualityManagementPlan, existingSourceControlBMPs);
            SetMessageForDisplay($"Successfully updated BMPs for {waterQualityManagementPlan.WaterQualityManagementPlanName}");

            await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator ,x => x.Detail(waterQualityManagementPlanPrimaryKey)));
        }

        private ViewResult ViewEditSourceControlBMPs(WaterQualityManagementPlan waterQualityManagementPlan,
            EditSourceControlBMPsViewModel viewModel)
        {
            var viewData = new EditSourceControlBMPsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan);
            return RazorView<EditSourceControlBMPs, EditSourceControlBMPsViewData, EditSourceControlBMPsViewModel>(viewData, viewModel);
        }


        #endregion

        #region WQMP Parcels

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult EditParcels([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var waterQualityManagementPlanParcels = WaterQualityManagementPlanParcels.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var parcelIDs = waterQualityManagementPlanParcels.Select(x => x.ParcelID).ToList();
            var viewModel = new EditParcelsViewModel(parcelIDs);
            return ViewEditParcels(waterQualityManagementPlan, viewModel, waterQualityManagementPlanParcels);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditParcels([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditParcelsViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            var waterQualityManagementPlanParcels = WaterQualityManagementPlanParcels.ListByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            if (!ModelState.IsValid)
            {
                return ViewEditParcels(waterQualityManagementPlan, viewModel, waterQualityManagementPlanParcels);
            }

            var waterQualityManagementPlanBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var oldBoundary = waterQualityManagementPlanBoundary?.GeometryNative;

            await viewModel.UpdateModels(waterQualityManagementPlan, _dbContext, waterQualityManagementPlanParcels, waterQualityManagementPlanBoundary);
            SetMessageForDisplay($"Successfully edited {FieldDefinitionType.Parcel.GetFieldDefinitionLabelPluralized()} for {FieldDefinitionType.WaterQualityManagementPlan.GetFieldDefinitionLabel()}.");
            await _dbContext.SaveChangesAsync();

            var newBoundary = waterQualityManagementPlan.WaterQualityManagementPlanBoundary?.GeometryNative;

            if (!(oldBoundary == null && newBoundary == null))
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldBoundary, newBoundary, _dbContext);
            }

            await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        private ViewResult ViewEditParcels(WaterQualityManagementPlan waterQualityManagementPlan, EditParcelsViewModel viewModel, ICollection<WaterQualityManagementPlanParcel> waterQualityManagementPlanParcels)
        {
            var wqmpParcelGeometries =
                waterQualityManagementPlanParcels.Select(x => x.Parcel.ParcelGeometry?.Geometry4326);
            var wqmpJurisdiction = waterQualityManagementPlan.StormwaterJurisdiction;
            var mapInitJson = new MapInitJson("editWqmpParcelMap", 0, new List<LayerGeoJson>(), wqmpParcelGeometries.Any() ?
                    new BoundingBoxDto(wqmpParcelGeometries) : new BoundingBoxDto(wqmpJurisdiction.StormwaterJurisdictionGeometry?.Geometry4326));
            var viewData = new EditParcelsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan, mapInitJson, _webConfiguration.MapServiceUrl, Constants.MapServiceLayerNameParcel, waterQualityManagementPlanParcels);
            return RazorView<EditParcels, EditParcelsViewData, EditParcelsViewModel>(viewData, viewModel);
        }

        #endregion


        private ViewResult ViewRefineArea(WaterQualityManagementPlan waterQualityManagementPlan, RefineAreaViewModel viewModel)
        {
            var waterQualityManagementPlanBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var geometry4326 = waterQualityManagementPlanBoundary?.Geometry4326;
            var featureCollection = geometry4326?.MultiPolygonToFeatureCollection();
            var boundingBoxDto = new BoundingBoxDto(geometry4326);
            var mapInitJson = new MapInitJson("EditWQMPBoundaryMap", 10, new List<LayerGeoJson>(), boundingBoxDto);
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.EditWQMPBoundary);
            var viewData = new RefineAreaViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, waterQualityManagementPlan, mapInitJson, _webConfiguration.MapServiceUrl, featureCollection);
            return RazorView<RefineArea, RefineAreaViewData, RefineAreaViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult RefineArea([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var viewModel = new RefineAreaViewModel();
            return ViewRefineArea(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> RefineArea([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, RefineAreaViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewRefineArea(waterQualityManagementPlan, viewModel);
            }

            var waterQualityManagementPlanBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanIDWithChangeTracking(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var oldBoundary = waterQualityManagementPlanBoundary?.GeometryNative;

            viewModel.UpdateModel(waterQualityManagementPlan, _dbContext, waterQualityManagementPlanBoundary);
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay($"Successfully edited boundary for {FieldDefinitionType.WaterQualityManagementPlan.GetFieldDefinitionLabel()}.");

            var newBoundary = waterQualityManagementPlanBoundary?.GeometryNative;

            if (!(oldBoundary == null && newBoundary == null))
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldBoundary, newBoundary, _dbContext);
            }

            await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator,
                x => x.Detail(waterQualityManagementPlan)));
        }
        #region WQMP O&M Verification Record




        [HttpGet("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public ViewResult WqmpVerify([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
        {
            var waterQualityManagementPlanVerify = WaterQualityManagementPlanVerifies.GetByID(_dbContext, waterQualityManagementPlanVerifyPrimaryKey);
            var waterQualityManagementPlanVerifyQuickBMPs =
                WaterQualityManagementPlanVerifyQuickBMPs.ListByWaterQualityManagementPlanVerifyID(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            var waterQualityManagementPlanVerifyTreatmentBMPs =
                WaterQualityManagementPlanVerifyTreatmentBMPs.ListByWaterQualityManagementPlanVerifyID(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);

            var viewData = new WqmpVerifyViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlanVerify, waterQualityManagementPlanVerifyQuickBMPs, waterQualityManagementPlanVerifyTreatmentBMPs);

            return RazorView<WqmpVerify, WqmpVerifyViewData>(viewData);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public ViewResult NewWqmpVerify([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var quickBMPs = QuickBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var treatmentBMPs = TreatmentBMPs.ListByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            var waterQualityManagementPlanVerify = new WaterQualityManagementPlanVerify
            {
                WaterQualityManagementPlanID = waterQualityManagementPlan.WaterQualityManagementPlanID,
                LastEditedByPersonID = CurrentPerson.PersonID,
                LastEditedDate = DateTime.UtcNow,
                IsDraft = true,
                // Default to today in Pacific time (the OC stormwater client locale) so a late-night
                // Pacific session doesn't pre-fill tomorrow's UTC date in the picker.
                VerificationDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time")))
            };
            var viewModel = new NewWqmpVerifyViewModel(waterQualityManagementPlan, waterQualityManagementPlanVerify, quickBMPs, treatmentBMPs);
            return ViewNewWqmpVerify(waterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> NewWqmpVerify([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, NewWqmpVerifyViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewNewWqmpVerify(waterQualityManagementPlan, viewModel);
            }

            var waterQualityManagementPlanVerify = new WaterQualityManagementPlanVerify{
                WaterQualityManagementPlanID = waterQualityManagementPlan.WaterQualityManagementPlanID,
                WaterQualityManagementPlanVerifyTypeID = viewModel.WaterQualityManagementPlanVerifyTypeID,
                WaterQualityManagementPlanVisitStatusID = viewModel.WaterQualityManagementPlanVisitStatusID,
                LastEditedByPersonID = CurrentPerson.PersonID,
                LastEditedDate = DateTime.UtcNow,
                IsDraft = !viewModel.HiddenIsFinalizeVerificationInput,
                VerificationDate = viewModel.VerificationDate
            };
            await _dbContext.WaterQualityManagementPlanVerifies.AddAsync(waterQualityManagementPlanVerify);
            await _dbContext.SaveChangesAsync();

            await viewModel.UpdateModel(waterQualityManagementPlanVerify, CurrentPerson, _dbContext, fileResourceService, new List<WaterQualityManagementPlanVerifyQuickBMP>(), new List<WaterQualityManagementPlanVerifyTreatmentBMP>());

            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay(
                $"Successfully updated {FieldDefinitionType.TreatmentBMP.GetFieldDefinitionLabelPluralized()} for {waterQualityManagementPlan.WaterQualityManagementPlanName}");

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        private ViewResult ViewNewWqmpVerify(WaterQualityManagementPlan waterQualityManagementPlan, NewWqmpVerifyViewModel viewModel)
        {
            var waterQualityManagementPlanVerifyTypes = WaterQualityManagementPlanVerifyType.All;
            var waterQualityManagementPlanVisitStatuses = WaterQualityManagementPlanVisitStatus.All;
            var waterQualityManagementPlanVerifyStatuses = WaterQualityManagementPlanVerifyStatus.All;
            var viewData = new NewWqmpVerifyViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan, waterQualityManagementPlanVerifyTypes, waterQualityManagementPlanVisitStatuses, waterQualityManagementPlanVerifyStatuses);
            return RazorView<NewWqmpVerify, NewWqmpVerifyViewData, NewWqmpVerifyViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public ViewResult EditWqmpVerify([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
        {
            var waterQualityManagementPlanVerify = WaterQualityManagementPlanVerifies.GetByID(_dbContext, waterQualityManagementPlanVerifyPrimaryKey);

            var waterQualityManagementPlanVerifyQuickBMPs =
                WaterQualityManagementPlanVerifyQuickBMPs.ListByWaterQualityManagementPlanVerifyID(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            var waterQualityManagementPlanVerifyTreatmentBMPs =
                WaterQualityManagementPlanVerifyTreatmentBMPs.ListByWaterQualityManagementPlanVerifyID(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);

            var viewModel = new EditWqmpVerifyViewModel(waterQualityManagementPlanVerify, waterQualityManagementPlanVerifyQuickBMPs, waterQualityManagementPlanVerifyTreatmentBMPs);
            return ViewEditWqmpVerify(waterQualityManagementPlanVerify.WaterQualityManagementPlan, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public async Task<IActionResult> EditWqmpVerify([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey, EditWqmpVerifyViewModel viewModel)
        {
            var waterQualityManagementPlanVerify = WaterQualityManagementPlanVerifies.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanVerifyPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditWqmpVerify(waterQualityManagementPlanVerify.WaterQualityManagementPlan, viewModel);
            }
            var waterQualityManagementPlan = waterQualityManagementPlanVerify.WaterQualityManagementPlan;
            waterQualityManagementPlanVerify.IsDraft = !viewModel.HiddenIsFinalizeVerificationInput;
            var waterQualityManagementPlanVerifyQuickBMPs = WaterQualityManagementPlanVerifyQuickBMPs.ListByWaterQualityManagementPlanVerifyIDWithChangeTracking(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            var waterQualityManagementPlanVerifyTreatmentBMPs = WaterQualityManagementPlanVerifyTreatmentBMPs.ListByWaterQualityManagementPlanVerifyIDWithChangeTracking(_dbContext, waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            await viewModel.UpdateModel(waterQualityManagementPlanVerify, CurrentPerson, _dbContext, fileResourceService, waterQualityManagementPlanVerifyQuickBMPs, waterQualityManagementPlanVerifyTreatmentBMPs);

            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay(
                $"Successfully updated Verification for {waterQualityManagementPlan.WaterQualityManagementPlanName}");

            return RedirectToAction(new SitkaRoute<WaterQualityManagementPlanController>(_linkGenerator, x => x.Detail(waterQualityManagementPlan)));
        }

        private ViewResult ViewEditWqmpVerify(WaterQualityManagementPlan waterQualityManagementPlan, EditWqmpVerifyViewModel viewModel)
        {
            var waterQualityManagementPlanVerifyTypes = WaterQualityManagementPlanVerifyType.All;
            var waterQualityManagementPlanVisitStatuses = WaterQualityManagementPlanVisitStatus.All;
            var waterQualityManagementPlanVerifyStatuses = WaterQualityManagementPlanVerifyStatus.All;
            var viewData = new EditWqmpVerifyViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, waterQualityManagementPlan, waterQualityManagementPlanVerifyTypes, waterQualityManagementPlanVisitStatuses, waterQualityManagementPlanVerifyStatuses);
            return RazorView<EditWqmpVerify, EditWqmpVerifyViewData, EditWqmpVerifyViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public PartialViewResult DeleteVerify([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
        {
            var waterQualityManagementPlanVerify = waterQualityManagementPlanVerifyPrimaryKey.EntityObject;
            var viewModel = new ConfirmDialogFormViewModel(waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            return ViewDeleteVerify(waterQualityManagementPlanVerify, viewModel);
        }

        [HttpPost("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public async Task<IActionResult> DeleteVerify([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var waterQualityManagementPlanVerify = waterQualityManagementPlanVerifyPrimaryKey.EntityObject;
            if (!ModelState.IsValid)
            {
                return ViewDeleteVerify(waterQualityManagementPlanVerify, viewModel);
            }

            var lastEditedDate = waterQualityManagementPlanVerify.LastEditedDate.ToShortDateString();

            await waterQualityManagementPlanVerify.DeleteFull(_dbContext);

            SetMessageForDisplay($"Successfully deleted \"{lastEditedDate}\".");

            return new ModalDialogFormJsonResult();
        }

        private PartialViewResult ViewDeleteVerify(WaterQualityManagementPlanVerify waterQualityManagementPlanVerify, ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData(
                $"Are you sure you want to delete the O&M Verification last edited on {waterQualityManagementPlanVerify.LastEditedDate.ToShortDateString()}?");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }


        [HttpGet("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyManageFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public PartialViewResult EditWqmpVerifyModal([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
        {
            var waterQualityManagementPlanVerify = waterQualityManagementPlanVerifyPrimaryKey.EntityObject;
            var viewModel = new ConfirmDialogFormViewModel(waterQualityManagementPlanVerify.WaterQualityManagementPlanVerifyID);
            return ViewEditWqmpVerifyModal(viewModel);
        }

        [HttpPost("{waterQualityManagementPlanVerifyPrimaryKey}")]
        [WaterQualityManagementPlanVerifyDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanVerifyPrimaryKey")]
        public ActionResult EditWqmpVerifyModal([FromRoute] WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewEditWqmpVerifyModal(viewModel);
            }

            return new ModalDialogFormJsonResult(
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.EditWqmpVerify(waterQualityManagementPlanVerifyPrimaryKey)));
        }

        private PartialViewResult ViewEditWqmpVerifyModal(ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData(
                "There is a verification in progress. Click OK to resume the existing verification record. Alternately, delete the in-progress verification from the verification panel on the WQMP page");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }
        #endregion

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public PartialViewResult EditModelingApproach([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var viewModel = new EditModelingApproachViewModel(waterQualityManagementPlan);
            return ViewEditModelingApproach(viewModel);
        }

        [HttpPost("{waterQualityManagementPlanPrimaryKey}")]
        [WaterQualityManagementPlanViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public async Task<IActionResult> EditModelingApproach([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey, EditModelingApproachViewModel viewModel)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDWithChangeTracking(_dbContext, waterQualityManagementPlanPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewEditModelingApproach(viewModel);
            }
            viewModel.UpdateModel(waterQualityManagementPlan);
            await _dbContext.SaveChangesAsync();

            var waterQualityManagementPlanBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(_dbContext, waterQualityManagementPlan.WaterQualityManagementPlanID);
            if (waterQualityManagementPlanBoundary != null)
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(waterQualityManagementPlanBoundary.GeometryNative, null, _dbContext);
                await NereidUtilities.MarkWqmpDirty(waterQualityManagementPlan, _dbContext);
            }

            SetMessageForDisplay($"Modeling Approach successfully changed for {waterQualityManagementPlan.WaterQualityManagementPlanName}.");
            return new ModalDialogFormJsonResult();
        }

        private PartialViewResult ViewEditModelingApproach(EditModelingApproachViewModel viewModel)
        {
            var viewData = new EditModelingApproachViewData(WaterQualityManagementPlanModelingApproach.All);
            return RazorPartialView<EditModelingApproach, EditModelingApproachViewData, EditModelingApproachViewModel>(viewData, viewModel);
        }

        [HttpGet("{waterQualityManagementPlanPrimaryKey}")]
        [ValidateEntityExistsAndPopulateParameterFilter("waterQualityManagementPlanPrimaryKey")]
        public JsonResult GetModelResults([FromRoute] WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
        {
            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByID(_dbContext, waterQualityManagementPlanPrimaryKey);
            var modeledPerformanceResultSimple = new ModeledPerformanceResultDto(_dbContext, waterQualityManagementPlan);
            return Json(modeledPerformanceResultSimple);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult UploadWqmps()
        {
            var viewModel = new UploadWqmpsViewModel();
            var errorList = new List<string>();
            return ViewUploadWqmps(viewModel, errorList);
        }
        
        [HttpPost]
        [JurisdictionEditFeature]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<IActionResult> UploadWqmps(UploadWqmpsViewModel viewModel)
        {
            var uploadXlsxInputStream = viewModel.UploadXLSX.OpenReadStream();

            DataTable dataTableFromExcel;
            try
            {
                dataTableFromExcel = ExcelHelper.GetDataTableFromExcel(uploadXlsxInputStream, "WQMP");
            }
            catch (Exception e)
            {
                SetErrorForDisplay("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewUploadWqmps(viewModel, []);
            }

            var wqmps = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext,
                dataTableFromExcel, viewModel.StormwaterJurisdictionID, out var errorList);

            if (errorList.Any())
            {
                return ViewUploadWqmps(viewModel, errorList);
            }

            var wqmpsAdded = wqmps.Where(x => !ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();
            var wqmpsUpdated = wqmps.Where(x => ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();

            await _dbContext.WaterQualityManagementPlans.AddRangeAsync(wqmpsAdded);
            await _dbContext.SaveChangesAsync();

            var message = $"Upload Successful: {wqmpsAdded.Count} records added, {wqmpsUpdated.Count} records updated!";
            SetMessageForDisplay(message);
            return new RedirectResult(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()));
        }

        private ViewResult ViewUploadWqmps(UploadWqmpsViewModel viewModel, List<string> errorList)
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.UploadWQMPs);
            var viewData = new UploadWqmpsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, errorList, neptunePage,
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.UploadWqmps()),
                StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson));
            return RazorView<UploadWqmps, UploadWqmpsViewData, UploadWqmpsViewModel>(viewData,
                viewModel);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<FileResult> UploadWQMPTemplate()
        {
            using var disposableTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".xlsx");
            await azureBlobStorageService.DownloadBlobToFileAsync(_webConfiguration.PathToBulkUploadWQMPTemplate, disposableTempFile.FileInfo.FullName);
            using var workbook = new XLWorkbook(disposableTempFile.FileInfo.FullName);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"UploadWQMPTemplate_{CurrentPerson.LastName}{CurrentPerson.FirstName}.xlsx");
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult UploadSimplifiedBMPs()
        {
            var viewModel = new UploadSimplifiedBMPsViewModel();
            var errorList = new List<string>();
            return ViewUploadSimplifiedBMPs(viewModel, errorList);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<IActionResult> UploadSimplifiedBMPs(UploadSimplifiedBMPsViewModel viewModel)
        {
            var uploadXlsxInputStream = viewModel.UploadXLSX.OpenReadStream();

            DataTable dataTableFromExcel;
            try
            {
                dataTableFromExcel = ExcelHelper.GetDataTableFromExcel(uploadXlsxInputStream, "BMP");
            }
            catch (Exception)
            {
                SetErrorForDisplay("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewUploadSimplifiedBMPs(viewModel, new List<string>());
            }

            var quickBMPs = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext,
                viewModel.StormwaterJurisdictionID, dataTableFromExcel, out var errorList);

            if (errorList.Any())
            {
                return ViewUploadSimplifiedBMPs(viewModel, errorList);
            }

            var quickBMPsAdded = quickBMPs.Where(x => !ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();
            var quickBMPsUpdated = quickBMPs.Where(x => ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();

            await _dbContext.QuickBMPs.AddRangeAsync(quickBMPsAdded);
            await _dbContext.SaveChangesAsync();

            var message = $"Upload Successful: {quickBMPsAdded.Count} records added, {quickBMPsUpdated.Count} records updated!";
            SetMessageForDisplay(message);
            return new RedirectResult(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()));
        }

        private ViewResult ViewUploadSimplifiedBMPs(UploadSimplifiedBMPsViewModel viewModel, List<string> errorList)
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.UploadSimplifiedBMPs);
            var viewData = new UploadSimplifiedBMPsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, errorList, neptunePage,
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.UploadWqmps()),
                StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson));
            return RazorView<UploadSimplifiedBMPs, UploadSimplifiedBMPsViewData, UploadSimplifiedBMPsViewModel>(viewData,
                viewModel);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<FileResult> SimplifiedBMPBulkUploadTemplate()
        {
            using var disposableTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".xlsx");
            await azureBlobStorageService.DownloadBlobToFileAsync(_webConfiguration.PathToSimplifiedBMPTemplate, disposableTempFile.FileInfo.FullName);
            using var workbook = new XLWorkbook(disposableTempFile.FileInfo.FullName);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SimplifiedBMPBulkUploadTemplate_{CurrentPerson.LastName}{CurrentPerson.FirstName}.xlsx");
        }


        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult UploadWqmpBoundaryFromAPNs()
        {
            var viewModel = new UploadWqmpBoundaryFromAPNsViewModel();
            var errorList = new List<string>();
            return ViewUploadWqmpBoundaryFromAPNs(viewModel, errorList);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<IActionResult> UploadWqmpBoundaryFromAPNs(UploadWqmpBoundaryFromAPNsViewModel viewModel)
        {
            var uploadedCSVFile = viewModel.UploadCSV;
            var stormwaterJurisdictionID = (int)viewModel.StormwaterJurisdictionID;
            var wqmpBoundaries = WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, uploadedCSVFile.OpenReadStream(), stormwaterJurisdictionID,
                out var errorList, out var missingApnList, out var oldBoundaries);

            if (errorList.Any())
            {
                return ViewUploadWqmpBoundaryFromAPNs(viewModel, errorList);
            }

            var wqmpBoundariesAdded = wqmpBoundaries.Where(x => !ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();
            var wqmpBoundariesUpdated = wqmpBoundaries.Where(x => ModelObjectHelpers.IsRealPrimaryKeyValue(x.PrimaryKey)).ToList();
            await _dbContext.WaterQualityManagementPlanBoundaries.AddRangeAsync(wqmpBoundariesAdded);
            await _dbContext.SaveChangesAsync();

            // queue LGU refresh for area covered by all wqmp boundaries
            var newBoundaryArea = wqmpBoundaries.Select(x => x.GeometryNative).ToList().UnionListGeometries();
            var oldBoundaryArea = oldBoundaries.UnionListGeometries();
            if (!(oldBoundaryArea == null && newBoundaryArea == null))
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldBoundaryArea, newBoundaryArea, _dbContext);
            }
            // mark each wqmp as dirty
            var dirtyModelNodes = new List<DirtyModelNode>();
            foreach (var wqmpBoundary in wqmpBoundaries)
            {
                var dirtyModelNode = new DirtyModelNode()
                {
                    CreateDate = DateTime.UtcNow,
                    WaterQualityManagementPlanID = wqmpBoundary.WaterQualityManagementPlanID
                };
                dirtyModelNodes.Add(dirtyModelNode);
            }
            await _dbContext.DirtyModelNodes.AddRangeAsync(dirtyModelNodes);
            await _dbContext.SaveChangesAsync();

            var stormwaterJurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization)
                .Single(x => x.StormwaterJurisdictionID == viewModel.StormwaterJurisdictionID)
                .GetOrganizationDisplayName();
            var missingApnMailMessage = string.Empty;
            if (missingApnList.Any())
            {
                missingApnMailMessage = $@"
<br /><br />
<div>The following APNs were not found in the system: {string.Join(", ", missingApnList.Distinct().OrderBy(x => x))}</div>
";
            }
            var body = $@"
<div>
The WQMP Boundaries for Stormwater Jurisdiction {stormwaterJurisdiction} were successfully update from the parcel geometries of the provided valid APNs.
{missingApnMailMessage}
</div>
";
            var mailMessage = new MailMessage
            {
                Subject = "WQMP Boundary Upload from APN List",
                Body = body,
                IsBodyHtml = true,
                From = new MailAddress(_webConfiguration.DoNotReplyEmail, "Orange County Stormwater Tools")
            };
            mailMessage.To.Add(CurrentPerson.Email);
            await sitkaSmtpClientService.Send(mailMessage);

            var message = $"Upload Successful: {wqmpBoundariesAdded.Count} WQMP Boundaries added, {wqmpBoundariesUpdated.Count} WQMP Boundaries updated!";
            SetMessageForDisplay(message);
            return new RedirectResult(SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()));
        }

        private ViewResult ViewUploadWqmpBoundaryFromAPNs(UploadWqmpBoundaryFromAPNsViewModel viewModel, List<string> errorList)
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WQMPBoundaryFromAPNList);
            var viewData = new UploadWqmpBoundaryFromAPNsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, errorList, neptunePage,
                SitkaRoute<WaterQualityManagementPlanController>.BuildUrlFromExpression(_linkGenerator, x => x.UploadWqmps()),
                StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson));
            return RazorView<UploadWqmpBoundaryFromAPNs, UploadWqmpBoundaryFromAPNsViewData, UploadWqmpBoundaryFromAPNsViewModel>(viewData,
                viewModel);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<FileResult> UploadWQMPBoundaryTemplate()
        {
            using var disposableTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".csv");
            await azureBlobStorageService.DownloadBlobToFileAsync(_webConfiguration.PathToUploadWQMPBoundaryTemplate, disposableTempFile.FileInfo.FullName);
            var stream = new FileStream(disposableTempFile.FileInfo.FullName, FileMode.Open);

            return File(stream, @"text/csv",
                $"UploadWQMPBoundaryTemplate_{CurrentPerson.LastName}{CurrentPerson.FirstName}.csv");
        }

        [HttpGet]
        public ViewResult WqmpModelingOptions()
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WQMPModelingOptions);
            var viewData = new WqmpModelingOptionsViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage);
            return RazorView<WqmpModelingOptions, WqmpModelingOptionsViewData>(viewData);
        }

        [HttpGet]
        [WaterQualityManagementPlanAnnualReportFeature]
        public ViewResult AnnualReport()
        {
            var approvalSummaryNeptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WQMPApprovalSummary);
            var postConstructionInspectionNeptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.WQMPPostConstructionInspectionAndVerification);

            var approvalSummaryGridSpec = new AnnualReportApprovalSummaryGridSpec(_linkGenerator);
            var postConstructionInspectionAndVerificationGridSpec = new AnnualReportPostConstructionInspectionAndVerificationGridSpec(_linkGenerator);
            var stormwaterJurisdictions = StormwaterJurisdictions.ListViewableByPersonForWQMPs(_dbContext, CurrentPerson);
            var viewData = new AnnualReportViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, approvalSummaryNeptunePage, postConstructionInspectionNeptunePage, approvalSummaryGridSpec, postConstructionInspectionAndVerificationGridSpec, stormwaterJurisdictions);
            return RazorView<AnnualReport, AnnualReportViewData>(viewData);
        }

        [HttpGet]
        [WaterQualityManagementPlanAnnualReportFeature]
        public GridJsonNetJObjectResult<vWaterQualityManagementPlanDetailed> AnnualReportApprovalSummaryGridData(int reportingYear, int stormwaterJurisdictionID)
        {
            var gridSpec = new AnnualReportApprovalSummaryGridSpec(_linkGenerator);

            var reportingPeriodStart = GetWQMPReportingPeriodStart(reportingYear);
            var reportingPeriodEnd = GetWQMPReportingPeriodEnd(reportingYear);

            var waterQualityManagementPlans =
                vWaterQualityManagementPlanDetaileds.ListViewableByPerson(_dbContext, CurrentPerson)
                    .Where(x => x.ApprovalDate >= reportingPeriodStart && x.ApprovalDate <= reportingPeriodEnd && (stormwaterJurisdictionID == -1 || x.StormwaterJurisdictionID == stormwaterJurisdictionID))
                    .OrderBy(x => x.WaterQualityManagementPlanName)
                    .ToList();
            
            var gridJsonNetJObjectResult = new GridJsonNetJObjectResult<vWaterQualityManagementPlanDetailed>(waterQualityManagementPlans, gridSpec);
            return gridJsonNetJObjectResult;
        }

        [HttpGet]
        [WaterQualityManagementPlanAnnualReportFeature]
        public GridJsonNetJObjectResult<PostConstructionInspectionAndVerificationGridSimple> AnnualReportPostConstructionInspectionAndVerificationGridData(int reportingYear, int stormwaterJurisdictionID)
        {
            var gridSpec = new AnnualReportPostConstructionInspectionAndVerificationGridSpec(_linkGenerator);

            var reportingPeriodStart = GetWQMPReportingPeriodStart(reportingYear);
            var reportingPeriodEnd = GetWQMPReportingPeriodEnd(reportingYear);

            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(_dbContext, CurrentPerson).ToList();

            var wqmpInventoryVerificationsAndFieldVisits = vWaterQualityManagementPlanAnnualReports
                .ListForStormwaterJurisdictionIDs(_dbContext, CurrentPerson, stormwaterJurisdictionIDsPersonCanView).Where(x =>
                    (stormwaterJurisdictionID == -1 || x.StormwaterJurisdictionID == stormwaterJurisdictionID) 
                    && x.WaterQualityManagementPlanVerifyVerificationDate >= DateOnly.FromDateTime(reportingPeriodStart) && x.WaterQualityManagementPlanVerifyVerificationDate <= DateOnly.FromDateTime(reportingPeriodEnd))
                .GroupBy(x => x.WaterQualityManagementPlanID);

            var postConstructionInspectionAndVerificationGridSimples = wqmpInventoryVerificationsAndFieldVisits.Select(
                x =>
                {
                    var wqmp = x.First();
                    var wqmpName = wqmp.WaterQualityManagementPlanName;
                    // count of BMPs is identical in all rows. Just take the first
                    

                    var mostRecentVerification =
                        x.OrderByDescending(y => y.WaterQualityManagementPlanVerifyVerificationDate).First();
                    var useTreatmentBMP = mostRecentVerification.WaterQualityManagementPlanVerifyTreatmentBMPCount.HasValue;// wqmp.TreatmentBMPCount.HasValue;
                    var numberOfBMPs = mostRecentVerification.WaterQualityManagementPlanVerifyTreatmentBMPCount ?? mostRecentVerification.WaterQualityManagementPlanVerifyQuickBMPCount;
                    var bmpsAdequate = useTreatmentBMP
                        ? mostRecentVerification.WaterQualityManagementPlanVerifyTreatmentBMPIsAdequateCount
                        : mostRecentVerification.WaterQualityManagementPlanVerifyQuickBMPIsAdequateCount;
                    var bmpsDeficient = useTreatmentBMP
                        ? mostRecentVerification.WaterQualityManagementPlanVerifyTreatmentBMPIsDeficientCount
                        : mostRecentVerification.WaterQualityManagementPlanVerifyQuickBMPIsDeficient;

                    var bmpNoteComments =
                        $"{(useTreatmentBMP ? mostRecentVerification.WaterQualityManagementPlanVerifyTreatmentBMPNotes : mostRecentVerification.WaterQualityManagementPlanVerifyQuickBMPNotes)}";
                    var comments =
                        $"{bmpNoteComments}{(string.IsNullOrWhiteSpace(bmpNoteComments) ? string.Empty : "; ")}{mostRecentVerification.EnforcementOrFollowupActions}";
                    return new PostConstructionInspectionAndVerificationGridSimple
                    {
                        WaterQualityManagementPlanID = x.Key,
                        WaterQualityManagementPlanName = wqmpName,
                        WaterQualityManagementPlanVerifyStatusName =
                            mostRecentVerification.WaterQualityManagementPlanVerifyStatusID.HasValue
                                ? WaterQualityManagementPlanVerifyStatus
                                    .AllLookupDictionary[
                                        mostRecentVerification.WaterQualityManagementPlanVerifyStatusID.Value]
                                    .WaterQualityManagementPlanVerifyStatusDisplayName
                                : string.Empty,
                        NumberOfBMPs = numberOfBMPs,
                        NumberOfBMPsAdequate = bmpsAdequate,
                        NumberOfBMPsDeficient = bmpsDeficient,
                        WQMPVerificationComments = comments
                    };
                }).OrderBy(x => x.WaterQualityManagementPlanName).ToList();

            var gridJsonNetJObjectResult = new GridJsonNetJObjectResult<PostConstructionInspectionAndVerificationGridSimple>(postConstructionInspectionAndVerificationGridSimples, gridSpec);
            return gridJsonNetJObjectResult;
        }

        private DateTime GetWQMPReportingPeriodStart(int reportingYear)
        {
            return new DateTime(reportingYear - 1, 7, 1);
        }

        private DateTime GetWQMPReportingPeriodEnd(int reportingYear)
        {
            return new DateTime(reportingYear, 6, DateTime.DaysInMonth(reportingYear, 6));
        }
    }
}
