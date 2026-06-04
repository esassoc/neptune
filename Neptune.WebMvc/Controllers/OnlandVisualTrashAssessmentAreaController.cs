using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Neptune.WebMvc.Common;
using Neptune.Common.Services;
using Neptune.WebMvc.Models;
using Neptune.Common.Services;
using Neptune.WebMvc.Security;
using Neptune.Common.Services;
using Neptune.WebMvc.Services;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.OnlandVisualTrashAssessmentArea;
using Neptune.Common.Services;

namespace Neptune.WebMvc.Controllers
{
    public class OnlandVisualTrashAssessmentAreaController(
        NeptuneDbContext dbContext,
        ILogger<OnlandVisualTrashAssessmentAreaController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        AzureBlobStorageService azureBlobStorageService,
        GDALAPIService gdalApiService)
        : NeptuneBaseController<OnlandVisualTrashAssessmentAreaController>(dbContext, logger, linkGenerator,
            webConfiguration)
    {
        [HttpGet]
        [JurisdictionManageFeature]
        public ActionResult BulkUploadOVTAAreas()
        {
            var bulkUploadTrashScreenVisitViewModel = new BulkUploadOVTAAreasViewModel() { AreaName = "OVTAAreaName" };

            return ViewBulkUploadOTVAAreas(bulkUploadTrashScreenVisitViewModel);
        }

        private ViewResult ViewBulkUploadOTVAAreas(
            BulkUploadOVTAAreasViewModel bulkUploadTrashScreenVisitViewModel)
        {
            var newGisUploadUrl = SitkaRoute<OnlandVisualTrashAssessmentAreaController>.BuildUrlFromExpression(_linkGenerator, x => x.BulkUploadOVTAAreas());
            var approveGisUploadUrl = SitkaRoute<OnlandVisualTrashAssessmentAreaController>.BuildUrlFromExpression(_linkGenerator, x => x.ApproveOVTAAreaGisUpload());
            var downloadOVTAAreaUrl = SitkaRoute<OnlandVisualTrashAssessmentExportController>.BuildUrlFromExpression(_linkGenerator, x => x.ExportAssessmentGeospatialData());
            var bulkUploadTrashScreenVisitViewData = new BulkUploadOVTAAreasViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson,
                newGisUploadUrl, approveGisUploadUrl, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), downloadOVTAAreaUrl);

            return RazorView<BulkUploadOVTAAreas, BulkUploadOVTAAreasViewData,
                BulkUploadOVTAAreasViewModel>(bulkUploadTrashScreenVisitViewData,
                bulkUploadTrashScreenVisitViewModel);
        }

        [HttpPost]
        [JurisdictionManageFeature]
        public async Task<IActionResult> BulkUploadOVTAAreas(BulkUploadOVTAAreasViewModel viewModel)
        {
            var file = viewModel.FileResourceData;
            var blobName = Guid.NewGuid().ToString();
            await azureBlobStorageService.UploadToBlobStorage(await FileStreamHelpers.StreamToBytes(file), blobName, ".gdb");
            var featureClassNames = await gdalApiService.OgrInfoGdbToFeatureClassInfo(file);

            if (featureClassNames.Count == 0)
            {
                ModelState.AddModelError("FileResourceData",
                    "The file geodatabase contained no feature class. Please upload a file geodatabase containing exactly one feature class.");
            }
            else if (featureClassNames.Count > 1)
            {
                ModelState.AddModelError("FileResourceData",
                    "The file geodatabase contained more than one feature class. Please upload a file geodatabase containing exactly one feature class.");
            }

            var featureClassName = featureClassNames.Single().LayerName;

            if (!ModelState.IsValid)
            {
                return ViewUpdateOVTAAreaGeometryErrors(viewModel);
            }

            try
            {
                var columns = new List<string>
                {
                    $"{viewModel.StormwaterJurisdictionID} as StormwaterJurisdictionID",
                    $"{viewModel.AreaName} as AreaName",
                    $"Description",
                    $"{CurrentPerson.PersonID} as UploadedByPersonID"
                };

                var apiRequest = new GdbToGeoJsonRequestDto()
                {
                    BlobContainer = AzureBlobStorageService.BlobContainerName,
                    CanonicalName = blobName,
                    GdbLayerOutputs = new()
                    {
                        new()
                        {
                            Columns = columns,
                            FeatureLayerName = featureClassName,
                            NumberOfSignificantDigits = 4,
                            Filter = "",
                            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                        }
                    }
                };
                var geoJson = await gdalApiService.Ogr2OgrGdbToGeoJson(apiRequest);
                var ovtaAreaStagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<OnlandVisualTrashAssessmentAreaStaging>(geoJson,
                    GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);

                var validOVTAAreaStagings = ovtaAreaStagings.Where(x => x.Geometry is { IsValid: true, Area: > 0 }).ToList();
                if (validOVTAAreaStagings.Any())
                {
                    var duplicates = validOVTAAreaStagings.GroupBy(x => x.AreaName)
                        .Where(g => g.Count() > 1)
                        .Select(x => x.Key).ToList();
                    if (duplicates.Count > 0)
                    {
                        throw new Exception($"Duplicate OVTA Area Names: {String.Join(", ", duplicates)}");
                    }
                    await _dbContext.OnlandVisualTrashAssessmentAreaStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ExecuteDeleteAsync();
                    _dbContext.OnlandVisualTrashAssessmentAreaStagings.AddRange(validOVTAAreaStagings);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Unrecognized field name",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    ModelState.AddModelError("",
                        "The columns in the uploaded file did not match the OVTA area schema. The file is invalid and cannot be uploaded.");
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                    //ModelState.AddModelError("",
                    //    $"There was a problem processing the Feature Class \"{featureClassName}\". The file may be corrupted or invalid.");
                }
                return ViewUpdateOVTAAreaGeometryErrors(viewModel);
            }


            return RedirectToAction(new SitkaRoute<OnlandVisualTrashAssessmentAreaController>(_linkGenerator, x => x.ApproveOVTAAreaGisUpload()));
        }

        [HttpGet]
        [JurisdictionManageFeature]
        public ActionResult ApproveOVTAAreaGisUpload()
        {
            var viewModel = new ApproveOVTAAreaGisUploadViewModel();
            return ViewApproveOVTAAreaGisUpload(viewModel);
        }

        [HttpPost]
        [JurisdictionManageFeature]
        public async Task<ActionResult> ApproveOVTAAreaGisUpload(ApproveOVTAAreaGisUploadViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewBulkUploadOTVAAreas(new BulkUploadOVTAAreasViewModel());
            }

            try
            {
                var onlandVisualTrashAssessmentAreaStagings = _dbContext.OnlandVisualTrashAssessmentAreaStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ToList();

                var stormwaterJurisdictionID = onlandVisualTrashAssessmentAreaStagings.First().StormwaterJurisdictionID;
                var stormwaterJurisdiction = StormwaterJurisdictions.GetByID(_dbContext, stormwaterJurisdictionID);
                var stormwaterJurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName();

                var ovtaAreaNames = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                    .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
                    .Select(x => x.OnlandVisualTrashAssessmentAreaName).ToList();

                var ovtaAreaStaging = _dbContext.OnlandVisualTrashAssessmentAreaStagings.AsNoTracking().Where(x =>
                    x.StormwaterJurisdictionID == stormwaterJurisdictionID &&
                    x.UploadedByPersonID == CurrentPerson.PersonID).ToList();

                foreach (var ovtaArea in ovtaAreaStaging)
                {
                    if (ovtaAreaNames.Contains(ovtaArea.AreaName))
                    {
                        var existingOVTA = _dbContext.OnlandVisualTrashAssessmentAreas.Single(x =>
                            x.OnlandVisualTrashAssessmentAreaName == ovtaArea.AreaName &&
                            x.StormwaterJurisdictionID == stormwaterJurisdictionID);

                        existingOVTA.AssessmentAreaDescription = ovtaArea.Description;
                        existingOVTA.OnlandVisualTrashAssessmentAreaGeometry = ovtaArea.Geometry.ProjectTo2771();
                        existingOVTA.OnlandVisualTrashAssessmentAreaGeometry4326 = ovtaArea.Geometry.ProjectTo4326();
                    }
                    else
                    {
                        var newOVTA = new OnlandVisualTrashAssessmentArea()
                        {
                            OnlandVisualTrashAssessmentAreaName = ovtaArea.AreaName,
                            AssessmentAreaDescription = ovtaArea.Description,
                            StormwaterJurisdictionID = ovtaArea.StormwaterJurisdictionID,
                            OnlandVisualTrashAssessmentAreaGeometry = ovtaArea.Geometry,
                            OnlandVisualTrashAssessmentAreaGeometry4326 = ovtaArea.Geometry.ProjectTo4326()
                        };

                        _dbContext.OnlandVisualTrashAssessmentAreas.AddRange(newOVTA);
                    }
                }

                
                await _dbContext.SaveChangesAsync();

                await _dbContext.OnlandVisualTrashAssessmentAreaStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ExecuteDeleteAsync();

                var successfulUploadCount = (int?)ovtaAreaStaging.Count;

                SetMessageForDisplay($"{successfulUploadCount} OVTA areas were successfully uploaded for Jurisdiction {stormwaterJurisdictionName}");


                return new RedirectResult($"{_webConfiguration.TrashModuleBaseUrl}/onland-visual-trash-assessment-areas");
            }
            catch (Exception e)
            {
                //Unsure that the message is this transparent outside of running locally, but worth having
                if (e is DbUpdateException && e.InnerException != null && e.InnerException.Message.Contains("Violation of UNIQUE KEY constraint 'AK_OnlandVisualTrashAssessmentArea_OnlandVisualTrashAssessmentAreaName_StormwaterJurisdictionID'. Cannot insert duplicate key in object 'dbo.OnlandVisualTrashAssessmentArea'. The duplicate key value is"))
                {
                    var start = e.InnerException.Message.IndexOf('(') + 1;
                    var end = e.InnerException.Message.IndexOf(',');
                    var duplicateOVTAArea = e.InnerException.Message.Substring(start, end - start);
                    ModelState.AddModelError("",
                        $"The OVTA Area Name field must contain unique values. There was at least one duplicated OVTA Area Name: {duplicateOVTAArea}");
                }
                else
                {
                    ModelState.AddModelError("", e.Message);
                }

                return ViewBulkUploadOTVAAreas(new BulkUploadOVTAAreasViewModel() { AreaName = "OVTAAreaName" });
            }
        }

        private PartialViewResult ViewApproveOVTAAreaGisUpload(ApproveOVTAAreaGisUploadViewModel viewModel)
        {
            var onlandVisualTrashAssessmentAreaStagings = _dbContext.OnlandVisualTrashAssessmentAreaStagings.Include(x => x.StormwaterJurisdiction)
                .Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ToList();

            var ovtaAreaUploadGisReportFromStaging = OVTAAreaUploadGisReportJsonResult.GetOVTAAreaUploadGisReportFromStaging(_dbContext, CurrentPerson, onlandVisualTrashAssessmentAreaStagings);

            var viewData = new ApproveOVTAAreaGisUploadViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, ovtaAreaUploadGisReportFromStaging);
            return RazorPartialView<ApproveOVTAAreaGisUpload, ApproveOVTAAreaGisUploadViewData, ApproveOVTAAreaGisUploadViewModel>(viewData, viewModel);

        }

        private PartialViewResult ViewUpdateOVTAAreaGeometryErrors(BulkUploadOVTAAreasViewModel viewModel)
        {
            var viewData = new BulkUploadOVTAAreasViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson,
                null, null, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), null);
            return RazorPartialView<UpdateOVTAAreaGeometryErrors, BulkUploadOVTAAreasViewData, BulkUploadOVTAAreasViewModel>(viewData, viewModel);
        }

    }
}
