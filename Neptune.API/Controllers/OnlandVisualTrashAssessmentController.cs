using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neptune.EFModels.Workflows;
using NetTopologySuite.Features;
using Neptune.Common.GeoSpatial;

namespace Neptune.API.Controllers;

[ApiController]
[Route("onland-visual-trash-assessments")]
public class OnlandVisualTrashAssessmentController(
    NeptuneDbContext dbContext,
    ILogger<OnlandVisualTrashAssessmentController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<OnlandVisualTrashAssessmentController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<OnlandVisualTrashAssessmentGridDto>>> List()
    {
        var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var onlandVisualTrashAssessmentGridDtos = OnlandVisualTrashAssessments.ListByStormwaterJurisdictionIDAsGridDto(DbContext, stormwaterJurisdictionIDs);
        return Ok(onlandVisualTrashAssessmentGridDtos);
    }

    [HttpPost]
    [JurisdictionEditFeature]
    public async Task<ActionResult<OnlandVisualTrashAssessmentSimpleDto>> Create([FromBody] OnlandVisualTrashAssessmentSimpleDto dto)
    {
        var onlandVisualTrashAssessment = await OnlandVisualTrashAssessments.CreateNew(DbContext, dto, CallingUser);
        return Ok(onlandVisualTrashAssessment);
    }

    [HttpGet("{onlandVisualTrashAssessmentID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<OnlandVisualTrashAssessmentDetailDto> Get([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessmentDetailDto = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID).AsDetailDto();
        return Ok(onlandVisualTrashAssessmentDetailDto);
    }

    [HttpDelete("{onlandVisualTrashAssessmentID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<IActionResult> Delete([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = OnlandVisualTrashAssessments.GetByIDWithChangeTracking(DbContext, onlandVisualTrashAssessmentID);

        var onlandVisualTrashAssessmentArea = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea;

        var isProgressAssessment = onlandVisualTrashAssessment.IsProgressAssessment;
        await onlandVisualTrashAssessment.DeleteFull(DbContext);

        if (onlandVisualTrashAssessmentArea != null)
        {
            var onlandVisualTrashAssessments = OnlandVisualTrashAssessments.ListByOnlandVisualTrashAssessmentAreaID(DbContext, onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaID);
            onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScoreID = OnlandVisualTrashAssessmentAreas.CalculateBaselineScoreFromBackingData(onlandVisualTrashAssessments)?.OnlandVisualTrashAssessmentScoreID;

            if (isProgressAssessment)
            {
                onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScoreID =
                    OnlandVisualTrashAssessments.CalculateProgressScore(onlandVisualTrashAssessments)
                        ?.OnlandVisualTrashAssessmentScoreID;
            }

        }
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentID}/area-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<FeatureCollection> GetAreaAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var featureCollection = OnlandVisualTrashAssessments.GetAssessmentAreaByIDAsFeatureCollection(DbContext, onlandVisualTrashAssessmentID);
        return Ok(featureCollection);
    }

    [HttpGet("{onlandVisualTrashAssessmentID}/transect-line-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<FeatureCollection> GetTransectLineAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID);
        var transectLine4326GeoJson = OnlandVisualTrashAssessments.GetTransectLine4326GeoJson(onlandVisualTrashAssessment);
        return Ok(transectLine4326GeoJson);
    }

    [HttpGet("{onlandVisualTrashAssessmentID}/parcels")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<OnlandVisualTrashAssessmentAddRemoveParcelsDto> GetByIDForAddOrRemoveParcel([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessmentAddRemoveParcelsDto = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID).AsAddRemoveParcelDto(DbContext);
        return Ok(onlandVisualTrashAssessmentAddRemoveParcelsDto);
    }

    [HttpPost("{onlandVisualTrashAssessmentID}/parcels")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult> UpdateOnlandVisualTrashAssessmentWithParcels([FromRoute] int onlandVisualTrashAssessmentID, [FromBody] List<int> parcelIDs)
    {
        await OnlandVisualTrashAssessments.UpdateGeometry(DbContext, onlandVisualTrashAssessmentID, parcelIDs);
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentID}/review-and-finalize")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<OnlandVisualTrashAssessmentReviewAndFinalizeDto> GetByIDForReviewAndFinalize([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessmentReviewAndFinalizeDto = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID).AsReviewAndFinalizeDto();
        return Ok(onlandVisualTrashAssessmentReviewAndFinalizeDto);
    }

    [HttpPost("{onlandVisualTrashAssessmentID}/review-and-finalize")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult> UpdateReviewAndFinalize([FromRoute] int onlandVisualTrashAssessmentID, [FromBody] OnlandVisualTrashAssessmentReviewAndFinalizeDto dto)
    {
        await OnlandVisualTrashAssessments.Update(DbContext, onlandVisualTrashAssessmentID, dto);
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentID}/progress")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<OnlandVisualTrashAssessmentWorkflowProgress.OnlandVisualTrashAssessmentWorkflowProgressDto> GetWorkflowProgress([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID);
        var onlandVisualTrashAssessmentProgressDto = OnlandVisualTrashAssessmentWorkflowProgress.GetProgress(onlandVisualTrashAssessment);
        return Ok(onlandVisualTrashAssessmentProgressDto);
    }

    [HttpPost("{onlandVisualTrashAssessmentID}/refine-area")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult> UpdateOnlandVisualTrashAssessmentWithRefinedArea([FromRoute] int onlandVisualTrashAssessmentID, OnlandVisualTrashAssessmentRefineAreaDto dto)
    {
        await OnlandVisualTrashAssessments.UpdateGeometry(DbContext, onlandVisualTrashAssessmentID, dto.GeometryAsGeoJson);
        return Ok();
    }

    [HttpPost("{onlandVisualTrashAssessmentID}/refresh-parcels")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult<OnlandVisualTrashAssessmentAddRemoveParcelsDto>> RefreshOnlandVisualTrashAssessmentParcels([FromRoute] int onlandVisualTrashAssessmentID)
    {
        await OnlandVisualTrashAssessments.RefreshParcels(DbContext, onlandVisualTrashAssessmentID);
        var onlandVisualTrashAssessmentAddRemoveParcelsDto = OnlandVisualTrashAssessments.GetByID(DbContext, onlandVisualTrashAssessmentID).AsAddRemoveParcelDto(DbContext);
        return Ok(onlandVisualTrashAssessmentAddRemoveParcelsDto);
    }

    [HttpPost("{onlandVisualTrashAssessmentID}/return-to-edit")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult> EditStatusToAllowEdit([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = OnlandVisualTrashAssessments.GetByIDWithChangeTracking(DbContext, onlandVisualTrashAssessmentID);

        onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID = (int) OnlandVisualTrashAssessmentStatusEnum.InProgress;
        onlandVisualTrashAssessment.AssessingNewArea = false;

        // update the assessment area scores now that this assessment no longer contributes
        var onlandVisualTrashAssessments =
            OnlandVisualTrashAssessments.ListByOnlandVisualTrashAssessmentAreaID(DbContext,
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID.Value);
        var onlandVisualTrashAssessmentArea = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea;
        onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScoreID =
            OnlandVisualTrashAssessmentAreas.CalculateBaselineScoreFromBackingData(onlandVisualTrashAssessments)?
                .OnlandVisualTrashAssessmentScoreID;

        onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScoreID =
            OnlandVisualTrashAssessments.CalculateProgressScore(onlandVisualTrashAssessments)?
                .OnlandVisualTrashAssessmentScoreID;

        await DbContext.SaveChangesAsync();
        return Ok();
    }
}