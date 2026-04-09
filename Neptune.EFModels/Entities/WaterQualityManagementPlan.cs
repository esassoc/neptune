using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities
{
    public partial class WaterQualityManagementPlan
    {
        public string MaintenanceContactAddressToString()
        {
            return string.Join(" ",
                new List<string>
                {
                    MaintenanceContactAddress1,
                    MaintenanceContactAddress2,
                    MaintenanceContactCity,
                    MaintenanceContactState,
                    MaintenanceContactZip
                }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public string GetLatestOandMVerificationDate()
        {
            return this.WaterQualityManagementPlanVerifies.Count > 0 ? WaterQualityManagementPlanVerifies.Select(x => x.LastEditedDate).Max().ToString("MM/dd/yyyy") : "";
        }

        //public string GetLatestOandMVerificationUrl()
        //{
        //    return WaterQualityManagementPlanVerifies.Count > 0 ? WaterQualityManagementPlanVerifies.Single(x => x.LastEditedDate == this.WaterQualityManagementPlanVerifies.Select(y => y.LastEditedDate).Max()).GetDetailUrl() : string.Empty;
        //}


        public bool HasAllRequiredDocuments()
        {
            foreach (var documentType in WaterQualityManagementPlanDocumentType.All.Where(x => x.IsRequired))
            {
                if (WaterQualityManagementPlanDocuments.All(x => x.WaterQualityManagementPlanDocumentType != documentType))
                {
                    return false;
                }
            }

            return true;
        }

        public Geometry GetCatchmentGeometry()
        {
            return WaterQualityManagementPlanBoundary?.GeometryNative;
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.DirtyModelNodes.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.HRUCharacteristics
                .Include(x => x.LoadGeneratingUnit)
                .Where(x => x.LoadGeneratingUnit.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.LoadGeneratingUnits
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.NereidResults.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.ProjectHRUCharacteristics
                .Include(x => x.ProjectLoadGeneratingUnit)
                .Where(x => x.ProjectLoadGeneratingUnit.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifyQuickBMPs
                .Include(x => x.QuickBMP)
                .Where(x => x.QuickBMP.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.QuickBMPs.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.SourceControlBMPs.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnits
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnit4326s
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            foreach (var treatmentBMP in dbContext.TreatmentBMPs.Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ToList())
            {
                treatmentBMP.WaterQualityManagementPlanID = null;
            }
            await dbContext.SaveChangesAsync();

            await dbContext.WaterQualityManagementPlanBoundaries
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            var documentIDs = await dbContext.WaterQualityManagementPlanDocuments
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .Select(x => x.WaterQualityManagementPlanDocumentID).ToListAsync();
            if (documentIDs.Any())
            {
                await dbContext.WaterQualityManagementPlanDocumentVectorStores
                    .Where(x => documentIDs.Contains(x.WaterQualityManagementPlanDocumentID)).ExecuteDeleteAsync();
                await dbContext.WaterQualityManagementPlanExtractionResults
                    .Where(x => documentIDs.Contains(x.WaterQualityManagementPlanDocumentID)).ExecuteDeleteAsync();
            }
            await dbContext.WaterQualityManagementPlanDocuments
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID)
                .ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanParcels
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifyPhotos
                .Include(x => x.WaterQualityManagementPlanVerify)
                .Where(x => x.WaterQualityManagementPlanVerify.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifySourceControlBMPs
                .Include(x => x.WaterQualityManagementPlanVerify)
                .Where(x => x.WaterQualityManagementPlanVerify.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs
                .Include(x => x.WaterQualityManagementPlanVerify)
                .Where(x => x.WaterQualityManagementPlanVerify.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            await dbContext.WaterQualityManagementPlanVerifies
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
            
            await dbContext.WaterQualityManagementPlans
                .Where(x => x.WaterQualityManagementPlanID == WaterQualityManagementPlanID).ExecuteDeleteAsync();
        }
    }
}
