using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities
{
    public partial class MaintenanceRecord
    {

        public async Task DeleteFull(NeptuneDbContext dbContext)
        {
            await dbContext.MaintenanceRecordObservationValues
                .Include(x => x.MaintenanceRecordObservation)
                .Where(x => x.MaintenanceRecordObservation.MaintenanceRecordID == MaintenanceRecordID)
                .ExecuteDeleteAsync();
            await dbContext.MaintenanceRecordObservations.Where(x => x.MaintenanceRecordID == MaintenanceRecordID)
                .ExecuteDeleteAsync();
            await dbContext.MaintenanceRecords.Where(x => x.MaintenanceRecordID == MaintenanceRecordID)
                .ExecuteDeleteAsync();
        }
    }
}