namespace Neptune.EFModels.Entities;

// NPT-1051: ApplyDraftOverlay / ClearDraftOverlay / Approve instance methods removed.
// The wizard no longer round-trips draft state through the DB — per-field marks are SPA-local
// and section saves write through to the live WQMP. The legacy DraftOverlayJson, ApprovedDate,
// ApprovedByPersonID, DraftUpdatedByPersonID, and DraftUpdatedDate columns and their related
// FKs were dropped from the schema in this change.
public partial class WaterQualityManagementPlanExtractionResult
{
}
