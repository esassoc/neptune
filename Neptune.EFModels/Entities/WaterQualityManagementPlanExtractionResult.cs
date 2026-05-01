namespace Neptune.EFModels.Entities;

// NPT-1051: ApplyDraftOverlay / ClearDraftOverlay / Approve instance methods removed.
// The wizard no longer round-trips draft state through the DB — per-field marks are SPA-local
// and section saves write through to the live WQMP. The DraftOverlayJson, ApprovedDate, and
// ApprovedByPersonID columns are deprecated; column drop is deferred to a follow-up
// data-cleanup ticket per the story's Cleanup section.
public partial class WaterQualityManagementPlanExtractionResult
{
}
