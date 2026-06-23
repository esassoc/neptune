using System.Linq.Expressions;
using Hangfire;
using Hangfire.Storage;
using Neptune.Common.DesignByContract;

namespace Neptune.Jobs.Hangfire
{
    public class HangfireJobScheduler
    {
        public static void ScheduleRecurringJobs()
        {
            var recurringJobIds = new List<string>();

            AddRecurringJob<TrashGeneratingUnitRefreshScheduledBackgroundJob>(TrashGeneratingUnitRefreshScheduledBackgroundJob.JobName, x => x.RunJob(null), MakeDailyUtcCronJobStringFromLocalTime(22, 30), recurringJobIds);

            // RL 12/18/24: Turning these off for now
            //AddRecurringJob<RegionalSubbasinRefreshScheduledBackgroundJob>("Refresh RSBs", x => x.RunJob(null), MakeWeeklyUtcCronJobStringFromLocalTime(1, 30, DayOfWeek.Saturday), recurringJobIds);

            AddRecurringJob<HRURefreshScheduledBackgroundJob>(HRURefreshScheduledBackgroundJob.JobName, x => x.RunJob(null), "0 0-4,14-23 * * 1-5", recurringJobIds);

            // Recalculate dirty model nodes on the same window as the HRU refresh. That window deliberately skips
            // 5-13 UTC, where the nightly Total Network Solve runs (1 AM PST = 9 UTC), so the delta solve never
            // collides with the nightly catch-all. This is decoupled from HRU so it runs regardless of whether the
            // HRU refresh completes its OCGIS loop.
            AddRecurringJob<DeltaSolveScheduledBackgroundJob>(DeltaSolveScheduledBackgroundJob.JobName, x => x.RunJob(null), "0 0-4,14-23 * * 1-5", recurringJobIds);

            AddRecurringJob<TotalNetworkSolveScheduledBackgroundJob>(TotalNetworkSolveScheduledBackgroundJob.JobName, x => x.RunJob(null), MakeDailyUtcCronJobStringFromLocalTime(1, 0), recurringJobIds);

            // Remove any jobs we haven't explicitly scheduled
            RemoveExtraneousJobs(recurringJobIds);
        }

        private static void AddRecurringJob<T>(string jobName, Expression<Action<T>> methodCallExpression,
            string cronExpression, ICollection<string> recurringJobIds)
        {
            RecurringJob.AddOrUpdate<T>(jobName, methodCallExpression, cronExpression);
            recurringJobIds.Add(jobName);
        }

        private static void RemoveExtraneousJobs(List<string> recurringJobIds)
        {
            using var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();
            var jobsToRemove = recurringJobs.Where(x => !recurringJobIds.Contains(x.Id)).ToList();
            foreach (var job in jobsToRemove)
            {
                RecurringJob.RemoveIfExists(job.Id);
            }
        }

        /// <summary>
        /// Hangfire defaults to a UTC time, so here convert from local time to UTC, then use the equivalent
        /// UTC time to create a cron string.
        /// 
        /// Since SetUpBackgroundHangfireJobs should be re-run when the webserver restarts, this should get
        /// updated enough to handle the problems associated with DST/UTC/TimeZone conversions. At the least,
        /// problems won't hang around for too long since AddOrUpdate will adjust the time to be the correct one
        /// after a DST change. -- SLG 03/16/2015
        /// </summary>
        private static string MakeDailyUtcCronJobStringFromLocalTime(int hour, int minute)
        {
            var utcCronTime = MakeUtcCronTime(hour, minute);
            return Cron.Daily(utcCronTime.Hour, utcCronTime.Minute);
        }

        private static string MakeWeeklyUtcCronJobStringFromLocalTime(int hour, int minute, DayOfWeek dayOfWeek)
        {
            var utcCronTime = MakeUtcCronTime(hour, minute);
            return Cron.Weekly(dayOfWeek, utcCronTime.Hour, utcCronTime.Minute);
        }

        private static string MakeYearlyUtcCronJobStringFromLocalTime(int month, int day, int hour, int minute)
        {
            var utcCronTime = MakeUtcCronTime(month, day, hour, minute);
            return Cron.Yearly(utcCronTime.Month, utcCronTime.Day, utcCronTime.Hour, utcCronTime.Minute);
        }

        private static DateTime MakeUtcCronTime(int hour, int minute)
        {
            var now = DateTime.Now;
            return MakeUtcCronTime(now.Year, now.Month, now.Day, hour, minute);
        }

        private static DateTime MakeUtcCronTime(int month, int day, int hour, int minute)
        {
            var now = DateTime.Now;
            return MakeUtcCronTime(now.Year, month, day, hour, minute);
        }

        public static DateTime MakeUtcCronTime(int year, int month, int day, int hour, int minute)
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"); // getting the timezone in PST
            var localCronTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);

            // Catch cases where time is invalid or ambiguous due to daylight savings
            // 3/8/20 2am becomes 3am (so 2am – 3am in invalid)
            // 11/1/20 2am becomes 1am(so 1am – 2am is ambiguous)
            if (tz.IsAmbiguousTime(localCronTime) || tz.IsInvalidTime(localCronTime))
            {
                localCronTime = localCronTime.Add(TimeSpan.Parse("1:01:00"));

                // Make sure we've fixed the issue
                Check.Ensure(!tz.IsAmbiguousTime(localCronTime));
                Check.Ensure(!tz.IsInvalidTime(localCronTime));
            }

            var utcCronTime = localCronTime - tz.GetUtcOffset(localCronTime);
            return utcCronTime;
        }
    }
}