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
    }
}