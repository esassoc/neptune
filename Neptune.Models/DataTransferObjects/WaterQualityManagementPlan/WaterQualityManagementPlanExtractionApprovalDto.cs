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

/// <summary>
/// Response from the WQMP AI extraction approve endpoint. Carries the updated WQMP
/// alongside any QuickBMPs that couldn't be auto-created (NPT-1020 item 3) so the
/// SPA can surface a non-blocking warning naming each skipped BMP and the missing
/// fields. Skipped BMPs do NOT roll back the rest of the approval.
/// </summary>
public class WaterQualityManagementPlanExtractionApprovalResponseDto
{
    public WaterQualityManagementPlanDto? WaterQualityManagementPlan { get; set; }

    public List<QuickBMPMergeSkipDto> SkippedBMPs { get; set; } = new();
}

/// <summary>
/// Single skipped-BMP report entry — names the proposed BMP and lists the missing
/// fields that prevented auto-creation.
/// </summary>
public class QuickBMPMergeSkipDto
{
    public string ProposedName { get; set; } = string.Empty;

    public List<string> Reasons { get; set; } = new();
}

/// <summary>
/// Backend-only report returned by <c>QuickBMPs.MergeWithReportAsync</c>. The Skipped
/// list rides out to the SPA via <see cref="WaterQualityManagementPlanExtractionApprovalResponseDto.SkippedBMPs"/>.
/// </summary>
public class QuickBMPMergeReport
{
    public List<QuickBMPMergeSkipDto> Skipped { get; set; } = new();
}
