using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.WebService;

namespace Neptune.ExternalAPI.Controllers;

[Authorize]
[ApiController]
[Tags("Water Quality Management Plans")]
[Route("water-quality-management-plan")]
public class WaterQualityManagementPlanController(NeptuneDbContext dbContext) : ControllerBase
{
    [HttpGet("attributes")]
    [EndpointSummary("WQMP Attributes")]
    [EndpointDescription("This table includes summary attributes of WQMP sites helpful for filtering and reporting, and each row is a single WQMP project site. Additional summary attributes may be added to this table in the future.  In its current form (as of March, 2020) this table does not yet support identification of treatment achieved via entries to the Other Structural Facility table available in the OCST interface, but standalone BMPs in the 'Treatment Facility Attributes' do store an association to a WQMP if such an association exists.  Future reporting capabilities are planned to include treatment accounted for in the Other Structural Facility interface.")]
    [ProducesResponseType(typeof(IEnumerable<WaterQualityManagementPlanAttributesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanAttributesDto>>> Attributes()
    {
        var data = (await dbContext.vPowerBIWaterQualityManagementPlans.ToListAsync()).Select(x => new WaterQualityManagementPlanAttributesDto
        {
            WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
            Name = x.WaterQualityManagementPlanName,
            Jurisdiction = x.OrganizationName,
            Status = x.WaterQualityManagementPlanStatusDisplayName,
            DevelopmentType = x.WaterQualityManagementPlanDevelopmentTypeDisplayName,
            LandUse = x.WaterQualityManagementPlanLandUseDisplayName,
            PermitTerm = x.WaterQualityManagementPlanPermitTermDisplayName,
            ApprovalDate = x.ApprovalDate,
            DateOfConstruction = x.DateOfConstruction,
            HydromodificationApplies = x.HydromodificationAppliesDisplayName,
            HydrologicSubarea = x.HydrologicSubareaName,
            RecordedWQMPAreaInAcres = x.RecordedWQMPAreaInAcres,
            TrashCaptureStatus = x.TrashCaptureStatusTypeDisplayName,
            TrashCaptureEffectiveness = x.TrashCaptureEffectiveness,
            ModelingApproach = x.ModelingApproach
        });
        return Ok(data);
    }

    [HttpGet("om-verifications")]
    [EndpointSummary("Water Quality Management Plan O&M Verifications")]
    [EndpointDescription("An inventory of O&M Verification visits for Water Quality Management Plans (WQMPs)")]
    [ProducesResponseType(typeof(IEnumerable<WaterQualityManagementPlanOMVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanOMVerificationDto>>> OMVerifications()
    {
        var data = (await dbContext.vPowerBIWaterQualityManagementPlanOAndMVerifications.ToListAsync()).Select(x => new WaterQualityManagementPlanOMVerificationDto
        {
            WQMPID = x.PrimaryKey,
            WQMPName = x.WQMPName,
            Jurisdiction = x.Jurisdiction,
            VerificationDate = x.VerificationDate.ToString(CultureInfo.InvariantCulture),
            LastEditedDate = x.LastEditedDate.ToString(CultureInfo.InvariantCulture),
            LastEditedBy = x.LastEditedBy,
            TypeOfVerification = x.TypeOfVerification,
            VisitStatus = x.VisitStatus,
            VerificationStatus = x.VerificationStatus,
            SourceControlCondition = x.SourceControlCondition,
            EnforcementOrFollowupActions = x.EnforcementOrFollowupActions,
            DraftOrFinalized = x.DraftOrFinalized
        });
        return Ok(data);
    }
}
