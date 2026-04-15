namespace Neptune.EFModels.Entities;

public partial class WaterQualityManagementPlanExtractionResult
{
    public void ApplyDraftOverlay(string draftOverlayJson, int editorPersonID, DateTime now)
    {
        if (ApprovedDate.HasValue)
        {
            throw new InvalidOperationException("Cannot save a draft on an extraction result that has already been approved.");
        }

        DraftOverlayJson = draftOverlayJson;
        DraftUpdatedByPersonID = editorPersonID;
        DraftUpdatedDate = now;
    }

    public void ClearDraftOverlay()
    {
        DraftOverlayJson = null;
        DraftUpdatedByPersonID = null;
        DraftUpdatedDate = null;
    }

    public void Approve(int approverPersonID, DateTime now)
    {
        if (ApprovedDate.HasValue)
        {
            throw new InvalidOperationException("Extraction result has already been approved and cannot be re-approved.");
        }

        ApprovedByPersonID = approverPersonID;
        ApprovedDate = now;
        ClearDraftOverlay();
    }
}
