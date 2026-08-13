using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities
{
    public partial class WaterQualityManagementPlanDocument
    {
        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            // The AI extraction result points at the document it was extracted from, with a
            // plain FK (no ON DELETE CASCADE), so the document delete fails outright once an
            // extraction has run against it. Drop the derived result first — it is a cache of
            // what the AI read out of this PDF, so it cannot outlive its source document.
            await dbContext.WaterQualityManagementPlanExtractionResults
                .Where(x => x.WaterQualityManagementPlanDocumentID == WaterQualityManagementPlanDocumentID)
                .ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanDocuments
                .Where(x => x.WaterQualityManagementPlanDocumentID == WaterQualityManagementPlanDocumentID)
                .ExecuteDeleteAsync();
        }
    }
}
