/*-----------------------------------------------------------------------
<copyright file="FieldVisitController.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
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

using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.Services;
using Neptune.WebMvc.Security;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.FieldVisit;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Neptune.Models.DataTransferObjects;
using Neptune.Common.Services;
using Neptune.WebMvc.Common;
using Neptune.Common.Services;
using Neptune.WebMvc.Common.MvcResults;
using Neptune.Common.Services;
using Neptune.WebMvc.Models;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.Shared;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.Shared.EditAttributes;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.Shared.Location;
using Neptune.Common.Services;
using Neptune.WebMvc.Views.Shared.ManagePhotosWithPreview;
using Neptune.Common.Services;
using FieldVisit = Neptune.EFModels.Entities.FieldVisit;
using FieldVisitSection = Neptune.EFModels.Entities.FieldVisitSection;
using Index = Neptune.WebMvc.Views.FieldVisit.Index;
using Neptune.WebMvc.Services.Filters;
using Neptune.Common.Services;
using Microsoft.Extensions.Options;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services;
using Neptune.WebMvc.Services;
using Neptune.Common.Services;
using ClosedXML.Excel;

namespace Neptune.WebMvc.Controllers
{
    public class FieldVisitController(
        NeptuneDbContext dbContext,
        ILogger<FieldVisitController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        FileResourceService fileResourceService,
        AzureBlobStorageService azureBlobStorageService)
        : NeptuneBaseController<FieldVisitController>(dbContext, logger, linkGenerator, webConfiguration)
    {
        [HttpGet]
        [FieldVisitViewFeature]
        public ViewResult Index()
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.FieldRecords);
            var maintenanceAttributeTypes =
                _dbContext.CustomAttributeTypes.Where(x => x.CustomAttributeTypePurposeID == CustomAttributeTypePurpose.Maintenance.CustomAttributeTypePurposeID).ToList();
            var viewData = new IndexViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, maintenanceAttributeTypes, _dbContext.TreatmentBMPAssessmentObservationTypes);
            return RazorView<Index, IndexViewData>(viewData);
        }

        [HttpGet("{treatmentBMPPrimaryKey}")]
        [AnonymousUnclassifiedFeature]
        [TreatmentBMPViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("treatmentBMPPrimaryKey")]
        public GridJsonNetJObjectResult<vFieldVisitDetailed> FieldVisitGridJsonData([FromRoute] TreatmentBMPPrimaryKey treatmentBMPPrimaryKey)
        {
            var treatmentBMP = TreatmentBMPs.GetByIDForFeatureContextCheck(_dbContext, treatmentBMPPrimaryKey.PrimaryKeyValue);
            var fieldVisits = vFieldVisitDetaileds.ListByTreatmentBMPID(_dbContext, treatmentBMP.TreatmentBMPID);
            var gridSpec = new FieldVisitGridSpec(CurrentPerson, true, _linkGenerator);
            var gridJsonNetJObjectResult = new GridJsonNetJObjectResult<vFieldVisitDetailed>(fieldVisits, gridSpec);
            return gridJsonNetJObjectResult;
        }

        [HttpGet]
        [FieldVisitViewFeature]
        public GridJsonNetJObjectResult<vFieldVisitDetailed> AllFieldVisitsGridJsonData()
        {
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(_dbContext, CurrentPerson);
            var fieldVisits = vFieldVisitDetaileds.ListForStormwaterJurisdictionIDs(_dbContext, stormwaterJurisdictionIDsPersonCanView);
            var gridSpec = new FieldVisitGridSpec(CurrentPerson, false, _linkGenerator);
            var gridJsonNetJObjectResult = new GridJsonNetJObjectResult<vFieldVisitDetailed>(fieldVisits, gridSpec);
            return gridJsonNetJObjectResult;
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitViewFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Detail([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var initialTreatmentBMPAssessment = treatmentBMPAssessments.SingleOrDefault(x => x.TreatmentBMPAssessmentTypeID == (int) TreatmentBMPAssessmentTypeEnum.Initial);
            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            var initialAssessmentTreatmentBMPAssessmentPhotos = initialTreatmentBMPAssessment == null ? new List<TreatmentBMPAssessmentPhoto>() : TreatmentBMPAssessmentPhotos.ListByTreatmentBMPAssessmentID(_dbContext, initialTreatmentBMPAssessment.TreatmentBMPAssessmentID);
            var initialAssessmentViewData = new AssessmentDetailViewData(_linkGenerator, CurrentPerson, initialTreatmentBMPAssessment, TreatmentBMPAssessmentTypeEnum.Initial, treatmentBMPType, initialAssessmentTreatmentBMPAssessmentPhotos);
            var postMaintenanceTreatmentBMPAssessment = treatmentBMPAssessments.SingleOrDefault(x => x.TreatmentBMPAssessmentTypeID == (int)TreatmentBMPAssessmentTypeEnum.PostMaintenance);
            var postMaintenanceTreatmentBMPAssessmentPhotos = postMaintenanceTreatmentBMPAssessment == null ? new List<TreatmentBMPAssessmentPhoto>() : TreatmentBMPAssessmentPhotos.ListByTreatmentBMPAssessmentID(_dbContext, postMaintenanceTreatmentBMPAssessment.TreatmentBMPAssessmentID);
            var postMaintenanceAssessmentViewData = new AssessmentDetailViewData(_linkGenerator, CurrentPerson, postMaintenanceTreatmentBMPAssessment, TreatmentBMPAssessmentTypeEnum.PostMaintenance, treatmentBMPType, postMaintenanceTreatmentBMPAssessmentPhotos);
            var customAttributes = CustomAttributes.ListByTreatmentBMPID(_dbContext, fieldVisit.TreatmentBMPID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new DetailViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, initialAssessmentViewData, postMaintenanceAssessmentViewData, customAttributes, treatmentBMPType, maintenanceRecord);
            return RazorView<Detail, DetailViewData>(viewData);
        }

        [HttpGet("{treatmentBMPPrimaryKey}")]
        [FieldVisitCreateFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("treatmentBMPPrimaryKey")]
        public PartialViewResult New([FromRoute] TreatmentBMPPrimaryKey treatmentBMPPrimaryKey)
        {
            var treatmentBMP = treatmentBMPPrimaryKey.EntityObject;
            var fieldVisit = FieldVisits.GetInProgressForTreatmentBMPIfAny(_dbContext, treatmentBMP.TreatmentBMPID);
            var viewModel = new NewFieldVisitViewModel(fieldVisit != null, fieldVisit?.FieldVisitTypeID ?? FieldVisitType.DryWeather.FieldVisitTypeID);
            return ViewNew(fieldVisit, viewModel);
        }

        private PartialViewResult ViewNew(FieldVisit? fieldVisit, NewFieldVisitViewModel viewModel)
        {
            var viewData = new NewFieldVisitViewData(fieldVisit);
            return RazorPartialView<NewFieldVisit, NewFieldVisitViewData, NewFieldVisitViewModel>(viewData, viewModel);
        }

        [HttpPost("{treatmentBMPPrimaryKey}")]
        [FieldVisitCreateFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("treatmentBMPPrimaryKey")]
        public async Task<IActionResult> New([FromRoute] TreatmentBMPPrimaryKey treatmentBMPPrimaryKey, NewFieldVisitViewModel viewModel)
        {
            var treatmentBMP = treatmentBMPPrimaryKey.EntityObject;
            var oldFieldVisit = FieldVisits.GetInProgressForTreatmentBMPIfAny(_dbContext, treatmentBMP.TreatmentBMPID);
            if (!ModelState.IsValid)
            {
                return ViewNew(oldFieldVisit, viewModel);
            }

            FieldVisit fieldVisit;
            var fieldVisitType = FieldVisitType.AllLookupDictionary[viewModel.FieldVisitTypeID.GetValueOrDefault()];
            if (viewModel.Continue == null)
            {
                fieldVisit = new FieldVisit
                {
                    TreatmentBMPID = treatmentBMP.TreatmentBMPID,
                    FieldVisitStatusID = FieldVisitStatus.InProgress.FieldVisitStatusID,
                    PerformedByPersonID = CurrentPerson.PersonID,
                    VisitDate = viewModel.FieldVisitDate.ConvertTimeFromPSTToUTC(),
                    FieldVisitTypeID = fieldVisitType.FieldVisitTypeID,
                    InventoryUpdated = false, IsFieldVisitVerified = false
                };
                await _dbContext.FieldVisits.AddAsync(fieldVisit);
            }
            else if (viewModel.Continue == false)
            {
                oldFieldVisit.FieldVisitStatusID = FieldVisitStatus.Unresolved.FieldVisitStatusID;
                fieldVisit = new FieldVisit
                {
                    TreatmentBMPID = treatmentBMP.TreatmentBMPID,
                    FieldVisitStatusID = FieldVisitStatus.InProgress.FieldVisitStatusID,
                    PerformedByPersonID = CurrentPerson.PersonID,
                    VisitDate = viewModel.FieldVisitDate.ConvertTimeFromPSTToUTC(),
                    FieldVisitTypeID = fieldVisitType.FieldVisitTypeID,
                    InventoryUpdated = false,
                    IsFieldVisitVerified = false
                };
                await _dbContext.FieldVisits.AddAsync(fieldVisit);
            }
            else // if Continue == true
            {
                fieldVisit = oldFieldVisit;
            }

            await _dbContext.SaveChangesAsync();

            return new ModalDialogFormJsonResult(
                SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Inventory(fieldVisit.FieldVisitID)));
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public PartialViewResult EditDateAndType([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = fieldVisitPrimaryKey.EntityObject;
            var viewModel = new EditDateAndTypeViewModel(fieldVisit);
            return ViewEditDateAndType(fieldVisit, viewModel);
        }

        private PartialViewResult ViewEditDateAndType(FieldVisit fieldVisit, EditDateAndTypeViewModel viewModel)
        {
            var viewData = new EditDateAndTypeViewData(fieldVisit);
            return RazorPartialView<EditDateAndType, EditDateAndTypeViewData, EditDateAndTypeViewModel>(viewData, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> EditDateAndType([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, EditDateAndTypeViewModel viewModel)
        {
            var fieldVisit = fieldVisitPrimaryKey.EntityObject;
            fieldVisit.FieldVisitTypeID = viewModel.FieldVisitTypeID;
            fieldVisit.VisitDate = viewModel.FieldVisitDate.ConvertTimeFromPSTToUTC();

            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay("Successfully updated Field Visit Date and Field Visit Type");
            //Because this could come from multiple places, look for where the modal was triggered from
            return new ModalDialogFormJsonResult(HttpContext.Request.GetReferrer());
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Inventory([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new InventoryViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord);
            return RazorView<Inventory, InventoryViewData>(viewData);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Inventory([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, InventoryViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }
            return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.VisitSummary(fieldVisit)));
        }

        private async Task<bool> FinalizeVisitIfNecessary(FieldVisitViewModel viewModel, FieldVisit fieldVisit)
        {
            if (viewModel.FinalizeVisit ?? false)
            {
                fieldVisit.FieldVisitStatusID = FieldVisitStatus.Complete.FieldVisitStatusID;
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Location([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new LocationViewModel(fieldVisit);
            return ViewLocation(fieldVisit, viewModel);
        }

        private ViewResult ViewLocation(FieldVisit fieldVisit, LocationViewModel viewModel)
        {
            var layerGeoJsons = MapInitJsonHelpers.GetJurisdictionMapLayers(_dbContext).ToList();
            var treatmentBMPLocationPoint4326 = fieldVisit.TreatmentBMP.LocationPoint4326;
            var boundingBox = treatmentBMPLocationPoint4326 != null
                ? new BoundingBoxDto(treatmentBMPLocationPoint4326)
                : new BoundingBoxDto();
            var mapInitJson =
                new MapInitJson($"BMP_{CurrentPerson.PersonID}_EditBMP", 10, layerGeoJsons, boundingBox, false)
                {
                    AllowFullScreen = false
                };
            var editLocationViewData = new EditLocationViewData(mapInitJson, "treatmentBMPLocation");
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new LocationViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, editLocationViewData, maintenanceRecord);

            return RazorView<Location, LocationViewData, LocationViewModel>(viewData, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Location([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, LocationViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewLocation(fieldVisit, viewModel);
            }
            fieldVisit.TreatmentBMP.MarkInventoryAsProvisionalIfNonManager(CurrentPerson);
            var delineation = Delineations.GetByTreatmentBMPIDWithChangeTracking(_dbContext, fieldVisit.TreatmentBMPID);
            viewModel.UpdateModel(_dbContext, fieldVisit.TreatmentBMP, CurrentPerson, delineation);
            fieldVisit.TreatmentBMP.SetTreatmentBMPPointInPolygonDataByLocationPoint(fieldVisit.TreatmentBMP.LocationPoint, _dbContext);
            fieldVisit.InventoryUpdated = true;
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }

            SetMessageForDisplay("Successfully updated Treatment BMP Location.");

            return RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.Location(fieldVisit)), new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.Photos(fieldVisit)), fieldVisit);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Photos([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPImages = TreatmentBMPImages.ListByTreatmentBMPID(_dbContext, fieldVisit.TreatmentBMPID);
            var viewModel = new PhotosViewModel(treatmentBMPImages);
            return ViewPhotos(fieldVisit, viewModel, treatmentBMPImages);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Photos([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, PhotosViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPImages = TreatmentBMPImages.ListByTreatmentBMPIDWithChangeTracking(_dbContext, fieldVisit.TreatmentBMPID);
            if (!ModelState.IsValid)
            {
                ViewPhotos(fieldVisit, viewModel, treatmentBMPImages);
            }

            await viewModel.UpdateModel(CurrentPerson, fieldVisit.TreatmentBMP, _dbContext, fileResourceService, treatmentBMPImages);
            fieldVisit.TreatmentBMP.MarkInventoryAsProvisionalIfNonManager(CurrentPerson);
            fieldVisit.InventoryUpdated = true;
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay("Successfully updated treatment BMP assessment photos.");
            return RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Photos(fieldVisit)), new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Attributes(fieldVisit)), fieldVisit);
        }

        private ViewResult ViewPhotos(FieldVisit fieldVisit, PhotosViewModel viewModel, List<TreatmentBMPImage> treatmentBMPImages)
        {
            var managePhotosWithPreviewViewData = new ManagePhotosWithPreviewViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, treatmentBMPImages);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var viewData = new PhotosViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, managePhotosWithPreviewViewData);
            return RazorView<Photos, PhotosViewData, PhotosViewModel>(viewData, viewModel);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Attributes([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var customAttributeUpsertDtos = CustomAttributes.ListByTreatmentBMPIDAndPurposes(_dbContext, fieldVisit.TreatmentBMPID, CustomAttributeTypePurposeEnum.OtherDesignAttributes).Select(x => x.AsUpsertDto()).ToList();
            var viewModel = new EditAttributesViewModel(customAttributeUpsertDtos);
            return ViewAttributes(fieldVisit, viewModel);
        }

        private ViewResult ViewAttributes(FieldVisit fieldVisit, EditAttributesViewModel viewModel)
        {
            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            var missingRequiredAttributes = treatmentBMPType.TreatmentBMPTypeCustomAttributeTypes.Any(x =>
            {
                var customAttributes = CustomAttributes.ListByTreatmentBMPID(_dbContext, fieldVisit.TreatmentBMPID);
                return x.CustomAttributeType.IsRequired && x.CustomAttributeType.CustomAttributeTypePurpose !=
                       CustomAttributeTypePurpose.Maintenance &&
                       !x.CustomAttributeType.IsCompleteForTreatmentBMP(customAttributes);
            });
            var editAttributesViewData = new EditAttributesViewData(treatmentBMPType, CustomAttributeTypePurposeEnum.OtherDesignAttributes, missingRequiredAttributes);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new AttributesViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord, editAttributesViewData);
            return RazorView<Attributes, AttributesViewData, EditAttributesViewModel>(viewData, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Attributes([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, EditAttributesViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewAttributes(fieldVisit, viewModel);
            }

            var allCustomAttributeTypes = CustomAttributeTypes.List(_dbContext);
            var existingCustomAttributes = CustomAttributes.ListByTreatmentBMPIDAndPurposesWithChangeTracking(_dbContext, fieldVisit.TreatmentBMPID, CustomAttributeTypePurposeEnum.OtherDesignAttributes);
            await viewModel.UpdateModel(_dbContext, fieldVisit.TreatmentBMP, existingCustomAttributes, allCustomAttributeTypes);
            fieldVisit.TreatmentBMP.MarkInventoryAsProvisionalIfNonManager(CurrentPerson);
            fieldVisit.InventoryUpdated = true;
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(
                    new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }

            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay("Successfully updated Treatment BMP Attributes.");
            return RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.Attributes(fieldVisit)), new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.Assessment(fieldVisit)), fieldVisit);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Assessment([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new AssessmentViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord);
            return RazorView<Assessment, AssessmentViewData>(viewData);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Assessment([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, FieldVisitViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            const TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum = TreatmentBMPAssessmentTypeEnum.Initial;
            return await SaveAssessmentImpl(viewModel, fieldVisit, treatmentBMPAssessmentTypeEnum);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult PostMaintenanceAssessment([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new PostMaintenanceAssessmentViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord);
            return RazorView<PostMaintenanceAssessment, PostMaintenanceAssessmentViewData>(viewData);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> PostMaintenanceAssessment([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, FieldVisitViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            const TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum = TreatmentBMPAssessmentTypeEnum.PostMaintenance;
            return await SaveAssessmentImpl(viewModel, fieldVisit, treatmentBMPAssessmentTypeEnum);
        }

        private async Task<IActionResult> SaveAssessmentImpl(FieldVisitViewModel viewModel, FieldVisit fieldVisit,
            TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
        {
            // check if we are wrapping up the visit
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }

            // we are not finalizing the visit, so we are beginning the assessment
            // if we don't already have one created now is the time
            if (fieldVisit.GetAssessmentByType(treatmentBMPAssessmentTypeEnum) == null)
            {
                var treatmentBMPAssessment = CreatePlaceholderTreatmentBMPAssessment(fieldVisit, treatmentBMPAssessmentTypeEnum);
                await _dbContext.TreatmentBMPAssessments.AddAsync(treatmentBMPAssessment);
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Observations(fieldVisit, treatmentBMPAssessmentTypeEnum)));
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult Maintain([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new MaintainViewModel();
            return ViewMaintain(fieldVisit, viewModel);
        }

        private ViewResult ViewMaintain(FieldVisit fieldVisit, MaintainViewModel viewModel)
        {
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new MaintainViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord);
            return RazorView<Maintain, MaintainViewData, MaintainViewModel>(viewData, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Maintain([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, MaintainViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewMaintain(fieldVisit, viewModel);
            }
            // check if we are wrapping up the visit
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit))
            {
                return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit)));
            }
            // we are not finalizing the visit, so we are beginning the maintenance
            // if we don't already have one created now is the time
            if (fieldVisit.MaintenanceRecord == null)
            {
                var maintenanceRecord = new MaintenanceRecord
                {
                    TreatmentBMPID = fieldVisit.TreatmentBMP.TreatmentBMPID,
                    TreatmentBMPTypeID = fieldVisit.TreatmentBMP.TreatmentBMPTypeID,
                    FieldVisitID = fieldVisit.FieldVisitID,
                    MaintenanceRecordTypeID = MaintenanceRecordType.Routine.MaintenanceRecordTypeID
                };
                await _dbContext.MaintenanceRecords.AddAsync(maintenanceRecord);
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.EditMaintenanceRecord(fieldVisitPrimaryKey)));
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ActionResult EditMaintenanceRecord([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var maintenanceRecord = fieldVisit.MaintenanceRecord;
            // need this check to support deleting maintenance records from the edit page
            if (maintenanceRecord == null)
            {
                return Redirect(SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Maintain(fieldVisitPrimaryKey)));
            }

            maintenanceRecord = MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord?.MaintenanceRecordID);
            var customAttributeUpsertDtos = maintenanceRecord.MaintenanceRecordObservations.Select(x => x.AsUpsertDto()).ToList();
            var viewModel = new EditMaintenanceRecordViewModel(maintenanceRecord, customAttributeUpsertDtos);
            return ViewEditMaintenanceRecord(viewModel, fieldVisit, maintenanceRecord);
        }

        private ViewResult ViewEditMaintenanceRecord(EditMaintenanceRecordViewModel viewModel, FieldVisit fieldVisit, MaintenanceRecord maintenanceRecord)
        {
            var missingRequiredAttributes = maintenanceRecord.IsMissingRequiredAttributes();
            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            var editMaintenanceRecordObservationsViewData = new EditAttributesViewData(treatmentBMPType, CustomAttributeTypePurposeEnum.Maintenance, missingRequiredAttributes);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var viewData = new EditMaintenanceRecordViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord, editMaintenanceRecordObservationsViewData);
            return RazorView<EditMaintenanceRecord, EditMaintenanceRecordViewData,
                EditMaintenanceRecordViewModel>(viewData, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> EditMaintenanceRecord([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, EditMaintenanceRecordViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            var maintenanceRecord = MaintenanceRecords.GetByIDWithChangeTracking(_dbContext, fieldVisit.MaintenanceRecord?.MaintenanceRecordID);
            if (!ModelState.IsValid)
            {
                return ViewEditMaintenanceRecord(viewModel, fieldVisit, maintenanceRecord);
            }

            fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(CurrentPerson);
            var allCustomAttributeTypes = CustomAttributeTypes.List(_dbContext);
            var existingMaintenanceRecordObservations =  maintenanceRecord.MaintenanceRecordObservations.Where(x =>
                x.CustomAttributeType.CustomAttributeTypePurposeID == CustomAttributeTypePurpose.Maintenance.CustomAttributeTypePurposeID).ToList();
            await viewModel.UpdateModel(_dbContext, maintenanceRecord, existingMaintenanceRecordObservations, allCustomAttributeTypes);
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit)) { return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit))); }

            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay($"{FieldDefinitionType.MaintenanceRecord.GetFieldDefinitionLabel()} successfully updated.");

            return RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.EditMaintenanceRecord(fieldVisit)), new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.PostMaintenanceAssessment(fieldVisit)), fieldVisit);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult VisitSummary([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            return ViewVisitSummary(new VisitSummaryViewModel(), fieldVisit);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> VisitSummary([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, VisitSummaryViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewVisitSummary(viewModel, fieldVisit);
            }

            fieldVisit.FieldVisitStatusID = FieldVisitStatus.Complete.FieldVisitStatusID;
            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay($"Successfully completed the Field Visit for {UrlTemplate.MakeHrefString(SitkaRoute<TreatmentBMPController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(fieldVisit.TreatmentBMPID)), fieldVisit.TreatmentBMP.TreatmentBMPName)}.");

            return RedirectToAction(new SitkaRoute<TreatmentBMPController>(_linkGenerator, x => x.FindABMP()));
        }

        private ViewResult ViewVisitSummary(VisitSummaryViewModel viewModel, FieldVisit fieldVisit)
        {
            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var customAttributes = CustomAttributes.ListByTreatmentBMPID(_dbContext, fieldVisit.TreatmentBMPID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new VisitSummaryViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, fieldVisit,
                treatmentBMPAssessments, maintenanceRecord, treatmentBMPType, customAttributes);
            return RazorView<VisitSummary, VisitSummaryViewData, VisitSummaryViewModel>(viewData, viewModel);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitVerifyFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public PartialViewResult VerifyFieldVisit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new ConfirmDialogFormViewModel(fieldVisit.FieldVisitID);
            return ViewVerifyFieldVisit(fieldVisit, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitVerifyFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> VerifyFieldVisit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewVerifyFieldVisit(fieldVisit, viewModel);
            }

            fieldVisit.VerifyFieldVisit();
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay("The Field Visit was successfully verified.");
            return new ModalDialogFormJsonResult(SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(fieldVisitPrimaryKey)));
        }

        private PartialViewResult ViewVerifyFieldVisit(FieldVisit fieldVisit, ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData($"Are you sure you want to verify the Assessment and Maintenance Records for the Field Visit to the treatment BMP '{fieldVisit.TreatmentBMP.TreatmentBMPName}' dated '{fieldVisit.VisitDate}'? ");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitVerifyFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public PartialViewResult MarkProvisionalFieldVisit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new ConfirmDialogFormViewModel(fieldVisit.FieldVisitID);
            return ViewMarkProvisionalFieldVisit(fieldVisit, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitVerifyFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> MarkProvisionalFieldVisit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewMarkProvisionalFieldVisit(fieldVisit, viewModel);
            }

            fieldVisit.MarkFieldVisitAsProvisional();
            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay("The Field Visit was successfully marked as provisional.");
            var redirectUrl =
                (fieldVisit.IsFieldVisitVerified || fieldVisit.FieldVisitStatus == FieldVisitStatus.Complete)
                    ? SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(fieldVisitPrimaryKey))
                    : SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Inventory(fieldVisitPrimaryKey));
            return new ModalDialogFormJsonResult(redirectUrl);
        }

        private PartialViewResult ViewMarkProvisionalFieldVisit(FieldVisit fieldVisit, ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData($"Are you sure you want to mark the Assessment and Maintenance Records as provisional for the Field Visit to the treatment BMP '{fieldVisit.TreatmentBMP.TreatmentBMPName}' dated '{fieldVisit.VisitDate}'? ");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitReturnToEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public PartialViewResult ReturnFieldVisitToEdit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new ConfirmDialogFormViewModel(fieldVisit.FieldVisitID);
            return ViewReturnFieldVisitToEdit(fieldVisit, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitReturnToEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> ReturnFieldVisitToEdit([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewReturnFieldVisitToEdit(fieldVisit, viewModel);
            }

            fieldVisit.ReturnFieldVisitToEdit();
            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay("The Field Visit was successfully returned to edit.");
            var redirectUrl =
                (fieldVisit.IsFieldVisitVerified || fieldVisit.FieldVisitStatus == FieldVisitStatus.Complete)
                    ? SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Detail(fieldVisitPrimaryKey))
                    : SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Inventory(fieldVisitPrimaryKey));
            return new ModalDialogFormJsonResult(redirectUrl);
        }

        private PartialViewResult ViewReturnFieldVisitToEdit(FieldVisit fieldVisit, ConfirmDialogFormViewModel viewModel)
        {
            var viewData = new ConfirmDialogFormViewData($"Are you sure you want to re-enable editing the Field Visit to the treatment BMP '{fieldVisit.TreatmentBMP.TreatmentBMPName}' dated '{fieldVisit.VisitDate}'? ");
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }


        [HttpGet("{fieldVisitPrimaryKey}/{treatmentBMPAssessmentTypeEnum}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ActionResult Observations([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessment = TreatmentBMPAssessments.GetByFieldVisitIDAndTreatmentBMPAssessmentType(_dbContext, fieldVisit.FieldVisitID, treatmentBMPAssessmentTypeEnum);

            // need this check to support deleting assessments from the edit page
            if (treatmentBMPAssessment == null)
            {
                return Redirect(treatmentBMPAssessmentTypeEnum == TreatmentBMPAssessmentTypeEnum.Initial
                    ? SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Assessment(fieldVisitPrimaryKey))
                    : SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x =>
                        x.PostMaintenanceAssessment(fieldVisitPrimaryKey)));
            }

            var existingObservations = treatmentBMPAssessment.TreatmentBMPObservations.ToList();
            var viewModel = new ObservationsViewModel(existingObservations);
            return ViewObservations(treatmentBMPAssessmentTypeEnum, viewModel, fieldVisit);
        }

        [HttpPost("{fieldVisitPrimaryKey}/{treatmentBMPAssessmentTypeEnum}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Observations([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum, ObservationsViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessment = TreatmentBMPAssessments.GetByFieldVisitIDAndTreatmentBMPAssessmentTypeWithChangeTracking(_dbContext, fieldVisit.FieldVisitID, treatmentBMPAssessmentTypeEnum);

            if (!ModelState.IsValid)
            {
                return ViewObservations(treatmentBMPAssessmentTypeEnum, viewModel, fieldVisit);
            }

            fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(CurrentPerson);

            // we may not have an assessment yet if we went directly to the url instead of using the wizard
            if (treatmentBMPAssessment == null)
            {
                treatmentBMPAssessment = CreatePlaceholderTreatmentBMPAssessment(fieldVisit, treatmentBMPAssessmentTypeEnum);
                await _dbContext.TreatmentBMPAssessments.AddAsync(treatmentBMPAssessment);
            }

            var treatmentBMPType = TreatmentBMPTypes.GetByIDWithChangeTracking(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            foreach (var collectionMethodSectionViewModel in viewModel.Observations)
            {
                var treatmentBMPAssessmentObservationType =
                    TreatmentBMPAssessmentObservationTypes.GetByIDWithChangeTracking(_dbContext, collectionMethodSectionViewModel
                            .TreatmentBMPAssessmentObservationTypeID.Value);
                var treatmentBMPObservation = await GetExistingTreatmentBMPObservationOrCreateNew(treatmentBMPAssessment, treatmentBMPAssessmentObservationType, treatmentBMPType, _dbContext);
                collectionMethodSectionViewModel.UpdateModel(treatmentBMPObservation);
            }

            // cache the score and the completeness status because they are difficult to calculate en masse later.
            treatmentBMPAssessment.CalculateIsAssessmentComplete();
            treatmentBMPAssessment.CalculateAssessmentScore(treatmentBMPType, treatmentBMPAssessment.TreatmentBMP);

            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit)) { return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit))); }
            SetMessageForDisplay("Assessment Information successfully saved.");

            await _dbContext.SaveChangesAsync();

            return RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.Observations(fieldVisit, treatmentBMPAssessmentTypeEnum)), new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                c.AssessmentPhotos(fieldVisit, treatmentBMPAssessmentTypeEnum)), fieldVisit);
        }

        private ViewResult ViewObservations(TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum,
            ObservationsViewModel viewModel, FieldVisit fieldVisit)
        {
            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, fieldVisit.TreatmentBMP.TreatmentBMPTypeID);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, fieldVisit.FieldVisitID);
            var maintenanceRecord = fieldVisit.MaintenanceRecord != null ? MaintenanceRecords.GetByID(_dbContext, fieldVisit.MaintenanceRecord.MaintenanceRecordID) : null;
            var viewData = new ObservationsViewData(HttpContext, _linkGenerator, _webConfiguration,
                CurrentPerson, fieldVisit, treatmentBMPAssessments, maintenanceRecord, treatmentBMPType, treatmentBMPAssessmentTypeEnum);
            return RazorView<Observations, ObservationsViewData, ObservationsViewModel>(viewData, viewModel);
        }

        private ActionResult RedirectToNextStep(FieldVisitViewModel viewModel, SitkaRoute<FieldVisitController> stayOnPageRoute,
            SitkaRoute<FieldVisitController> nextPageRoute, FieldVisit fieldVisit)
        {
            if (viewModel.StepToAdvanceTo.HasValue)
            {
                switch (viewModel.StepToAdvanceTo)
                {
                    case StepToAdvanceToEnum.StayOnPage:
                        return RedirectToAction(stayOnPageRoute);
                    case StepToAdvanceToEnum.NextPage:
                        return RedirectToAction(nextPageRoute);
                    case StepToAdvanceToEnum.WrapUpPage:
                        return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, c =>
                            c.VisitSummary(fieldVisit)));
                    default:
                        throw new ArgumentOutOfRangeException($"Invalid StepToAdvanceTo {viewModel.StepToAdvanceTo}");
                }
            }

            return RedirectToAction(stayOnPageRoute);
        }


        private static async Task<TreatmentBMPObservation> GetExistingTreatmentBMPObservationOrCreateNew(
            TreatmentBMPAssessment treatmentBMPAssessment,
            TreatmentBMPAssessmentObservationType treatmentBMPAssessmentObservationType, TreatmentBMPType treatmentBMPType, NeptuneDbContext dbContext)
        {
            var treatmentBMPObservation = treatmentBMPAssessment.TreatmentBMPObservations.ToList()
                .Find(x => x.TreatmentBMPAssessmentObservationTypeID ==
                           treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID);
            if (treatmentBMPObservation == null)
            {
                var treatmentBMPTypeAssessmentObservationType =
                    treatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes.SingleOrDefault(
                        x =>
                            x.TreatmentBMPAssessmentObservationTypeID == treatmentBMPAssessmentObservationType
                                .TreatmentBMPAssessmentObservationTypeID);
                if (treatmentBMPTypeAssessmentObservationType == null)
                {
                    throw new NullReferenceException($"Not a valid Observation Type {treatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName} for Treatment BMP Type {treatmentBMPType.TreatmentBMPTypeName}");
                }
                treatmentBMPObservation = new TreatmentBMPObservation
                {
                    TreatmentBMPAssessment = treatmentBMPAssessment,
                    TreatmentBMPTypeAssessmentObservationType = treatmentBMPTypeAssessmentObservationType,
                    TreatmentBMPType = treatmentBMPType,
                    TreatmentBMPAssessmentObservationType = treatmentBMPAssessmentObservationType
                };
                await dbContext.TreatmentBMPObservations.AddAsync(treatmentBMPObservation);
            }

            return treatmentBMPObservation;
        }

        #region Helper methods for Assessment

        private static TreatmentBMPAssessment CreatePlaceholderTreatmentBMPAssessment(FieldVisit fieldVisit, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
        {
            return new TreatmentBMPAssessment
            {
                TreatmentBMP = fieldVisit.TreatmentBMP, 
                TreatmentBMPType = fieldVisit.TreatmentBMP.TreatmentBMPType,
                FieldVisit = fieldVisit, TreatmentBMPAssessmentTypeID = (int)treatmentBMPAssessmentTypeEnum,
                IsAssessmentComplete = false
            };
        }
        #endregion

        #region Assessment Photos

        [HttpGet("{fieldVisitPrimaryKey}/{treatmentBMPAssessmentTypeEnum}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public ViewResult AssessmentPhotos([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, [FromRoute] TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessment = fieldVisit.GetAssessmentByType(treatmentBMPAssessmentTypeEnum);
            var treatmentBMPAssessmentPhotos = TreatmentBMPAssessmentPhotos.ListByTreatmentBMPAssessmentID(_dbContext, treatmentBMPAssessment.TreatmentBMPAssessmentID);
            var viewModel = new AssessmentPhotosViewModel(treatmentBMPAssessmentPhotos);
            return ViewAssessmentPhotos(treatmentBMPAssessment, treatmentBMPAssessmentTypeEnum, viewModel, treatmentBMPAssessmentPhotos);
        }

        [HttpPost("{fieldVisitPrimaryKey}/{treatmentBMPAssessmentTypeEnum}")]
        [FieldVisitEditFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> AssessmentPhotos([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, [FromRoute] TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum, AssessmentPhotosViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            var treatmentBMPAssessment = fieldVisit.GetAssessmentByType(treatmentBMPAssessmentTypeEnum);
            var treatmentBMPAssessmentPhotos = TreatmentBMPAssessmentPhotos.ListByTreatmentBMPAssessmentIDWithChangeTracking(_dbContext, treatmentBMPAssessment.TreatmentBMPAssessmentID);
            if (!ModelState.IsValid)
            {
                return ViewAssessmentPhotos(treatmentBMPAssessment, treatmentBMPAssessmentTypeEnum, viewModel, treatmentBMPAssessmentPhotos);
            }

            fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(CurrentPerson);

            if (treatmentBMPAssessment == null)
            {
                treatmentBMPAssessment = CreatePlaceholderTreatmentBMPAssessment(fieldVisit, treatmentBMPAssessmentTypeEnum);
                await _dbContext.TreatmentBMPAssessments.AddAsync(treatmentBMPAssessment);
            }

            await viewModel.UpdateModel(CurrentPerson, treatmentBMPAssessment, _dbContext, fileResourceService, treatmentBMPAssessmentPhotos);
            if (await FinalizeVisitIfNecessary(viewModel, fieldVisit)) { return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Detail(fieldVisit))); }
            await _dbContext.SaveChangesAsync();
            SetMessageForDisplay("Successfully updated treatment BMP assessment photos.");

            return treatmentBMPAssessmentTypeEnum == TreatmentBMPAssessmentTypeEnum.Initial
                    ? RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.AssessmentPhotos(fieldVisit, treatmentBMPAssessmentTypeEnum)), new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Maintain(fieldVisit)), fieldVisit)
                    : RedirectToNextStep(viewModel, new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.AssessmentPhotos(fieldVisit, treatmentBMPAssessmentTypeEnum)), new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.VisitSummary(fieldVisit)), fieldVisit);
        }

        private ViewResult ViewAssessmentPhotos(TreatmentBMPAssessment treatmentBMPAssessment, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum, AssessmentPhotosViewModel viewModel, IEnumerable<TreatmentBMPAssessmentPhoto> treatmentBMPAssessmentPhotos)
        {
            var fieldVisitSection = treatmentBMPAssessmentTypeEnum == TreatmentBMPAssessmentTypeEnum.Initial ? (FieldVisitSection)FieldVisitSection.Assessment : FieldVisitSection.PostMaintenanceAssessment;

            var managePhotosWithPreviewViewData = new ManagePhotosWithPreviewViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, treatmentBMPAssessmentPhotos);

            var treatmentBMPType = TreatmentBMPTypes.GetByID(_dbContext, treatmentBMPAssessment.TreatmentBMPTypeID);
            var treatmentBMPAssessments = TreatmentBMPAssessments.ListByFieldVisitID(_dbContext, treatmentBMPAssessment.FieldVisitID);
            var maintenanceRecord = treatmentBMPAssessment.FieldVisit.MaintenanceRecord;
            var viewData = new AssessmentPhotosViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, treatmentBMPAssessment, fieldVisitSection, managePhotosWithPreviewViewData, treatmentBMPType, maintenanceRecord, treatmentBMPAssessments);
            return RazorView<AssessmentPhotos, AssessmentPhotosViewData, AssessmentPhotosViewModel>(viewData, viewModel);
        }

        #endregion

        [HttpGet("{fieldVisitPrimaryKey}")]
        [FieldVisitDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public PartialViewResult Delete([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey)
        {
            var fieldVisit = FieldVisits.GetByID(_dbContext, fieldVisitPrimaryKey);
            var viewModel = new ConfirmDialogFormViewModel(fieldVisit.FieldVisitID);
            return ViewDeleteFieldVisit(fieldVisit, viewModel);
        }

        [HttpPost("{fieldVisitPrimaryKey}")]
        [FieldVisitDeleteFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fieldVisitPrimaryKey")]
        public async Task<IActionResult> Delete([FromRoute] FieldVisitPrimaryKey fieldVisitPrimaryKey, ConfirmDialogFormViewModel viewModel)
        {
            var fieldVisit = FieldVisits.GetByIDWithChangeTracking(_dbContext, fieldVisitPrimaryKey);
            if (!ModelState.IsValid)
            {
                return ViewDeleteFieldVisit(fieldVisit, viewModel);
            }
            await fieldVisit.DeleteFull(_dbContext);
            SetMessageForDisplay("Successfully deleted the field visit.");
            return new ModalDialogFormJsonResult(SitkaRoute<FieldVisitController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()));
        }

        private PartialViewResult ViewDeleteFieldVisit(FieldVisit fieldVisit, ConfirmDialogFormViewModel viewModel)
        {
            var confirmMessage = $"Are you sure you want to delete the field visit from '{fieldVisit.VisitDate}'?{AssociatedFieldVisitEntitiesString(fieldVisit)}";

            var viewData = new ConfirmDialogFormViewData(confirmMessage, true);
            return RazorPartialView<ConfirmDialogForm, ConfirmDialogFormViewData, ConfirmDialogFormViewModel>(viewData, viewModel);
        }

        private static string AssociatedFieldVisitEntitiesString(FieldVisit fieldVisit)
        {
            var entitiesSubstrings = new List<string>
            {
                (fieldVisit.GetInitialAssessment() != null) ? "initial assessment" : null,
                fieldVisit.GetPostMaintenanceAssessment() != null ? "post-maintenance assessment" : null,
                fieldVisit.MaintenanceRecord != null ? "maintenance record" : null
            };
            var entitiesConcatenated = string.Join(", ", entitiesSubstrings.Where(x => x != null));
            var lastComma = entitiesConcatenated.LastIndexOf(",", StringComparison.InvariantCulture);
            var associatedFieldVisitEntitiesString = lastComma > -1 ? entitiesConcatenated.Insert(lastComma + 1, " and") : entitiesConcatenated;

            return !string.IsNullOrWhiteSpace(associatedFieldVisitEntitiesString) ? $" This will delete the associated {associatedFieldVisitEntitiesString}." : "";
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public ViewResult BulkUploadTrashScreenVisit()
        {
            var bulkUploadTrashScreenVisitViewModel = new BulkUploadTrashScreenVisitViewModel();

            return ViewBulkUploadTrashScreenVisit(bulkUploadTrashScreenVisitViewModel);
        }

        private ViewResult ViewBulkUploadTrashScreenVisit(
            BulkUploadTrashScreenVisitViewModel bulkUploadTrashScreenVisitViewModel)
        {
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.BulkUploadFieldVisits);
            var bulkUploadTrashScreenVisitViewData = new BulkUploadTrashScreenVisitViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage);

            return RazorView<BulkUploadTrashScreenVisit, BulkUploadTrashScreenVisitViewData,
                BulkUploadTrashScreenVisitViewModel>(bulkUploadTrashScreenVisitViewData,
                bulkUploadTrashScreenVisitViewModel);
        }

        private const string INLET = "Inlet Condition";
        private const string OUTLET = "Outlet Condition";
        private const string OPERABILITY = "Device Operability";
        private const string NUISANCE = "Significant Nuisance Conditions";
        private const string ACCUMULATION = "Material Accumulation as Percent of Total System Volume";

        private const int InletAndTrashScreenTreatmentBMPTypeID = 35;

        private const string GREEN_WASTE = "Percent Green Waste";
        private const string MECHANICAL_REPAIR = "Mechanical Repair Conducted";
        private const string SEDIMENT = "Percent Sediment";
        private const string STRUCTURAL_REPAIR = "Structural Repair Conducted";
        private const string TRASH = "Percent Trash";
        private const string VOLUME_CUFT = "Total Material Volume Removed (cu-ft)";
        private const string VOLUME_GAL = "Total Material Volume Removed (gal)";

        [HttpPost]
        [JurisdictionEditFeature]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<IActionResult> BulkUploadTrashScreenVisit(BulkUploadTrashScreenVisitViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return ViewBulkUploadTrashScreenVisit(viewModel);
            }

            var uploadXlsxInputStream = viewModel.UploadXLSX.OpenReadStream();

            DataTable dataTableFromExcel;
            try
            {
                dataTableFromExcel = GetDataTableFromExcel(uploadXlsxInputStream, "Field Visits");
            }
            catch (Exception e)
            {
                SetErrorForDisplay(e.Message.Contains("column")
                    ? e.Message
                    : "Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewBulkUploadTrashScreenVisit(viewModel);
            }

            var numRows = dataTableFromExcel.Rows.Count;

            var stormwaterJurisdictionsPersonCanView = StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, CurrentPerson);

            if (!CurrentPerson.IsAdministrator())
            {
                foreach (DataRow row in dataTableFromExcel.Rows)
                {
                    var rowJurisdiction = row["Jurisdiction"].ToString();
                    if (!stormwaterJurisdictionsPersonCanView.Select(x => x.Organization.OrganizationName)
                        .Contains(rowJurisdiction))
                    {
                        SetErrorForDisplay(
                            $"You attempted to upload a spreadsheet containing BMPs in Jurisdiction {rowJurisdiction}, which you do not have permission to manage.");
                        return ViewBulkUploadTrashScreenVisit(viewModel);
                    }
                }
            }

            var treatmentBMPTypeAssessmentObservationTypes =
                _dbContext.TreatmentBMPTypeAssessmentObservationTypes
                    .Include(x => x.TreatmentBMPAssessmentObservationType).Where(x =>
                    x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID).ToList();

            var treatmentBMPTypeCustomAttributeTypes = _dbContext

                .TreatmentBMPTypeCustomAttributeTypes.Include(x => x.CustomAttributeType)
                .Where(x => x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID &&
                            x.CustomAttributeType.CustomAttributeTypePurposeID == (int)
                            CustomAttributeTypePurposeEnum.Maintenance).ToList();

            var caredAboutAssessmentObservationTypeNames = new[] { INLET, OUTLET, OPERABILITY, NUISANCE, ACCUMULATION };
            var caredAboutCustomAttributeTypeNames = new[]{ GREEN_WASTE, MECHANICAL_REPAIR, SEDIMENT, STRUCTURAL_REPAIR, TRASH, VOLUME_CUFT, VOLUME_GAL };

            var treatmentBMPTypeCustomAttributeTypeDictionary = caredAboutCustomAttributeTypeNames.ToDictionary(name => name, name => treatmentBMPTypeCustomAttributeTypes.Single(x => x.CustomAttributeType.CustomAttributeTypeName == name));

            var treatmentBMPAssessmentObservationTypeDictionary = caredAboutAssessmentObservationTypeNames.ToDictionary(name => name, name => treatmentBMPTypeAssessmentObservationTypes.Select(x => x.TreatmentBMPAssessmentObservationType).Single(x => x.TreatmentBMPAssessmentObservationTypeName == name));

            var numColumns = dataTableFromExcel.Columns.Count;

            var errors = new List<string>();

            try
            {
                for (var i = 0; i < numRows; i++)
                {
                    try
                    {
                        var row = dataTableFromExcel.Rows[i];
                        var rowEmpty = true;
                        for (var j = 0; j < numColumns; j++)
                        {
                            rowEmpty = string.IsNullOrWhiteSpace(row[j].ToString());
                            if (!rowEmpty)
                            {
                                break;
                            }
                        }

                        if (rowEmpty)
                        {
                            continue;
                        }

                        var treatmentBMPName = row["BMP Name"].ToString()?.Trim();
                        var jurisdictionName = row["Jurisdiction"].ToString()?.Trim();

                        var treatmentBMP = _dbContext.TreatmentBMPs
                            .Include(x => x.TreatmentBMPBenchmarkAndThresholds)
                            .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization).SingleOrDefault(x =>
                                x.TreatmentBMPName == treatmentBMPName &&
                                x.StormwaterJurisdiction.Organization.OrganizationName == jurisdictionName);

                        if (treatmentBMP == null)
                        {
                            throw new InvalidOperationException($"Invalid BMP Name or Jurisdiction at row {i + 2}");
                        }

                        var rawFieldVisitType = row["Field Visit Type"].ToString()?.ToLower();
                        var fieldVisitType = FieldVisitType.All.SingleOrDefault(x => x.FieldVisitTypeDisplayName.ToLower() == rawFieldVisitType);
                        if (fieldVisitType == null)
                        {
                            throw new InvalidOperationException($"Invalid Field Visit Type at row {i + 2}");
                        }

                        var rawFieldVisitDate = row["Field Visit Date"].ToString();
                        var fieldVisitDateIsValid = DateTime.TryParse(rawFieldVisitDate, out var fieldVisitDate);

                        if (!fieldVisitDateIsValid)
                        {
                            throw new InvalidOperationException($"Invalid Field Visit Date at row {i + 2}");
                        }

                        var fieldVisit = _dbContext.FieldVisits
                                .Include(x => x.MaintenanceRecord)
                                .ThenInclude(x => x.MaintenanceRecordObservations)
                                .ThenInclude(x => x.MaintenanceRecordObservationValues)
                                .Include(x => x.TreatmentBMPAssessments).ThenInclude(x => x.TreatmentBMPObservations)
                                .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMP.TreatmentBMPID && x.VisitDate.Date == fieldVisitDate.Date);
                        if (fieldVisit == null)
                        {
                            fieldVisit = new FieldVisit
                            {
                                TreatmentBMP = treatmentBMP,
                                FieldVisitStatusID = FieldVisitStatus.Complete.FieldVisitStatusID,
                                PerformedByPersonID = CurrentPerson.PersonID,
                                VisitDate = fieldVisitDate.ConvertTimeFromPSTToUTC(),
                                FieldVisitTypeID = fieldVisitType.FieldVisitTypeID,
                                InventoryUpdated = false,
                                IsFieldVisitVerified = true
                            };
                            await _dbContext.FieldVisits.AddAsync(fieldVisit);
                        }

                        var treatmentBMPType = TreatmentBMPTypes.GetByIDWithChangeTracking(_dbContext, treatmentBMP.TreatmentBMPTypeID);
                        if (InitialAssessmentFieldsPopulated(row, i))
                        {
                            var initialAssessment = fieldVisit.GetInitialAssessment();
                            if (initialAssessment == null)
                            {
                                initialAssessment = new TreatmentBMPAssessment
                                {
                                    TreatmentBMP = treatmentBMP,
                                    TreatmentBMPType = treatmentBMPType,
                                    FieldVisit = fieldVisit,
                                    TreatmentBMPAssessmentTypeID = (int)TreatmentBMPAssessmentTypeEnum.Initial,
                                    IsAssessmentComplete = true
                                };
                                await _dbContext.TreatmentBMPAssessments.AddAsync(initialAssessment);
                            }

                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row,
                                treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, INLET, ObservationTypeDataTypeEnum.PassFail,
                                false, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row,
                                treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, OUTLET, ObservationTypeDataTypeEnum.PassFail, false, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row,
                                treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, OPERABILITY,
                                ObservationTypeDataTypeEnum.PassFail, false, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row,
                                treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, NUISANCE, ObservationTypeDataTypeEnum.PassFail, false, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row,
                                treatmentBMPAssessmentObservationTypeDictionary, i, initialAssessment, treatmentBMPType, ACCUMULATION,
                                ObservationTypeDataTypeEnum.Numeric, false, _dbContext, errors);

                            try
                            {
                                initialAssessment.CalculateAssessmentScore(treatmentBMPType, treatmentBMP);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(ex.Message + $" on row {i+2}");
                            }
                        }

                        if (MaintenanceRecordFieldsPopulated(row))
                        {
                            var maintenanceRecord = fieldVisit.MaintenanceRecord;
                            if (maintenanceRecord == null)
                            {
                                maintenanceRecord = new MaintenanceRecord
                                {
                                    TreatmentBMP = treatmentBMP,
                                    TreatmentBMPType = treatmentBMPType,
                                    FieldVisit = fieldVisit
                                };
                                await _dbContext.MaintenanceRecords.AddAsync(maintenanceRecord);
                            }

                            var rawMaintenanceType = row["Maintenance Type"].ToString()?.ToLower();
                            var rawDescription = row["Description"].ToString();

                            var maintenanceRecordType = MaintenanceRecordType.All.SingleOrDefault(x =>
                                x.MaintenanceRecordTypeDisplayName.ToLower() == rawMaintenanceType);

                            if (maintenanceRecordType == null)
                            {
                                throw new InvalidOperationException($"Invalid Maintenance type at row {i + 2}");
                            }


                            maintenanceRecord.MaintenanceRecordTypeID = maintenanceRecordType.MaintenanceRecordTypeID;
                            maintenanceRecord.MaintenanceRecordDescription = rawDescription;

                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, STRUCTURAL_REPAIR, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, MECHANICAL_REPAIR, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, VOLUME_CUFT, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, VOLUME_GAL, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, TRASH, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, GREEN_WASTE, i, errors);
                            errors = await UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(_dbContext, row,
                                treatmentBMPTypeCustomAttributeTypeDictionary, maintenanceRecord, SEDIMENT, i, errors);
                        }

                        if (PostMaintenanceAssessmentFieldsPopulated(row, i))
                        {
                            var postMaintenanceAssessment = fieldVisit.GetPostMaintenanceAssessment();
                            if (postMaintenanceAssessment == null)
                            {
                                postMaintenanceAssessment = new TreatmentBMPAssessment
                                {
                                    TreatmentBMP = treatmentBMP,
                                    TreatmentBMPType = treatmentBMPType,
                                    FieldVisit = fieldVisit,
                                    TreatmentBMPAssessmentTypeID = (int)TreatmentBMPAssessmentTypeEnum.PostMaintenance,
                                    IsAssessmentComplete = true
                                };
                                await _dbContext.TreatmentBMPAssessments.AddAsync(postMaintenanceAssessment);
                            }

                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, INLET, ObservationTypeDataTypeEnum.PassFail, true, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, OUTLET, ObservationTypeDataTypeEnum.PassFail, true, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment,
                                treatmentBMPType, OPERABILITY, ObservationTypeDataTypeEnum.PassFail, true, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, NUISANCE, ObservationTypeDataTypeEnum.PassFail, true, _dbContext, errors);
                            errors = await UpdateOrCreateSingleValueObservationFromDataTableRow(row, treatmentBMPAssessmentObservationTypeDictionary, i, postMaintenanceAssessment, treatmentBMPType, ACCUMULATION, ObservationTypeDataTypeEnum.Numeric, true, _dbContext, errors);

                            if (errors.Count == 0)
                            {
                                try
                                {
                                    postMaintenanceAssessment.CalculateAssessmentScore(treatmentBMPType, treatmentBMP);
                                }
                                catch (Exception ex)
                                {
                                    SetErrorForDisplay(ex.Message);
                                    throw ex;
                                }
                            }
                        }
                    }
                    catch (InvalidOperationException ioe)
                    {
                        errors.Add(ioe.Message);
                    }
                }
            }
            catch (Exception e)
            {
                SetErrorForDisplay("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
                return ViewBulkUploadTrashScreenVisit(viewModel);
            }

            if (errors.Count > 0)
            {
                SetErrorForDisplay(string.Join("<br/>", errors));
                return ViewBulkUploadTrashScreenVisit(viewModel);
            }

            await _dbContext.SaveChangesAsync();

            SetMessageForDisplay("Successfully bulk uploaded Field Visit Assessment and Maintenance Records");

            return RedirectToAction(new SitkaRoute<FieldVisitController>(_linkGenerator, x => x.Index()));
        }

        private static bool PostMaintenanceAssessmentFieldsPopulated(DataRow row, int index)
        {
            var startIndex = row.Table.Columns.IndexOf($"{INLET} (Post-Maintenance)");
            var endIndex = row.Table.Columns.IndexOf($"{ACCUMULATION} Notes (Post-Maintenance)");

            // they are allowed to submit a completely blank post-maint assessment, but all fields must be filled out if any are
            var allowBlank = true;

            for (var i = startIndex; i <= endIndex; i++)
            {
                if (row.Table.Columns[i].ColumnName.Trim().EndsWith("Notes (Post-Maintenance)"))
                {
                    // don't care about the notes columns which are optional.
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row[i].ToString()))
                {
                    allowBlank = false;
                }
                // if this field is empty, but a previous field is not, then we have to bork.
                else if (!allowBlank)
                {
                    throw new InvalidOperationException($"Post-Maintenance Assessment at row {index + 2} must be completely filled out or left completely blank.");
                }
            }

            // if allowBlank is still true, then the assessment is empty, i.e. "not populated", i.e. this function returns false. and v/v

            return !allowBlank;
        }

        private static bool MaintenanceRecordFieldsPopulated(DataRow row)
        {
            var startIndex = row.Table.Columns.IndexOf("Maintenance Type");
            var endIndex = row.Table.Columns.IndexOf(SEDIMENT);

            for (var i = startIndex; i <= endIndex; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i].ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InitialAssessmentFieldsPopulated(DataRow row, int index)
        {
            var startIndex = row.Table.Columns.IndexOf(INLET);
            var endIndex = row.Table.Columns.IndexOf($"{ACCUMULATION} Notes");

            // they are allowed to submit a completely blank initial assessment, but all fields must be filled out if any are
            var allowBlank = true;

            for (var i = startIndex; i <= endIndex; i++)
            {
                if (row.Table.Columns[i].ColumnName.Trim().EndsWith("Notes"))
                {
                    // don't care about the notes columns which are optional.
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(row[i].ToString()))
                {
                    allowBlank = false;
                }
                // if this field is empty, but a previous field is not, then we have to bork.
                else if (!allowBlank)
                {
                    throw new InvalidOperationException($"Initial Assessment at row {index + 2} must be completely filled out or left completely blank.");
                }
            }

            // if allowBlank is still true, then the assessment is empty, i.e. "not populated", i.e. this function returns false. and v/v

            return !allowBlank;
        }

        private static async Task<List<string>> UpdateOrCreateMaintenanceRecordObservationFromDataTableRow(NeptuneDbContext dbContext,
            DataRow row,
            IReadOnlyDictionary<string, TreatmentBMPTypeCustomAttributeType>
                treatmentBMPTypeCustomAttributeTypeDictionary,
            MaintenanceRecord maintenanceRecord, string observationName, int rowNumber, List<string> errors)
        {
            var rawObservation = row[observationName].ToString();
            var treatmentBMPTypeCustomAttributeType = treatmentBMPTypeCustomAttributeTypeDictionary[observationName];

            var maintenanceRecordObservation = maintenanceRecord.MaintenanceRecordObservations.SingleOrDefault(x =>
                x.CustomAttributeType.CustomAttributeTypeName.ToLower() == observationName.ToLower());
            string valueParsedForDataType;
            try
            {
                valueParsedForDataType = treatmentBMPTypeCustomAttributeType.CustomAttributeType.CustomAttributeDataType
                    .ValueParsedForDataType(rawObservation);
            }
            catch (Exception)
            {
                errors.Add($"Invalid {observationName} at row {rowNumber + 2}");
                return errors;
            }

            if (maintenanceRecordObservation != null)
            {
                var maintenanceRecordObservationValue = maintenanceRecordObservation.MaintenanceRecordObservationValues.SingleOrDefault();
                if (maintenanceRecordObservationValue != null)
                {
                    maintenanceRecordObservationValue.ObservationValue = valueParsedForDataType;
                }
                else
                {
                    maintenanceRecordObservationValue =
                        new MaintenanceRecordObservationValue
                        {
                            MaintenanceRecordObservation = maintenanceRecordObservation,
                            ObservationValue = valueParsedForDataType
                        };
                    await dbContext.MaintenanceRecordObservationValues.AddAsync(maintenanceRecordObservationValue);
                }
            }
            else
            {
                maintenanceRecordObservation = new MaintenanceRecordObservation()
                {
                    MaintenanceRecord = maintenanceRecord,
                    TreatmentBMPTypeCustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeCustomAttributeTypeID,
                    TreatmentBMPTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeID,
                    CustomAttributeType = treatmentBMPTypeCustomAttributeType.CustomAttributeType
                };
                await dbContext.MaintenanceRecordObservations.AddAsync(maintenanceRecordObservation);
                var maintenanceRecordObservationValue =
                    new MaintenanceRecordObservationValue
                    {
                        MaintenanceRecordObservation = maintenanceRecordObservation,
                        ObservationValue = valueParsedForDataType
                    };
                await dbContext.MaintenanceRecordObservationValues.AddAsync(maintenanceRecordObservationValue);
            }

            return errors;
        }

        private static async Task<List<string>> UpdateOrCreateSingleValueObservationFromDataTableRow(DataRow row,
            Dictionary<string, TreatmentBMPAssessmentObservationType> treatmentBMPAssessmentObservationTypeDictionary,
            int rowNumber, TreatmentBMPAssessment assessment, TreatmentBMPType treatmentBMPType,
            string observationTypeName, ObservationTypeDataTypeEnum dataType, bool isPostMaintenance, NeptuneDbContext dbContext,
            List<string> errors)
        {
            var suffix = isPostMaintenance ? " (Post-Maintenance)" : "";
            var rawInletValidationResult = dataType.ValidateAndParse(row[$"{observationTypeName}{suffix}"].ToString());
            var rawInletConditionNotes = row[$"{observationTypeName} Notes{suffix}"].ToString();

            if (!rawInletValidationResult.IsValid)
            {
                errors.Add($"Invalid {observationTypeName}{suffix} at row {rowNumber + 2}. Message: {rawInletValidationResult.ErrorMessage}");
                return errors;
            }

            var inletConditionBoxed = new
            {
                SingleValueObservations = new[]
                {
                    new
                    {
                        PropertyObserved = observationTypeName,
                        ObservationValue = rawInletValidationResult.ParsedValue,
                        Notes = rawInletConditionNotes
                    }
                }
            };

            var inletConditionJson = GeoJsonSerializer.Serialize(inletConditionBoxed);

            var validateObservationDataJson = treatmentBMPAssessmentObservationTypeDictionary[observationTypeName]
                .ObservationTypeSpecification.ObservationTypeCollectionMethod
                .ValidateObservationDataJson(treatmentBMPAssessmentObservationTypeDictionary[observationTypeName],
                    inletConditionJson);

            if (validateObservationDataJson.Count > 0)
            {
                errors.Add($"Invalid {observationTypeName} at row {rowNumber + 2}");
                return errors;
            }

            try
            {
                var initialInletConditionObservation = await GetExistingTreatmentBMPObservationOrCreateNew(assessment,
                    treatmentBMPAssessmentObservationTypeDictionary[observationTypeName], treatmentBMPType, dbContext);
                initialInletConditionObservation.ObservationData = inletConditionJson;
            }
            catch (Exception e)
            {
                errors.Add($"{e.Message} on row {rowNumber + 2}");
            }
            

            return errors;
        }

        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<FileResult> TrashScreenBulkUploadTemplate()
        {
            var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(_dbContext, CurrentPerson).ToList();

            var currentPersonTrashScreens = TreatmentBMPs.GetNonPlanningModuleBMPs(_dbContext)
                .Where(x => x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID &&
                            stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID)).ToList();

            var treatmentBMPIDs = currentPersonTrashScreens.Select(x => x.TreatmentBMPID);

            var inletScreensDict = CustomAttributes.ListByCustomAttributeTypeID(_dbContext,
                CustomAttributeTypes.CustomAttributeTypeIDNumberOfInletScreens).Where(x =>  treatmentBMPIDs.Contains(x.TreatmentBMPID)).ToDictionary(x => x.TreatmentBMPID);
            var trashBucketsDict = CustomAttributes.ListByCustomAttributeTypeID(_dbContext,
                CustomAttributeTypes.CustomAttributeTypeIDNumberOfTrashBaskets).Where(x =>  treatmentBMPIDs.Contains(x.TreatmentBMPID)).ToDictionary(x => x.TreatmentBMPID);
            var connectorPipeScreensDict = CustomAttributes.ListByCustomAttributeTypeID(_dbContext,
                CustomAttributeTypes.CustomAttributeTypeIDNumberOfConnectorPipeScreens).Where(x =>  treatmentBMPIDs.Contains(x.TreatmentBMPID)).ToDictionary(x => x.TreatmentBMPID);

            var row = 2;
            using var disposableTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".xlsx");
            await azureBlobStorageService.DownloadBlobToFileAsync(_webConfiguration.PathToFieldVisitUploadTemplate, disposableTempFile.FileInfo.FullName);

            using var workbook = new XLWorkbook(disposableTempFile.FileInfo.FullName);
            var worksheet = workbook.Worksheet("Field Visits");
            foreach (var treatmentBMP in currentPersonTrashScreens)
            {
                worksheet.Cells($"A{row}").Value = treatmentBMP.TreatmentBMPName;
                worksheet.Cells($"B{row}").Value = treatmentBMP.StormwaterJurisdiction.Organization.OrganizationName;
                worksheet.Cells($"C{row}").Value = treatmentBMP.YearBuilt;
                worksheet.Cells($"D{row}").Value = treatmentBMP.Notes;
                SetCellValueFromCustomAttribute(inletScreensDict, treatmentBMP, worksheet, "E", row);
                SetCellValueFromCustomAttribute(trashBucketsDict, treatmentBMP, worksheet, "F", row);
                SetCellValueFromCustomAttribute(connectorPipeScreensDict, treatmentBMP, worksheet, "G", row);
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TrashScreenBulkUploadTemplate_{CurrentPerson.LastName}{CurrentPerson.FirstName}.xlsx");
        }

        private static void SetCellValueFromCustomAttribute(IReadOnlyDictionary<int, CustomAttribute> customAttributeDict, TreatmentBMP treatmentBMP,
            IXLRangeBase worksheet, string column, int row)
        {
            if (customAttributeDict.TryGetValue(treatmentBMP.TreatmentBMPID, out var customAttribute))
            {
                var value = customAttribute.CustomAttributeValues.FirstOrDefault();
                if (value != null && !string.IsNullOrWhiteSpace(value.AttributeValue))
                {
                    if (int.TryParse(value.AttributeValue, out var int1))
                    {
                        worksheet.Cells($"{column}{row}").Value = int1;
                    }
                }
            }
        }

        public static DataTable GetDataTableFromExcel(Stream inputStream, dynamic worksheet)
        {
            var dataTable = new DataTable();
            using var workBook = new XLWorkbook(inputStream);
            IXLWorksheet workSheet = workBook.Worksheet(worksheet);

            //Loop through the Worksheet rows.
            var firstRow = true;
            foreach (var row in workSheet.Rows())
            {
                //Use the first row to add columns to DataTable.
                if (firstRow)
                {
                    foreach (var cell in row.Cells())
                    {
                        if (!string.IsNullOrEmpty(cell.Value.ToString()))
                        {
                            dataTable.Columns.Add(cell.Value.ToString());
                        }
                        else
                        {
                            break;
                        }
                    }
                    firstRow = false;
                }
                else
                {
                    var i = 0;
                    var toInsert = dataTable.NewRow();
                    foreach (var cell in row.Cells(1, dataTable.Columns.Count))
                    {
                        toInsert[i] = cell.Value.ToString();
                        i++;
                    }
                    dataTable.Rows.Add(toInsert);
                }
            }

            return dataTable;
        }
    }
}