using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("onland-visual-trash-assessment-areas")]
public class OnlandVisualTrashAssessmentAreaController(
    NeptuneDbContext dbContext,
    ILogger<OnlandVisualTrashAssessmentAreaController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<OnlandVisualTrashAssessmentAreaController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<OnlandVisualTrashAssessmentAreaGridDto>>> List()
    {
        var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var onlandVisualTrashAssessmentAreaGridDtos = OnlandVisualTrashAssessmentAreas
            .ListByStormwaterJurisdictionIDList(DbContext, stormwaterJurisdictionIDs).Select(x => x.AsGridDto()).ToList();
        return Ok(onlandVisualTrashAssessmentAreaGridDtos);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<OnlandVisualTrashAssessmentAreaDetailDto> Get([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var onlandVisualTrashAssessmentAreaDetailDto = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID).AsDetailDto();
        return Ok(onlandVisualTrashAssessmentAreaDetailDto);
    }

    [HttpPut("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> Update([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaSimpleDto onlandVisualTrashAssessmentAreaDto)
    {
        await OnlandVisualTrashAssessmentAreas.Update(DbContext, onlandVisualTrashAssessmentAreaID, onlandVisualTrashAssessmentAreaDto);
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/onland-visual-trash-assessments")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<List<OnlandVisualTrashAssessmentGridDto>> ListAssessmentsByOVTAID([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var visualTrashAssessmentGridDtos = OnlandVisualTrashAssessments.ListByOnlandVisualTrashAssessmentAreaID(DbContext, onlandVisualTrashAssessmentAreaID).Select(x => x.AsGridDto());
        return Ok(visualTrashAssessmentGridDtos);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/parcel-geometries")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<List<ParcelGeometrySimpleDto>> GetParcelGeometries([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var onlandVisualTrashAssessmentArea = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        var geometries = ParcelGeometries.GetIntersected(DbContext,
            onlandVisualTrashAssessmentArea.TransectLine).Select(x => x.AsSimpleDto()).ToList();
        return Ok(geometries);
    }

    [HttpPost("{onlandVisualTrashAssessmentAreaID}/parcel-geometries")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> UpdateOnlandVisualTrashAssessmentWithParcels([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaGeometryDto onlandVisualTrashAssessmentAreaGeometryDto)
    {
        OnlandVisualTrashAssessmentAreas.UpdateGeometry(DbContext, onlandVisualTrashAssessmentAreaGeometryDto);
        await DbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/area-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<FeatureCollection> GetAreaAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var featureCollection = OnlandVisualTrashAssessmentAreas.GetAssessmentAreaByIDAsFeatureCollection(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok(featureCollection);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/transect-line-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<FeatureCollection> GetTransectLineAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var featureCollection = OnlandVisualTrashAssessmentAreas.GetTransectLineByIDAsFeatureCollection(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok(featureCollection);
    }

    [HttpGet("jurisdictions/{jurisdictionID}")]
    [AllowAnonymous]
    public ActionResult<List<OnlandVisualTrashAssessmentAreaSimpleDto>> ListByJurisdictionID([FromRoute] int jurisdictionID)
    {
        var onlandVisualTrashAssessmentAreaSimpleDtos =
            DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Where(
                    x => x.StormwaterJurisdictionID == jurisdictionID)
                .OrderBy(x => x.OnlandVisualTrashAssessmentAreaName)
                .Select(x => x.AsSimpleDto()).ToList();

        return Ok(onlandVisualTrashAssessmentAreaSimpleDtos);
    }

    [HttpPost("{onlandVisualTrashAssessmentAreaID}/move-assessments")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> MoveAssessments([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaMoveAssessmentsDto dto)
    {
        if (dto == null || dto.TargetOnlandVisualTrashAssessmentAreaID <= 0)
        {
            return BadRequest("A target OVTA Area must be specified.");
        }

        if (onlandVisualTrashAssessmentAreaID == dto.TargetOnlandVisualTrashAssessmentAreaID)
        {
            return BadRequest("Cannot move an OVTA Area's assessments to itself.");
        }

        var sourceArea = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        var targetArea = DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaID == dto.TargetOnlandVisualTrashAssessmentAreaID);
        if (targetArea == null)
        {
            return BadRequest("Target OVTA Area not found.");
        }

        if (sourceArea.StormwaterJurisdictionID != targetArea.StormwaterJurisdictionID)
        {
            return BadRequest("Source and target OVTA Areas must belong to the same Jurisdiction.");
        }

        if (!await CallingUser.CanEditJurisdiction(sourceArea.StormwaterJurisdictionID, DbContext))
        {
            return Forbid();
        }

        var hasInProgressAssessment = DbContext.OnlandVisualTrashAssessments.Any(x =>
            x.OnlandVisualTrashAssessmentAreaID == onlandVisualTrashAssessmentAreaID &&
            x.OnlandVisualTrashAssessmentStatusID != (int)OnlandVisualTrashAssessmentStatusEnum.Complete);
        if (hasInProgressAssessment)
        {
            return BadRequest("Cannot move assessments: the source OVTA Area has assessments still in progress. Finish or delete those assessments first.");
        }

        await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(DbContext, onlandVisualTrashAssessmentAreaID, dto.TargetOnlandVisualTrashAssessmentAreaID);

        return Ok();
    }

    [HttpDelete("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> Delete([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var area = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        if (!await CallingUser.CanEditJurisdiction(area.StormwaterJurisdictionID, DbContext))
        {
            return Forbid();
        }

        await OnlandVisualTrashAssessmentAreas.DeleteAreaAsync(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok();
    }

}