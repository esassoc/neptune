using Hangfire;
using Microsoft.Extensions.Logging;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;

namespace Neptune.Jobs.Hangfire
{
    public class LoadGeneratingUnitRefreshJob
    {
        private readonly ILogger<LoadGeneratingUnitRefreshJob> _logger;
        private readonly QGISAPIService _qgisApiService;

        public LoadGeneratingUnitRefreshJob(ILogger<LoadGeneratingUnitRefreshJob> logger, QGISAPIService qgisAPIService)
        {
            _logger = logger;
            _qgisApiService = qgisAPIService;
        }

        // DisableConcurrentExecution: LGU refreshes are enqueued on every delineation/BMP/WQMP boundary edit and
        // chained from RSB refreshes; two overlapping QGIS overlay runs would race the delete-then-insert stored
        // procs. Defense in depth — today the single Hangfire worker (WorkerCount = 1) already serializes jobs,
        // but this survives that ever being raised. Timeout is generous because a full refresh runs for hours;
        // a delta refresh queued behind one should wait rather than fail fast. No AutomaticRetry on purpose:
        // the delete procs run before insert, so a retry would restart a multi-hour run (global default is 0).
        [DisableConcurrentExecution(timeoutInSeconds: 3600)]
        public async Task RunJob(int? loadGeneratingUnitRefreshAreaID)
        {
            await _qgisApiService.GenerateLGUs(new GenerateLoadGeneratingUnitRequestDto()
                { LoadGeneratingUnitRefreshAreaID = loadGeneratingUnitRefreshAreaID });
            if (!loadGeneratingUnitRefreshAreaID.HasValue) 
            {
                // no loadGeneratingUnitRefreshAreaID implies a full LGU refresh, so we need to run a Full HRU refresh
                BackgroundJob.Enqueue<HRURefreshJob>(x => x.RunJob(null));
            }
        }
    }
}