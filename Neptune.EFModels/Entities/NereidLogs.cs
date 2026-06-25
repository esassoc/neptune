using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class NereidLogs
{

    /// <summary>
    /// NPT-1068: Sitka-admin download link payload for the BMP detail Modeled BMP Performance
    /// panel. Returns null if the BMP has no associated NereidLog row.
    /// </summary>
    public static async Task<TreatmentBMPNereidLogContentDto?> GetLatestForTreatmentBMPAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var log = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(x => x.LastNereidLog)
            .SingleOrDefaultAsync();
        if (log == null)
        {
            return null;
        }
        return new TreatmentBMPNereidLogContentDto
        {
            NereidLogID = log.NereidLogID,
            NereidRequest = log.NereidRequest,
            NereidResponse = log.NereidResponse,
        };
    }
}