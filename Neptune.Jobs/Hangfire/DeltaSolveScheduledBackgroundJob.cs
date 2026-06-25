using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.Common.Email;
using Neptune.EFModels.Entities;

namespace Neptune.Jobs.Hangfire
{
    public class DeltaSolveScheduledBackgroundJob : ScheduledBackgroundJobBase<DeltaSolveScheduledBackgroundJob>
    {
        public const string JobName = "Nereid Delta Solve";

        public DeltaSolveScheduledBackgroundJob(ILogger<DeltaSolveScheduledBackgroundJob> logger,
            IWebHostEnvironment webHostEnvironment, NeptuneDbContext neptuneDbContext,
            IOptions<NeptuneJobConfiguration> neptuneJobConfiguration, SitkaSmtpClientService sitkaSmtpClientService) : base(JobName, logger, webHostEnvironment,
            neptuneDbContext, neptuneJobConfiguration, sitkaSmtpClientService)
        {
        }

        public override List<RunEnvironment> RunEnvironments => new() { RunEnvironment.Staging, RunEnvironment.Production };

        protected override void RunJobImplementation()
        {
            // This enqueues DeltaSolveJob and returns; the base wrapper's try/catch (and its failure email) only
            // covers enqueue-time errors, NOT exceptions thrown while DeltaSolveJob later runs on a worker — same
            // as TotalNetworkSolveScheduledBackgroundJob. A failing DeltaSolveJob surfaces as a Failed job in the
            // Hangfire dashboard and is retried via [AutomaticRetry] on DeltaSolveJob.RunJob.
            BackgroundJob.Enqueue<DeltaSolveJob>(x => x.RunJob());
        }
    }
}
