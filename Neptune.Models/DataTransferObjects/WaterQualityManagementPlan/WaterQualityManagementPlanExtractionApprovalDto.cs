using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// Payload for the WQMP AI extraction approve endpoint. Bundles the user-edited
/// WQMP root-field upsert (Steps 1+2 of the review workflow) together with the
/// list of QuickBMPs the user accepted on Step 3 (NPT-1047). Rejected and pending
/// BMP cards are filtered client-side and not sent.
/// </summary>
public class WaterQualityManagementPlanExtractionApprovalDto
{
    [Required]
    public WaterQualityManagementPlanUpsertDto? WaterQualityManagementPlan { get; set; }

    public List<QuickBMPUpsertDto> ApprovedQuickBMPs { get; set; } = new();
}
