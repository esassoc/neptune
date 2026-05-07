using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.Common.Email;
using Neptune.Common.Recaptcha;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("support-requests")]
public class SupportRequestController(
    NeptuneDbContext dbContext,
    ILogger<SupportRequestController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    SitkaSmtpClientService sitkaSmtpClientService)
    : SitkaController<SupportRequestController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("site-key")]
    [AllowAnonymous]
    public ActionResult<string> GetRecaptchaSiteKey()
    {
        // Public site key, safe to expose to anonymous callers; server still validates the token
        // against the secret on submit.
        return Ok(NeptuneConfiguration.GoogleRecaptchaV3Config?.SiteKey ?? string.Empty);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SupportRequestSubmissionResultDto>> Submit([FromBody] SupportRequestSubmissionDto submission)
    {
        if (submission == null
            || string.IsNullOrWhiteSpace(submission.Name)
            || string.IsNullOrWhiteSpace(submission.Email)
            || string.IsNullOrWhiteSpace(submission.Description)
            || submission.SupportRequestTypeID <= 0)
        {
            return BadRequest(new SupportRequestSubmissionResultDto { Success = false, Message = "Please fill out all required fields." });
        }

        var caller = await People.GetByIDAsDtoAsync(DbContext, CallingUser.PersonID);
        var isAnonymous = caller == null || CallingUser.PersonID <= 0 || caller.RoleID == (int)RoleEnum.Unassigned && string.IsNullOrEmpty(caller.GlobalID);

        // Anonymous users must clear the reCAPTCHA gate. Authenticated users skip it (mirrors legacy HelpController.ValidateRecaptcha).
        if (User.Identity?.IsAuthenticated != true)
        {
            var recaptchaConfig = NeptuneConfiguration.GoogleRecaptchaV3Config;
            if (recaptchaConfig != null && !string.IsNullOrWhiteSpace(recaptchaConfig.SecretKey))
            {
                if (string.IsNullOrWhiteSpace(submission.RecaptchaToken)
                    || !await RecaptchaValidator.IsValidResponse(recaptchaConfig.VerifyUrl, recaptchaConfig.SecretKey, submission.RecaptchaToken))
                {
                    return BadRequest(new SupportRequestSubmissionResultDto { Success = false, Message = "Your Captcha response is incorrect. Please refresh the page and try again." });
                }
            }
        }

        // Pull the actual Person entity for the helper so it can populate from the user when not anonymous.
        Person? callerEntity = null;
        if (User.Identity?.IsAuthenticated == true && CallingUser.PersonID > 0)
        {
            callerEntity = await DbContext.People
                .Include(p => p.Organization)
                .FirstOrDefaultAsync(p => p.PersonID == CallingUser.PersonID);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            await SupportRequestLogs.LogAndEmailAsync(
                DbContext,
                sitkaSmtpClientService,
                submission,
                callerEntity,
                ipAddress,
                userAgent,
                NeptuneConfiguration.DoNotReplyEmail);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send support request email");
            return Ok(new SupportRequestSubmissionResultDto
            {
                Success = false,
                Message = "Your support request was logged but we hit an error sending the email. Our team will follow up.",
            });
        }

        return Ok(new SupportRequestSubmissionResultDto { Success = true, Message = "Support request sent." });
    }
}
