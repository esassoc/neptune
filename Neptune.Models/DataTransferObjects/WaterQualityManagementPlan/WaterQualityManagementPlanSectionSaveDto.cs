using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// NPT-1051: Body for the AI wizard's per-section "Save Location" button. Reframes the AI flow as
/// another data-entry method peer to the modal CRUD editors — section saves write accepted/edited
/// fields straight through to the live WQMP. The wizard reduces its per-field state map (pending →
/// AI value, accepted → AI value, edited → user value, rejected → live value) into a complete
/// <see cref="WaterQualityManagementPlanUpsertDto"/> built by overlaying resolved values onto the
/// live WQMP — server-side it's a single <c>UpdateAsync</c> call plus a parcel write.
/// Null-check on <c>WaterQualityManagementPlan</c> is enforced by the controller (with a custom
/// 400 message); a model-binding [Required] would short-circuit before that, returning a generic
/// ProblemDetails body the SPA's error path doesn't render.
/// </summary>
public class WaterQualityManagementPlanSectionSaveLocationDto
{
    public WaterQualityManagementPlanUpsertDto? WaterQualityManagementPlan { get; set; }

    public List<int> ParcelIDs { get; set; } = new();
}

/// <summary>
/// NPT-1051: Response from any of the section-save endpoints. Carries the updated WQMP DTO
/// (mirroring the modal CRUD editors' return shape) plus, for the BMPs section only, any QuickBMPs
/// that couldn't be auto-created. Skipped BMPs surface as a non-blocking SPA warning toast and do
/// not roll back the rest of the save.
/// </summary>
public class WaterQualityManagementPlanSectionSaveResponseDto
{
    public WaterQualityManagementPlanDto? WaterQualityManagementPlan { get; set; }

    public List<QuickBMPMergeSkipDto> SkippedBMPs { get; set; } = new();
}
