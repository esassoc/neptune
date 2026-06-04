/*-----------------------------------------------------------------------
<copyright file="DelineationController.cs" company="Tahoe Regional Planning Agency">
Copyright (c) Tahoe Regional Planning Agency. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Models;
using Neptune.WebMvc.Security;
using Neptune.WebMvc.Services;
using NetTopologySuite.Features;
using Neptune.WebMvc.Views.DelineationGeometry;

namespace Neptune.WebMvc.Controllers
{
    public class DelineationGeometryController(
        NeptuneDbContext dbContext,
        ILogger<DelineationGeometryController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        AzureBlobStorageService azureBlobStorageService,
        GDALAPIService gdalApiService)
        : NeptuneBaseController<DelineationGeometryController>(dbContext, logger, linkGenerator, webConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult UpdateDelineationGeometry()
        {
            var viewModel = new UpdateDelineationGeometryViewModel(){ TreatmentBMPNameField = "TreatmentBMPName", DelineationStatusField = "DelineationStatus" };
            return ViewUpdateDelineationGeometry(viewModel);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        public async Task<ActionResult> UpdateDelineationGeometry(UpdateDelineationGeometryViewModel viewModel)
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
            //if (!OgrInfoCommandLineRunner.ConfirmAttributeExistsOnFeatureClass(
            //        new FileInfo(NeptuneWebConfiguration.OgrInfoExecutable),
            //        gdbFile,
            //        Ogr2OgrCommandLineRunner.DefaultTimeOut, featureClassName, TreatmentBMPNameField))
            //{
            //    errors.Add(new ValidationResult($"The feature class in the file geodatabase does not have an attribute named {TreatmentBMPNameField}. Please double-check the attribute name you entered and try again."));
            //    return errors;
            //}

            if (!ModelState.IsValid)
            {
                return ViewUpdateDelineationGeometryErrors(viewModel);
            }

            try
            {
                var columns = new List<string>
                {
                    $"{CurrentPerson.PersonID} as UploadedByPersonID",
                    $"{viewModel.StormwaterJurisdictionID} as StormwaterJurisdictionID",
                    $"{viewModel.TreatmentBMPNameField} as TreatmentBMPName"
                };
                if (!string.IsNullOrWhiteSpace(viewModel.DelineationStatusField))
                {
                    columns.Add($"{viewModel.DelineationStatusField} as DelineationStatus");
                }
                
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
                var delineationStagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<DelineationStaging>(geoJson,
                    GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
                // todo: Run MakeValid "update dbo.DelineationStaging set Geometry = Geometry.MakeValid() where Geometry.STIsValid() = 0";

                var validDelineationStagings = delineationStagings.Where(x => x.Geometry is { IsValid: true, Area: > 0 }).ToList();

                var centralizedDelineations = _dbContext.Delineations.AsNoTracking()
                    .Where(x => x.DelineationTypeID == (int)DelineationTypeEnum.Centralized)
                    .Include(x => x.TreatmentBMP).Select(x => x.TreatmentBMP.TreatmentBMPName).ToList();

                var centralized = validDelineationStagings.Select(x => x.TreatmentBMPName).Intersect(centralizedDelineations).ToList();
                if (centralized.Any())
                {
                    throw new Exception(
                        $"This file contains the following treatment BMPs that have centralized delineations: {string.Join(", ", centralized)}. The file is invalid and cannot be uploaded.");
                }
                if (validDelineationStagings.Any())
                {
                    await _dbContext.DelineationStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ExecuteDeleteAsync();
                    _dbContext.DelineationStagings.AddRange(validDelineationStagings);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                //Unsure that the message is this transparent outside of running locally, but worth having
                if (e.Message.Contains("Unrecognized field name",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    ModelState.AddModelError("",
                        "The columns in the uploaded file did not match the Delineation schema. The file is invalid and cannot be uploaded. Ensure that your field names entered above match the GDB exactly, and if DelineationStatus is not present in the GDB ensure that field is left blank.");
                }
                else if (e.Message.Contains("Centralized",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    ModelState.AddModelError("", e.Message);
                } else if (e is DbUpdateException && e.InnerException != null && e.InnerException.Message.Contains("Violation of UNIQUE KEY constraint 'AK_DelineationStaging_TreatmentBMPName_StormwaterJurisdictionID'. Cannot insert duplicate key in object 'dbo.DelineationStaging'. The duplicate key value is "))
                {
                    var start = e.InnerException.Message.IndexOf('(') + 1;
                    var end = e.InnerException.Message.IndexOf(',');
                    var duplicateBMP = e.InnerException.Message.Substring(start, end-start);
                    ModelState.AddModelError("",
                        $"The Treatment BMP Name field must contain unique values. There was at least one duplicated Treatment BMP Name: {duplicateBMP}");
                }
                else
                {
                    ModelState.AddModelError("",
                        $"There was a problem processing the Feature Class \"{featureClassName}\". The file may be corrupted or invalid. Ensure that your field names entered above match the GDB exactly, and if DelineationStatus is not present in the GDB ensure that field is left blank.");
                }
                return ViewUpdateDelineationGeometryErrors(viewModel);
            }

            var stormwaterJurisdictionID = viewModel.StormwaterJurisdictionID.GetValueOrDefault();
            var treatmentBMPAKs = _dbContext.DelineationStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).Select(x => x.TreatmentBMPName).ToList();

            var treatmentBMPsWithUpstreamBMPAlreadySet = _dbContext.TreatmentBMPs.Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID &&
                treatmentBMPAKs.Contains(x.TreatmentBMPName) && x.UpstreamBMPID != null).ToList();

            if (treatmentBMPsWithUpstreamBMPAlreadySet.Any())
            {
                var namesOfInvalidBMPs = treatmentBMPsWithUpstreamBMPAlreadySet.Select(x=> x.TreatmentBMPName);

                namesOfInvalidBMPs.ToList().ForEach(x =>
                {
                    ModelState.AddModelError("Upstream BMP", $"Treatment BMP:{x} has an Upstream BMP and cannot accept delineations. Please either remove this Treatment BMP from your file or remove the Upstream BMP from the Treatment BMP and try again.");
                });
                return ViewUpdateDelineationGeometryErrors(viewModel);
            }

            return RedirectToAction(new SitkaRoute<DelineationGeometryController>(_linkGenerator, x => x.ApproveDelineationGisUpload()));
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult DownloadDelineationGeometry()
        {
            var viewModel = new DownloadDelineationGeometryViewModel();
            return ViewDownloadDelineationGeometry(viewModel);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        [Produces(@"application/zip")]
        public async Task<FileContentResult> DownloadDelineationGeometry(DownloadDelineationGeometryViewModel viewModel)
        {
            var stormwaterJurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization)
                .Single(x => x.StormwaterJurisdictionID == viewModel.StormwaterJurisdictionID)
                .GetOrganizationDisplayName();
            var featureCollection = new FeatureCollection();
            var delineations = _dbContext.Delineations.Include(x => x.TreatmentBMP)
                    .ThenInclude(x => x.StormwaterJurisdiction)
                        .ThenInclude(x => x.Organization)
                .Include(x => x.TreatmentBMP)
                    .ThenInclude(x => x.TreatmentBMPType)
                .Where(x =>
                        x.TreatmentBMP.ProjectID == null &&
                x.TreatmentBMP.StormwaterJurisdictionID == viewModel.StormwaterJurisdictionID &&
                x.DelineationTypeID == viewModel.DelineationTypeID).ToList();

            foreach (var delineation in delineations)
            {
                var attributesTable = new AttributesTable
                {
                    { "DelineationID", delineation.DelineationID },
                    { "TreatmentBMPName", delineation.TreatmentBMP.TreatmentBMPName },
                    { "Jurisdiction", delineation.TreatmentBMP.StormwaterJurisdiction.GetOrganizationDisplayName() },
                    { "BMPType", delineation.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName },
                    { "DelineationStatus", delineation.GetDelineationStatus() },
                    { "DelineationArea", delineation.GetDelineationArea() },
                    { "DateOfLastDelineationModification", delineation.DateLastModified },
                    { "DateOfLastDelineationVerification", delineation.DateLastVerified },
                };
                featureCollection.Add(new Feature(delineation.DelineationGeometry, attributesTable));
            }

            if (featureCollection.Count == 0)
            {
                var attributesTable = new AttributesTable
                {
                    { "DelineationID", null },
                    { "TreatmentBMPName", null },
                    { "Jurisdiction", null },
                    { "BMPType", null },
                    { "DelineationStatus", null },
                    { "DelineationArea", null },
                    { "DateOfLastDelineationModification", null },
                    { "DateOfLastDelineationVerification", null },
                };
                featureCollection.Add(new Feature(null, attributesTable));
            }

            var jurisdictionName = stormwaterJurisdiction.Replace(' ', '-');
            var delineationTypeName = DelineationType.AllLookupDictionary[(int)viewModel.DelineationTypeID]
                .DelineationTypeDisplayName;

            var gdbInput = new GdbInput()
            {
                FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
                LayerName = $"{delineationTypeName.ToLower()}-delineations",
                CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                GeometryTypeName = "POLYGON",
            };
            var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(new GdbInputsToGdbRequestDto()
            {
                GdbInputs = new List<GdbInput> { gdbInput },
                GdbName = $"{delineationTypeName.ToLower()}-{jurisdictionName}-delineation-export"
            });

            return File(bytes, "application/zip", $"{delineationTypeName.ToLower()}-{jurisdictionName}-delineation-export.zip");
        }


        private PartialViewResult ViewUpdateDelineationGeometryErrors(UpdateDelineationGeometryViewModel viewModel)
        {
            var viewData = new UpdateDelineationGeometryViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson,
                null, null, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), null);
            return RazorPartialView<UpdateDelineationGeometryErrors, UpdateDelineationGeometryViewData, UpdateDelineationGeometryViewModel>(viewData, viewModel);
        }

        private ViewResult ViewUpdateDelineationGeometry(UpdateDelineationGeometryViewModel viewModel)
        {
            var newGisUploadUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.UpdateDelineationGeometry());
            var approveGisUploadUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.ApproveDelineationGisUpload());
            var downloadGisUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.DownloadDelineationGeometry());

            var viewData = new UpdateDelineationGeometryViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson,
                newGisUploadUrl, approveGisUploadUrl, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), downloadGisUrl);
            return RazorView<UpdateDelineationGeometry, UpdateDelineationGeometryViewData, UpdateDelineationGeometryViewModel>(viewData, viewModel);
        }

        private ViewResult ViewDownloadDelineationGeometry(DownloadDelineationGeometryViewModel viewModel)
        {
            var newGisUploadUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.DownloadDelineationGeometry());
            var gisUploadUrl = SitkaRoute<DelineationGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.UpdateDelineationGeometry());

            var viewData = new DownloadDelineationGeometryViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson,
                newGisUploadUrl, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), gisUploadUrl, DelineationType.All);
            return RazorView<DownloadDelineationGeometry, DownloadDelineationGeometryViewData, DownloadDelineationGeometryViewModel>(viewData, viewModel);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ActionResult ApproveDelineationGisUpload()
        {
            var viewModel = new ApproveDelineationGisUploadViewModel();
            return ViewApproveDelineationGisUpload(viewModel);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        public async Task<ActionResult> ApproveDelineationGisUpload(ApproveDelineationGisUploadViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewUpdateDelineationGeometry(new UpdateDelineationGeometryViewModel());
            }

            var delineationStagings = _dbContext.DelineationStagings.AsNoTracking().Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ToList();
            var treatmentBMPNames = delineationStagings.Select(x => x.TreatmentBMPName).ToList();
            // Will break if there are multiple batches of staged uploads, which is precisely what we want to happen. 
            var stormwaterJurisdictionID = delineationStagings.Select(x => x.StormwaterJurisdictionID).Distinct().Single();
            var delineationsToDelete = _dbContext.Delineations.AsNoTracking().Include(x => x.TreatmentBMP)
                .Where(x => treatmentBMPNames.Contains(x.TreatmentBMP.TreatmentBMPName)).Select(x => x.DelineationID).ToList();
            foreach (var delineationID in delineationsToDelete)
            {
                await Delineation.DeleteFull(_dbContext, delineationID);
            }

            // Starting from the treatment BMP is kind of backwards, conceptually, but it's easier to read and write
            var treatmentBMPsToUpdate = _dbContext.TreatmentBMPs.Include(x => x.Delineation).Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && treatmentBMPNames.Contains(x.TreatmentBMPName)).ToList();

            var newDelineations = new List<Delineation>();
            foreach (var treatmentBMP in treatmentBMPsToUpdate)
            {
                var delineationStaging = delineationStagings.Single(z => treatmentBMP.TreatmentBMPName == z.TreatmentBMPName);

                var delineation = new Delineation
                {
                    HasDiscrepancies = false,
                    IsVerified = delineationStaging.DelineationStatus?.Trim().ToLower() == "verified",
                    DelineationTypeID = (int)DelineationTypeEnum.Distributed,
                    TreatmentBMPID = treatmentBMP.TreatmentBMPID,
                    DateLastModified = DateTime.UtcNow,
                    VerifiedByPersonID = CurrentPerson.PersonID,
                    DateLastVerified = DateTime.UtcNow,
                    DelineationGeometry4326 = delineationStaging.Geometry.ProjectTo4326(),
                    DelineationGeometry = delineationStaging.Geometry
                };
                newDelineations.Add(delineation);
            }
            _dbContext.Delineations.AddRange(newDelineations);

            var successfulUploadCount = (int?)treatmentBMPsToUpdate.Count;

            var stormwaterJurisdiction = _dbContext.StormwaterJurisdictions.AsNoTracking().Include(x => x.Organization)
                .Single(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
            var stormwaterJurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName();

            SetMessageForDisplay($"{successfulUploadCount} Delineations were successfully uploaded for Jurisdiction {stormwaterJurisdictionName}");

            await _dbContext.SaveChangesAsync();

            await _dbContext.DelineationStagings.Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ExecuteDeleteAsync();

            return RedirectToAction(new SitkaRoute<DelineationController>(_linkGenerator, x => x.DelineationMap(null)));
        }

        private PartialViewResult ViewApproveDelineationGisUpload(ApproveDelineationGisUploadViewModel viewModel)
        {
            var delineationStagings = _dbContext.DelineationStagings.Include(x => x.StormwaterJurisdiction)
                .Where(x => x.UploadedByPersonID == CurrentPerson.PersonID).ToList();

            var delineationUpoadGisReportFromStaging = DelineationUploadGisReportJsonResult.GetDelineationUploadGisReportFromStaging(_dbContext, CurrentPerson, delineationStagings);

            var viewData = new ApproveDelineationGisUploadViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, delineationUpoadGisReportFromStaging);
            return RazorPartialView<ApproveDelineationGisUpload, ApproveDelineationGisUploadViewData, ApproveDelineationGisUploadViewModel>(viewData, viewModel);

        }
    }
}