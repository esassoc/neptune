using Hangfire;
using Microsoft.Extensions.Logging;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;

namespace Neptune.Jobs.Hangfire;

public class TrashGeneratingUnitRefreshJob
{
    private readonly ILogger<TrashGeneratingUnitRefreshJob> _logger;
    private readonly OverlayAPIService _overlayApiService;

    public TrashGeneratingUnitRefreshJob(ILogger<TrashGeneratingUnitRefreshJob> logger, OverlayAPIService overlayApiService)
    {
        _logger = logger;
        _overlayApiService = overlayApiService;
    }

    // DisableConcurrentExecution: the nightly 22:30 scheduled refresh and any manually-enqueued run must not
    // overlap — the full-county overlay runs for hours and races pTrashGeneratingUnitDelete + insert. Defense
    // in depth — today the single Hangfire worker (WorkerCount = 1) already serializes jobs, but this survives
    // that ever being raised. No AutomaticRetry on purpose: the delete proc runs before insert, so a retry
    // would restart a multi-hour run (global default is 0).
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task RunJob()
    {
        await _overlayApiService.GenerateTGUs(new GenerateTrashGeneratingUnitRequestDto());
    }
}