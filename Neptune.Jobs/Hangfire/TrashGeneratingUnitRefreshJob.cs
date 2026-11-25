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
    // overlap — the full-county overlay takes a few minutes and both runs would fill the one staging table.
    // Defense in depth — today the single Hangfire worker (WorkerCount = 1) already serializes jobs, but this
    // survives that ever being raised. No AutomaticRetry (global default is 0): a failed run leaves the
    // previous units in place (NPT-1156) and the next nightly run tries again.
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    public async Task RunJob()
    {
        await _overlayApiService.GenerateTGUs(new GenerateTrashGeneratingUnitRequestDto());
    }
}