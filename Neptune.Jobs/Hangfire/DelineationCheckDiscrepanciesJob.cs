using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neptune.EFModels.Entities;

namespace Neptune.Jobs.Hangfire;

public class DelineationCheckDiscrepanciesJob(NeptuneDbContext dbContext, ILogger<DelineationCheckDiscrepanciesJob> logger)
{
    public async Task RunJob()
    {
        logger.LogInformation("Running pDelineationMarkThoseThatHaveDiscrepancies...");
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDelineationMarkThoseThatHaveDiscrepancies");
        logger.LogInformation("pDelineationMarkThoseThatHaveDiscrepancies complete.");
    }
}
