using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("field-visits/{fieldVisitID}/assessments")]
public class TreatmentBMPAssessmentByFieldVisitController(NeptuneDbContext dbContext, ILogger<TreatmentBMPAssessmentByFieldVisitController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<TreatmentBMPAssessmentByFieldVisitController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("{treatmentBMPAssessmentTypeID}")]
    [UserViewFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<TreatmentBMPAssessmentDetailDto?>> GetByType([FromRoute] int fieldVisitID, [FromRoute] int treatmentBMPAssessmentTypeID)
    {
        if (!Enum.IsDefined(typeof(TreatmentBMPAssessmentTypeEnum), treatmentBMPAssessmentTypeID))
        {
            return BadRequest($"Unknown TreatmentBMPAssessmentType {treatmentBMPAssessmentTypeID}.");
        }

        var dto = await TreatmentBMPAssessments.GetByFieldVisitIDAndTypeAsDetailDtoAsync(DbContext, fieldVisitID, (TreatmentBMPAssessmentTypeEnum)treatmentBMPAssessmentTypeID);
        return Ok(dto);
    }

    [HttpPost("{treatmentBMPAssessmentTypeID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<TreatmentBMPAssessmentDetailDto>> Create([FromRoute] int fieldVisitID, [FromRoute] int treatmentBMPAssessmentTypeID)
    {
        if (!Enum.IsDefined(typeof(TreatmentBMPAssessmentTypeEnum), treatmentBMPAssessmentTypeID))
        {
            return BadRequest($"Unknown TreatmentBMPAssessmentType {treatmentBMPAssessmentTypeID}.");
        }

        var dto = await TreatmentBMPAssessments.CreateForFieldVisitAsync(DbContext, fieldVisitID, (TreatmentBMPAssessmentTypeEnum)treatmentBMPAssessmentTypeID);
        return Ok(dto);
    }
}
