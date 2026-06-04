/*-----------------------------------------------------------------------
<copyright file="LandUseBlockController.cs" company="Tahoe Regional Planning Agency">
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

using System.Net.Mail;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.Common.Email;
using Neptune.Common.Services;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Neptune.Jobs.Hangfire;
using Neptune.Common.Services;
using Neptune.WebMvc.Common;
using Neptune.Common.Services;
using Neptune.WebMvc.Security;
using Neptune.Common.Services;
using Neptune.WebMvc.Services;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.LandUseBlockGeometry;
using Neptune.Common.Services;
using NetTopologySuite.Features;

namespace Neptune.WebMvc.Controllers
{
    //[Area("Trash")]
    //[Route("[area]/[controller]/[action]", Name = "[area]_[controller]_[action]")]
    public class LandUseBlockGeometryController(
        NeptuneDbContext dbContext,
        ILogger<LandUseBlockGeometryController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        SitkaSmtpClientService sitkaSmtpClientService,
        GDALAPIService gdalApiService,
        AzureBlobStorageService azureBlobStorageService)
        : NeptuneBaseController<LandUseBlockGeometryController>(dbContext, logger, linkGenerator, webConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult UpdateLandUseBlockGeometry()
        {
            var viewModel = new UpdateLandUseBlockGeometryViewModel { PersonID = CurrentPerson.PersonID };
            return ViewUpdateLandUseBlockGeometry(viewModel);
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult DownloadLandUseBlockGeometry()
        {
            var viewModel = new DownloadLandUseBlockGeometryViewModel { PersonID = CurrentPerson.PersonID };
            return ViewDownloadLandUseBlockGeometry(viewModel);
        }

        [HttpPost]
        [JurisdictionEditFeature]
        public async Task<ActionResult> UpdateLandUseBlockGeometry(UpdateLandUseBlockGeometryViewModel viewModel)
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
            if (!ModelState.IsValid)
            {
                return ViewUpdateLandUseBlockGeometry(viewModel);
            }

            try
            {
                var columns = new List<string>
                {
                    $"{CurrentPerson.PersonID} as UploadedByPersonID",
                    "prioritylandusetype as PriorityLandUseType",
                    "landusedescription as LandUseDescription",
                    "trashgenerationrate as TrashGenerationRate",
                    "landusefortgr as LandUseForTGR",
                    "medianhouseholdincomeresidential as MedianHouseholdIncomeResidential",
                    "medianhouseholdincomeretail as MedianHouseholdIncomeRetail",
                    $"{viewModel.StormwaterJurisdictionID} as StormwaterJurisdictionID",
                    "permittype as PermitType",
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
                            FeatureLayerName = featureClassNames.Single().LayerName,
                            NumberOfSignificantDigits = 4,
                            Filter = "",
                            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                        }
                    }
                };
                var geoJson = await gdalApiService.Ogr2OgrGdbToGeoJson(apiRequest);
                var landUseBlockStagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<LandUseBlockStaging>(geoJson,
                    GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
                var validLandUseBlockStagings = landUseBlockStagings.Where(x => x.Geometry is { IsValid: true, Area: > 0 }).ToList();
                if (validLandUseBlockStagings.Any())
                {
                    await _dbContext.Database.ExecuteSqlAsync($"dbo.pLandUseBlockStagingDeleteByPersonID @PersonID = {CurrentPerson.PersonID}"); 
                    _dbContext.LandUseBlockStagings.AddRange(validLandUseBlockStagings);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Unrecognized field name",
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    ModelState.AddModelError("",
                        "The columns in the uploaded file did not match the LandUseBlock schema. Please ensure that every field in the schema is present and does not rely on an alias to meet the requirements. The file is invalid and cannot be uploaded.");
                }
                else
                {
                    ModelState.AddModelError("",
                        $"There was a problem processing the Feature Class \"{featureClassNames[0]}\". The file may be corrupted or invalid.");
                }
                return ViewUpdateLandUseBlockGeometry(viewModel);
            }

            try
            {
                var count = await _dbContext.LandUseBlockStagings.CountAsync();

                if (count > 0)
                {
                    BackgroundJob.Enqueue<LandUseBlockUploadBackgroundJob>(x => x.RunJob(CurrentPerson.PersonID));
                }
            }
            catch (Exception)
            {
                var body =
                    "There was an unexpected system error during processing of your Parcel Upload. The Orange County Stormwater Tools development team will investigate and be in touch when this issue is resolved.";

                var mailMessage = new MailMessage
                {
                    Subject = "Land Use Block Upload Error",
                    Body = body,
                    From = new MailAddress(_webConfiguration.DoNotReplyEmail, "Orange County Stormwater Tools")
                };

                mailMessage.To.Add(CurrentPerson.Email);
                await sitkaSmtpClientService.Send(mailMessage);

                throw;
            }

            SetMessageForDisplay("The Land Use Blocks were successfully added to the staging area. The staged Land Use Blocks will be processed and added to the system. You will receive an email notification when this process completes or if errors in the upload are discovered during processing.");

            return Redirect($"{_webConfiguration.TrashModuleBaseUrl}/land-use-blocks"); 
        }

        [HttpPost]
        [JurisdictionEditFeature]
        public async Task<ActionResult> DownloadLandUseBlockGeometry(DownloadLandUseBlockGeometryViewModel viewModel)
        {
            var stormwaterJurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization)
                .Single(x => x.StormwaterJurisdictionID == viewModel.StormwaterJurisdictionID)
                .GetOrganizationDisplayName();
            await using var stream = new MemoryStream();

            var featureCollection = new FeatureCollection();
            var landUseBlocks = _dbContext.LandUseBlocks
                .Include(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Include(x => x.TrashGeneratingUnits)
                .Where(x =>
                    x.StormwaterJurisdictionID == viewModel.StormwaterJurisdictionID).ToList();

            foreach (var landUseBlock in landUseBlocks)
            {
                var attributesTable = new AttributesTable
            {
                { "LandUseBlockID", landUseBlock.LandUseBlockID },
                { "PriorityLandUseType", PriorityLandUseType.AllLookupDictionary[(int)landUseBlock.PriorityLandUseTypeID].PriorityLandUseTypeDisplayName},
                { "BlockArea", landUseBlock.LandUseBlockGeometry.Area * Constants.SquareMetersToAcres },
                { "LandUseDescription", landUseBlock.LandUseDescription },
                { "TrashGenerationRate", landUseBlock.TrashGenerationRate },
                { "TrashResultsArea", (landUseBlock.TrashGeneratingUnits.Sum(y => y.TrashGeneratingUnitGeometry.Area) *
                                       Constants.SquareMetersToAcres) },
                { "LandUseForTGR", landUseBlock.LandUseForTGR },
                { "MedianHouseholdIncomeRetail", landUseBlock.MedianHouseholdIncomeRetail },
                { "MedianHouseholdIncomeResidential", landUseBlock.MedianHouseholdIncomeResidential },
                { "StormwaterJurisdiction", landUseBlock.StormwaterJurisdiction.GetOrganizationDisplayName() },
                { "PermitType", PermitType.AllLookupDictionary[landUseBlock.PermitTypeID].PermitTypeDisplayName }
            };
                var feature = new Feature(landUseBlock.LandUseBlockGeometry, attributesTable);
                featureCollection.Add(feature);
            }

            if (featureCollection.Count == 0)
            {
                var attributesTable = new AttributesTable
                {
                    { "LandUseBlockID", null },
                    { "PriorityLandUseType", null},
                    { "BlockArea", null },
                    { "LandUseDescription", null },
                    { "TrashGenerationRate", null },
                    { "TrashResultsArea", null },
                    { "LandUseForTGR", null },
                    { "MedianHouseholdIncomeRetail", null },
                    { "MedianHouseholdIncomeResidential", null },
                    { "StormwaterJurisdiction", null },
                    { "PermitType", null }
                };
                var feature = new Feature(null, attributesTable);
                featureCollection.Add(feature);
            }

            await GeoJsonSerializer.SerializeAsGeoJsonToStream(featureCollection,
                GeoJsonSerializer.DefaultSerializerOptions, stream);

            var jurisdictionName = stormwaterJurisdiction.Replace(' ', '-');

            var gdbInput = new GdbInput()
            {
                FileContents = stream.ToArray(),
                LayerName = "land-use-blocks",
                CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                GeometryTypeName = "POLYGON",
            };
            var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(new GdbInputsToGdbRequestDto()
            {
                GdbInputs = new List<GdbInput> { gdbInput },
                GdbName = $"{jurisdictionName}-land-use-blocks-export"
            });

            return File(bytes, "application/zip", $"{jurisdictionName}-land-use-block.zip");
        }

        private ViewResult ViewDownloadLandUseBlockGeometry(DownloadLandUseBlockGeometryViewModel viewModel)
        {
            var newGisDownloadUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.DownloadLandUseBlockGeometry());
            var downloadLandUseBlockUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.UpdateLandUseBlockGeometry());

            var viewData = new DownloadLandUseBlockGeometryViewData(HttpContext, _linkGenerator, CurrentPerson, _webConfiguration, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson), downloadLandUseBlockUrl);
            return RazorView<DownloadLandUseBlockGeometry, DownloadLandUseBlockGeometryViewData, DownloadLandUseBlockGeometryViewModel>(viewData, viewModel);
        }

        private ViewResult ViewUpdateLandUseBlockGeometry(UpdateLandUseBlockGeometryViewModel viewModel)
        {
            var newGisUploadUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.UpdateLandUseBlockGeometry());
            var downloadLandUseBuildUrl = SitkaRoute<LandUseBlockGeometryController>.BuildUrlFromExpression(_linkGenerator, x => x.DownloadLandUseBlockGeometry());

            var viewData = new UpdateLandUseBlockGeometryViewData(HttpContext, _linkGenerator, CurrentPerson, _webConfiguration, newGisUploadUrl, downloadLandUseBuildUrl, StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson));
            return RazorView<UpdateLandUseBlockGeometry, UpdateLandUseBlockGeometryViewData, UpdateLandUseBlockGeometryViewModel>(viewData, viewModel);
        }
    }
}
