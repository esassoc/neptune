using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common.Email;
using Neptune.EFModels.Entities;

namespace Neptune.Jobs.Hangfire
{
    public class LandUseBlockUploadBackgroundJob(
        NeptuneDbContext dbContext,
        IOptions<NeptuneJobConfiguration> neptuneJobConfiguration,
        SitkaSmtpClientService sitkaSmtpClientService)
    {
        private readonly NeptuneJobConfiguration _neptuneJobConfiguration = neptuneJobConfiguration.Value;

        public async Task RunJob(int personID)
        {
            var person = People.GetByID(dbContext, personID);

            if (person == null)
            {
                throw new InvalidOperationException("PersonID must be valid!");
            }
            try
            {
                // NPT-1077: validation logic is now shared with the controller's staging-report
                // endpoint so the SPA approve page surfaces the same errors the email used to.
                // The controller re-runs ValidateStagings on approve and 400s with the report if
                // errors exist, so in normal flow the job's pre-validation here is a safety net.
                var landUseBlockStagings = LandUseBlockStagings.ListByPersonID(dbContext, personID);

                // Copilot review on PR #541: if staging is empty by the time the job runs (e.g.
                // a race where the user discarded the staging between approve and job execution),
                // emit an error instead of cascading through to the "successful import" email.
                var errorList = landUseBlockStagings.Count == 0
                    ? new List<string> { "There are no staged Land Use Blocks to process. The staging table was empty when the import job ran." }
                    : LandUseBlockStagings.ValidateStagings(landUseBlockStagings);

                if (!errorList.Any())
                {
                    var landUseBlocksToUpload = landUseBlockStagings.Select(LandUseBlocks.FromStaging).ToList();
                    var stormwaterJurisdictionIDsToClear =
                        landUseBlocksToUpload.Select(x => x.StormwaterJurisdictionID).Distinct();
                    await dbContext.TrashGeneratingUnits
                        .Where(x => stormwaterJurisdictionIDsToClear.Contains(x.StormwaterJurisdictionID))
                        .ExecuteUpdateAsync(x => x.SetProperty(y => y.LandUseBlockID, (int?)null));
                    await dbContext.TrashGeneratingUnit4326s
                        .Where(x => stormwaterJurisdictionIDsToClear.Contains(x.StormwaterJurisdictionID))
                        .ExecuteUpdateAsync(x => x.SetProperty(y => y.LandUseBlockID, (int?)null));
                    await dbContext.LandUseBlocks
                        .Where(x => stormwaterJurisdictionIDsToClear.Contains(x.StormwaterJurisdictionID))
                        .ExecuteDeleteAsync();

                    await dbContext.LandUseBlocks.AddRangeAsync(landUseBlocksToUpload);
                    await dbContext.SaveChangesAsync();

                    var body = "Your Land Use Block Upload has been processed. The updated Land Use Blocks are now in the Orange County Stormwater Tools system. It may take up to 24 hours for updated Trash Results to appear in the system.";

                    var mailMessage = new MailMessage
                    {
                        Subject = "Land Use Block Upload Results",
                        Body = body,
                        From = new MailAddress(_neptuneJobConfiguration.DoNotReplyEmail, "Orange County Stormwater Tools")
                    };

                    mailMessage.To.Add(person.Email);
                    await sitkaSmtpClientService.Send(mailMessage);
                }
                else
                {
                    var body =
                        "Your Land Use Block upload had errors. Please review the following report, correct the errors, and try again: \n" +
                        string.Join("\n", errorList);

                    var mailMessage = new MailMessage
                    {
                        Subject = "Land Use Block Upload Error",
                        Body = body,
                        From = new MailAddress(_neptuneJobConfiguration.DoNotReplyEmail, "Orange County Stormwater Tools")
                    };

                    mailMessage.To.Add(person.Email);
                    await sitkaSmtpClientService.Send(mailMessage);
                }

                await dbContext.Database.ExecuteSqlRawAsync($"EXEC dbo.pLandUseBlockStagingDeleteByPersonID @PersonID = {personID}");
            }
            catch (Exception)
            {
                var body =
                    "There was an unexpected system error during processing of your Land Use Block Upload. The Orange County Stormwater Tools development team will investigate and be in touch when this issue is resolved.";

                var mailMessage = new MailMessage
                {
                    Subject = "Land Use Block Upload Error",
                    Body = body,
                    From = new MailAddress(_neptuneJobConfiguration.DoNotReplyEmail, "Orange County Stormwater Tools")
                };

                mailMessage.To.Add(person.Email);
                await sitkaSmtpClientService.Send(mailMessage);

                throw;
            }
        }
    }
}