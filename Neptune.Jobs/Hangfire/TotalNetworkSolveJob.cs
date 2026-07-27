using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.Jobs.Services;
using System.Text.Json.Nodes;

namespace Neptune.Jobs.Hangfire;

public class TotalNetworkSolveJob(
    IOptions<NeptuneJobConfiguration> configuration,
    ILogger<TotalNetworkSolveJob> logger,
    NeptuneDbContext dbContext,
    NereidService nereidService)
    : BlobStorageWritingJob<TotalNetworkSolveJob>(configuration, logger, dbContext)
{
    public const string ModelResultsFileName = "ModelResults.json";
    public const string BaselineModelResultsFileName = "BaselineModelResults.json";

    public async Task RunJob()
    {
        // NPT-1115: repair any SQL-invalid delineation geometry before the solve consumes it.
        // Invalid geometry breaks NetTopologySuite overlay operations and also blanks the
        // GeoServer Delineations WMS layer (SQL error 24144); the nightly full run is the
        // natural place to re-heal the whole table.
        await DbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDelineationMakeValid");

        // clear out all dirty nodes since the whole network is being run.
        await DbContext.DirtyModelNodes.ExecuteDeleteAsync();
        // clear out all the nereid results since we are rerunning it for the whole network
        await DbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteNereidResults");

        var networkSolveResultBaseline = await nereidService.TotalNetworkSolve(DbContext, true);
        var networkSolveResult = await nereidService.TotalNetworkSolve(DbContext, false);
        await CreateNereidResultsAsJsonFileAndPostToBlobStorage(networkSolveResultBaseline.NereidResults, BaselineModelResultsFileName);
        await CreateNereidResultsAsJsonFileAndPostToBlobStorage(networkSolveResult.NereidResults, ModelResultsFileName);

        // clear out old nereid logs
        await DbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDeleteOldNereidLogs");
    }

    private async Task CreateNereidResultsAsJsonFileAndPostToBlobStorage(IEnumerable<NereidResult> nereidResults, string blobFilename)
    {
        var list = nereidResults.Select(x =>
        {
            var jsonObject = GeoJsonSerializer.Deserialize<JsonObject>(x.FullResponse);
            jsonObject["TreatmentBMPID"] = x.TreatmentBMPID;
            jsonObject["WaterQualityManagementPlanID"] = x.WaterQualityManagementPlanID;
            jsonObject["DelineationID"] = x.DelineationID;
            jsonObject["RegionalSubbasinID"] = x.RegionalSubbasinID;
            return jsonObject;
        }).ToList();

        await SerializeAndUploadToBlobStorage(list, blobFilename);
    }
}