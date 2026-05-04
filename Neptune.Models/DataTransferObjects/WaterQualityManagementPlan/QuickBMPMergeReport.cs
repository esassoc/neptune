using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// Single skipped-BMP report entry — names the proposed BMP and lists the missing
/// fields that prevented auto-creation. NPT-1051: surfaced through the section-save
/// response so the wizard's BMPs Save can warn the reviewer about BMPs that couldn't
/// be auto-created (typically: Treatment BMP Type not extracted by Claude and not
/// picked by the reviewer before Save).
/// </summary>
public class QuickBMPMergeSkipDto
{
    public string ProposedName { get; set; } = string.Empty;

    public List<string> Reasons { get; set; } = new();
}

/// <summary>
/// Backend-only report returned by <c>QuickBMPs.MergeWithReportAsync</c>. The Skipped
/// list rides out to the SPA via <see cref="WaterQualityManagementPlanSectionSaveResponseDto.SkippedBMPs"/>.
/// </summary>
public class QuickBMPMergeReport
{
    public List<QuickBMPMergeSkipDto> Skipped { get; set; } = new();
}
