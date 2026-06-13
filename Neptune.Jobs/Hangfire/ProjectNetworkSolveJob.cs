using System.Diagnostics;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.Common.Email;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Jobs.EsriAsynchronousJob;
using Neptune.Jobs.Services;

namespace Neptune.Jobs.Hangfire
{
    public class ProjectNetworkSolveJob(
        ILogger<ProjectNetworkSolveJob> logger,
        IWebHostEnvironment webHostEnvironment,
        NeptuneDbContext dbContext,
        IOptions<NeptuneJobConfiguration> neptuneJobConfiguration,
        SitkaSmtpClientService sitkaSmtpClientService,
        NereidService nereidService,
        OCGISService ocgisService,
        QGISAPIService qgisApiService)
    {
        public const string JobName = "Nereid Planned Project Network Solve";

        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly NeptuneJobConfiguration _neptuneJobConfiguration = neptuneJobConfiguration.Value;

        public async Task RunNetworkSolveForProject(int projectID, int projectNetworkSolveHistoryID)
        {
            var project = dbContext.Projects.AsNoTracking().SingleOrDefault(x => x.ProjectID == projectID);
            if (project == null)
            {
                throw new NullReferenceException($"Project with ID {projectID} does not exist!");
            }
            var projectNetworkSolveHistory = dbContext.ProjectNetworkSolveHistories.Include(x => x.RequestedByPerson).First(x => x.ProjectNetworkSolveHistoryID == projectNetworkSolveHistoryID);
            var treatmentBMPs = dbContext.TreatmentBMPs.Include(x => x.Delineation).AsNoTracking().Where(x => x.ProjectID == projectID);
            var projectRegionalSubbasinIDs = treatmentBMPs.Select(x => x.RegionalSubbasinID).Distinct().ToList();

            var regionalSubbasinIDs = dbContext.vRegionalSubbasinUpstreams.AsNoTracking()
                .Where(x => projectRegionalSubbasinIDs.Contains(x.PrimaryKey) && x.RegionalSubbasinID.HasValue).Select(x => x.RegionalSubbasinID.Value).ToList();
            var projectDistributedDelineationIDs = treatmentBMPs.Select(x => x.Delineation).Where(x => x.DelineationTypeID == (int)DelineationTypeEnum.Distributed).Select(x => x.DelineationID).ToList();
            try
            {
                //Get our LGUs
                await LoadGeneratingUnitRefreshImpl(projectID, regionalSubbasinIDs);
                //Get our HRUs
                await HRURefreshImpl(projectID);
                await nereidService.ProjectNetworkSolve(dbContext, false, projectID, regionalSubbasinIDs, projectDistributedDelineationIDs);
                projectNetworkSolveHistory.ProjectNetworkSolveHistoryStatusTypeID = (int)ProjectNetworkSolveHistoryStatusTypeEnum.Succeeded;
                projectNetworkSolveHistory.LastUpdated = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                await SendProjectNetworkSolveTerminalStatusEmail(projectNetworkSolveHistory.RequestedByPerson, project, true, null);
            }
            catch (Exception ex)
            {
                projectNetworkSolveHistory.ProjectNetworkSolveHistoryStatusTypeID = (int)ProjectNetworkSolveHistoryStatusTypeEnum.Failed;
                projectNetworkSolveHistory.LastUpdated = DateTime.UtcNow;
                projectNetworkSolveHistory.ErrorMessage = ex.Message;
                await dbContext.SaveChangesAsync();
                await SendProjectNetworkSolveTerminalStatusEmail(projectNetworkSolveHistory.RequestedByPerson, project, false, ex.Message);
                throw;
            }
        }

        private async Task SendProjectNetworkSolveTerminalStatusEmail(Person requestPerson,
            Project project, bool successful, string errorMessage)
        {
            var projectName = project.ProjectName;
            var subject = successful ? $"Modeled Results calculated for Project: {projectName}" : $"Model Results calculation failed for Project: {projectName}";
            var requestPersonEmail = requestPerson.Email;
            var errorContext = $"<br /><br/>See the provided error message for more details:\n {errorMessage}";
            var planningURL = $"{_neptuneJobConfiguration.OcStormwaterToolsBaseUrl}/projects/edit/{project.ProjectID}/stormwater-treatments/modeled-performance-and-metrics";
            var message = $@"
<div style='font-size: 12px; font-family: Arial'>
<strong>{subject}</strong><br />
<br />
Model results calculation for Project:{projectName} has completed{(!successful ? " but encountered errors." : ".")}
<br /><br />
You can view the results or trigger another network solve <a href='{planningURL}'>here</a>.

{(!successful ? errorContext : "")}
";
            // Create Notification
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_neptuneJobConfiguration.DoNotReplyEmail),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };

            mailMessage.To.Add(requestPersonEmail);

            if (!successful)
            {
                foreach (var email in People.GetEmailAddressesForAdminsThatReceiveSupportEmails(dbContext))
                {
                    mailMessage.CC.Add(email);
                }
            }

            await sitkaSmtpClientService.Send(mailMessage);
        }

        private async Task LoadGeneratingUnitRefreshImpl(int projectID, List<int> regionalSubbasinIDs)
        {
            await qgisApiService.GeneratePLGUs(new GenerateProjectLoadGeneratingUnitRequestDto() { ProjectID = projectID, RegionalSubbasinIDs = regionalSubbasinIDs });
        }

        private async Task HRURefreshImpl(int projectID)
        {
            // collect the load generating units that require updates, which will be all for the Project
            // group them by Model basin so requests to the HRU service are spatially bounded. It is possible that a number of these won't have a model basin, but if the system is used in a logical manner any of these that don't have model basins will be in an rsb that is in North OC, so technically will still be spatially bounded
            // and batch them for processing 25 at a time so requests are small.
            logger.LogInformation($"Processing '{JobName}'-HRURefresh for {projectID}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var loadGeneratingUnitsToUpdate = dbContext.vProjectLoadGeneratingUnitUpdateCandidates.Where(x => x.ProjectID == projectID).ToList();
            var loadGeneratingUnitsToUpdateGroupedBySpatialGridUnit = loadGeneratingUnitsToUpdate.GroupBy(x => x.SpatialGridUnitID);

            foreach (var group in loadGeneratingUnitsToUpdateGroupedBySpatialGridUnit.OrderByDescending(x => x.Count()))
            {
                try
                {
                    var loadGeneratingUnitIDs = group.Select(x => x.PrimaryKey).ToList();
                    var hruResponseResult = await ProcessHRUs(group, ocgisService);

                    var hruResponseFeatures = hruResponseResult.HRUResponseFeatures;
                    if (!hruResponseFeatures.Any())
                    {
                        await dbContext.ProjectLoadGeneratingUnits.Where(x =>
                                loadGeneratingUnitIDs.Contains(x.ProjectLoadGeneratingUnitID))
                            .ExecuteUpdateAsync(x => x.SetProperty(y => y.IsEmptyResponseFromHRUService, true));

                        logger.LogWarning($"No data for PLGUs with these IDs: {string.Join(", ", loadGeneratingUnitIDs)}");
                    }

                    var projectHruCharacteristics = hruResponseFeatures.Select(x =>
                    {
                        var projectHRUCharacteristic = new ProjectHRUCharacteristic
                        {
                            ProjectLoadGeneratingUnitID = x.Attributes.QueryFeatureID,
                            ProjectID = projectID
                        };
                        HRURefreshJob.SetHRUCharacteristicProperties(x, projectHRUCharacteristic);
                        return projectHRUCharacteristic;
                    }).ToList();
                    dbContext.ProjectHRUCharacteristics.AddRange(projectHruCharacteristics);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // this batch failed, but we don't want to give up the whole job.
                    logger.LogWarning(ex.Message);
                }

                if (stopwatch.Elapsed.Minutes > 20)
                {
                    break;
                }

                if (stopwatch.Elapsed.Minutes > 20)
                {
                    break;
                }
            }

            stopwatch.Stop();

            // delete the old HRU logs
            await dbContext.Database.ExecuteSqlAsync($"EXEC dbo.pDeleteOldHRULogs");
        }

        public async Task<HRUResponseResult> ProcessHRUs(IEnumerable<ILoadGeneratingUnit> loadGeneratingUnitsGroup, OCGISService ocgisService)
        {
            var loadGeneratingUnits = loadGeneratingUnitsGroup.ToList();
            var loadGeneratingUnitIDs = loadGeneratingUnits.Select(y => y.PrimaryKey);
            var hruRequestFeatures = HruRequestFeatureHelpers.GetHRURequestFeatures(
                loadGeneratingUnits.ToDictionary(x => x.PrimaryKey,
                    x => x.Geometry), true);
            var hruLog = new HRULog()
            {
                RequestDate = DateTime.UtcNow
            };
            await dbContext.AddAsync(hruLog);
            await dbContext.SaveChangesAsync();
            // todo: probably would not need this anymore if we are reading from the HRULog since every request should have a HRULog
            await dbContext.ProjectLoadGeneratingUnits.Where(x => loadGeneratingUnitIDs.Contains(x.ProjectLoadGeneratingUnitID)).ExecuteUpdateAsync(x => x.SetProperty(y => y.DateHRURequested, DateTime.UtcNow).SetProperty(y => y.HRULogID, hruLog.HRULogID));
            var hruResponseResult = await ocgisService.RetrieveHRUResponseFeatures(hruRequestFeatures.ToList(), hruLog);
            return hruResponseResult;
        }

    }
}
