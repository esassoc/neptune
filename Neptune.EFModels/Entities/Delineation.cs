using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities
{
    public partial class Delineation
    {
        public string GetGeometry4326GeoJson()
        {
            var attributesTable = new AttributesTable
            {
                { "DelineationID", DelineationID },
                { "TreatmentBMPID", TreatmentBMPID }
            };

            var feature = new Feature(DelineationGeometry4326, attributesTable);
            return GeoJsonSerializer.Serialize(feature);
        }

        public void MarkAsVerified(Person currentPerson)
        {
            IsVerified = true;
            DateLastVerified = DateTime.UtcNow;
            VerifiedByPersonID = currentPerson.PersonID;
        }

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.DelineationOverlaps.Where(x => x.DelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.DelineationOverlaps.Where(x => x.OverlappingDelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.DirtyModelNodes.Where(x => x.DelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.HRUCharacteristics.Include(x => x.LoadGeneratingUnit).Where(x => x.LoadGeneratingUnit.DelineationID == DelineationID)
                .ExecuteDeleteAsync();
            await dbContext.LoadGeneratingUnits.Where(x => x.DelineationID == DelineationID)
                .ExecuteDeleteAsync();
            await dbContext.NereidResults.Where(x => x.DelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.ProjectHRUCharacteristics
                .Include(x => x.ProjectLoadGeneratingUnit)
                .Where(x => x.ProjectLoadGeneratingUnit.DelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits.Where(x => x.DelineationID == DelineationID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnit4326s.Where(x => x.DelineationID == DelineationID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnits.Where(x => x.DelineationID == DelineationID)
                .ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults.Where(x => x.DelineationID == DelineationID).ExecuteDeleteAsync();
            await dbContext.Delineations.Where(x => x.DelineationID == DelineationID).ExecuteDeleteAsync();
        }

        public static async Task DeleteFull(NeptuneDbContext dbContext, int delineationID)
        {
            await dbContext.DelineationOverlaps.Where(x => x.DelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.DelineationOverlaps.Where(x => x.OverlappingDelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.DirtyModelNodes.Where(x => x.DelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.HRUCharacteristics.Include(x => x.LoadGeneratingUnit).Where(x => x.LoadGeneratingUnit.DelineationID == delineationID)
                .ExecuteDeleteAsync();
            await dbContext.LoadGeneratingUnits.Where(x => x.DelineationID == delineationID)
                .ExecuteDeleteAsync();
            await dbContext.NereidResults.Where(x => x.DelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.ProjectHRUCharacteristics
                .Include(x => x.ProjectLoadGeneratingUnit)
                .Where(x => x.ProjectLoadGeneratingUnit.DelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits.Where(x => x.DelineationID == delineationID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnit4326s.Where(x => x.DelineationID == delineationID)
                .ExecuteDeleteAsync();
            await dbContext.TrashGeneratingUnits.Where(x => x.DelineationID == delineationID)
                .ExecuteDeleteAsync();
            await dbContext.ProjectNereidResults.Where(x => x.DelineationID == delineationID).ExecuteDeleteAsync();
            await dbContext.Delineations.Where(x => x.DelineationID == delineationID).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Set-based equivalent of <see cref="DeleteFull"/> for many delineations at once. Issues a fixed number of
        /// ExecuteDelete statements (one per child table) regardless of how many delineations are passed, instead of
        /// the ~12 statements per delineation that calling DeleteFull in a loop produces. This matters for large
        /// jurisdiction re-uploads (e.g. 1,000+ delineations, which would otherwise be ~13k sequential roundtrips).
        /// IDs are chunked to stay under SQL Server's ~2,100 parameter cap on the generated IN (...) clauses.
        /// </summary>
        public static async Task DeleteFullForMany(NeptuneDbContext dbContext, List<int> delineationIDs)
        {
            if (delineationIDs == null || delineationIDs.Count == 0)
            {
                return;
            }

            foreach (var chunk in delineationIDs.Chunk(2000))
            {
                var ids = chunk.ToList();
                await dbContext.DelineationOverlaps.Where(x => ids.Contains(x.DelineationID)).ExecuteDeleteAsync();
                await dbContext.DelineationOverlaps.Where(x => ids.Contains(x.OverlappingDelineationID)).ExecuteDeleteAsync();
                await dbContext.DirtyModelNodes.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.HRUCharacteristics.Where(x => x.LoadGeneratingUnit.DelineationID != null && ids.Contains(x.LoadGeneratingUnit.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.LoadGeneratingUnits.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.NereidResults.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.ProjectHRUCharacteristics.Where(x => x.ProjectLoadGeneratingUnit.DelineationID != null && ids.Contains(x.ProjectLoadGeneratingUnit.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.ProjectLoadGeneratingUnits.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.TrashGeneratingUnit4326s.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.TrashGeneratingUnits.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.ProjectNereidResults.Where(x => x.DelineationID != null && ids.Contains(x.DelineationID.Value)).ExecuteDeleteAsync();
                await dbContext.Delineations.Where(x => ids.Contains(x.DelineationID)).ExecuteDeleteAsync();
            }
        }
    }
}