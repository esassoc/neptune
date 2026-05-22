/*-----------------------------------------------------------------------
<copyright file="HelpController.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using System.Globalization;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Views.Shared;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Security;
using Neptune.WebMvc.Views.Help;
using System.Net.Mail;
using Neptune.Common;
using Neptune.Common.Email;
using Neptune.Common.Recaptcha;
using Neptune.WebMvc.Common.Mvc;

namespace Neptune.WebMvc.Controllers
{
    public class HelpController(
        NeptuneDbContext dbContext,
        ILogger<HelpController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        SitkaSmtpClientService sitkaSmtpClientService)
        : NeptuneBaseController<HelpController>(dbContext, logger, linkGenerator, webConfiguration)
    {
        [HttpGet]
        public ViewResult Support()
        {
            return ViewSupport(null, "");
        }
        
        private ViewResult ViewSupport(SupportRequestTypeEnum? supportRequestTypeEnum, string optionalPrepopulatedDescription)
        {
            var currentPageUrl = string.Empty;
            var referrer = HttpContext.Request.GetReferrer();
            if (referrer != null)
            {
                currentPageUrl = referrer;
            }
            else
            {
                var displayUrl = HttpContext.Request.GetDisplayUrl();
                if (displayUrl != null)
                {
                    currentPageUrl = displayUrl;
                }
            }

            var viewModel = new SupportFormViewModel(currentPageUrl, supportRequestTypeEnum);
            var isAnonymousUser = CurrentPerson.IsAnonymousUser();
            if (!isAnonymousUser)
            {
                viewModel.RequestPersonName = CurrentPerson.GetFullNameFirstLast();
                viewModel.RequestPersonEmail = CurrentPerson.Email;
                if (CurrentPerson.Organization != null)
                {
                    viewModel.RequestPersonOrganization = CurrentPerson.Organization.OrganizationName;
                }
                viewModel.RequestPersonPhone = CurrentPerson.Phone;
            }
            viewModel.RequestDescription = optionalPrepopulatedDescription;
            return ViewSupportImpl(viewModel);
        }

        private ViewResult ViewSupportImpl(SupportFormViewModel viewModel)
        {
            var allSupportRequestTypes = SupportRequestType.All.OrderBy(x => x.SupportRequestTypeSortOrder).ToList();

            var supportRequestTypes =
                allSupportRequestTypes.OrderBy(x => x.SupportRequestTypeSortOrder)
                    .ToSelectListWithEmptyFirstRow(x => x.SupportRequestTypeID.ToString(CultureInfo.InvariantCulture), x => x.SupportRequestTypeDisplayName);
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.RequestSupport);
            var referrer = HttpContext.Request.GetReferrer();
            var cancelUrl = string.IsNullOrWhiteSpace(referrer) ? SitkaRoute<HomeController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()) : referrer;
            viewModel.ReturnUrl = cancelUrl;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var viewData = new SupportFormViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, _webConfiguration.GoogleRecaptchaV3Config.SiteKey, supportRequestTypes, CurrentPerson.IsAnonymousUser(), cancelUrl);
            return RazorView<SupportForm, SupportFormViewData, SupportFormViewModel>(viewData, viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Support(SupportFormViewModel viewModel)
        {
            await ValidateRecaptcha(viewModel);

            if (!ModelState.IsValid)
            {
                return ViewSupportImpl(viewModel);
            }

            var supportRequestLog = SupportRequestLogs.Create(CurrentPerson);
            viewModel.UpdateModel(supportRequestLog, CurrentPerson);
            await _dbContext.SupportRequestLogs.AddAsync(supportRequestLog);
            await _dbContext.SaveChangesAsync();
            await SendMessage(supportRequestLog, viewModel.CurrentPageUrl, supportRequestLog.SupportRequestType, CurrentPerson);
            SetMessageForDisplay("Support request sent.");
            return Redirect(viewModel.ReturnUrl);
        }

        private async Task ValidateRecaptcha(RecaptchaFormViewModel viewModel)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                if (
                    !(await RecaptchaValidator.IsValidResponse(_webConfiguration.GoogleRecaptchaV3Config.VerifyUrl,
                        _webConfiguration.GoogleRecaptchaV3Config.SecretKey, viewModel.RecaptchaToken)))
                {
                    ModelState.AddModelError(null, "Your Captcha response is incorrect.");
                }
            }
        }

        [HttpGet]
        [LoggedInUnclassifiedFeature]
        public ViewResult RequestOrganizationNameChange()
        {
            var currentPageUrl = string.Empty;
            var referrer = HttpContext.Request.GetReferrer();
            if (referrer != null)
            {
                currentPageUrl = referrer;
            }
            else
            {
                var displayUrl = HttpContext.Request.GetDisplayUrl();
                if (displayUrl != null)
                {
                    currentPageUrl = displayUrl;
                }
            }

            var viewModel = new SupportFormViewModel(currentPageUrl, SupportRequestTypeEnum.RequestOrganizationNameChange);
            var isAnonymousUser = CurrentPerson.IsAnonymousUser();
            if (!isAnonymousUser)
            {
                viewModel.RequestPersonName = CurrentPerson.GetFullNameFirstLast();
                viewModel.RequestPersonEmail = CurrentPerson.Email;
                if (CurrentPerson.Organization != null)
                {
                    viewModel.RequestPersonOrganization = CurrentPerson.Organization.OrganizationName;
                }
                viewModel.RequestPersonPhone = CurrentPerson.Phone;
            }
            viewModel.RequestDescription = string.Empty;
            var allSupportRequestTypes = SupportRequestType.All.OrderBy(x => x.SupportRequestTypeSortOrder).ToList();

            var supportRequestTypes =
                allSupportRequestTypes.OrderBy(x => x.SupportRequestTypeSortOrder)
                    .ToSelectListWithEmptyFirstRow(x => x.SupportRequestTypeID.ToString(CultureInfo.InvariantCulture), x => x.SupportRequestTypeDisplayName);
            var neptunePage = NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.RequestSupport);
            var cancelUrl = string.IsNullOrWhiteSpace(referrer) ? SitkaRoute<HomeController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()) : referrer;
            viewModel.ReturnUrl = cancelUrl;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var viewData = new SupportFormViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, neptunePage, _webConfiguration.GoogleRecaptchaV3Config.SiteKey, supportRequestTypes, isAnonymousUser, cancelUrl);
            return RazorView<RequestOrganizationNameChange, SupportFormViewData, SupportFormViewModel>(viewData, viewModel);
        }

        [HttpPost]
        [LoggedInUnclassifiedFeature]
        public async Task<IActionResult> RequestOrganizationNameChange(SupportFormViewModel viewModel)
        {
            await ValidateRecaptcha(viewModel);
            if (!ModelState.IsValid)
            {
                return ViewSupportImpl(viewModel);
            }
            var supportRequestLog = SupportRequestLogs.Create(CurrentPerson);
            viewModel.UpdateModel(supportRequestLog, CurrentPerson);
            await _dbContext.SupportRequestLogs.AddAsync(supportRequestLog);
            await _dbContext.SaveChangesAsync();
            await SendMessage(supportRequestLog, viewModel.CurrentPageUrl, supportRequestLog.SupportRequestType, CurrentPerson);
            SetMessageForDisplay("Support request sent.");
            return Redirect(SitkaRoute<OrganizationController>.BuildUrlFromExpression(_linkGenerator, x => x.Index()));
        }

        [HttpGet]
        [LoggedInUnclassifiedFeature]
        public ViewResult RequestToChangePrivileges()
        {
            return ViewSupport(SupportRequestTypeEnum.RequestToChangeUserAccountPrivileges, string.Empty);
        }

        [HttpPost]
        public async Task<IActionResult> RequestToChangePrivileges(SupportFormViewModel viewModel)
        {
            return await Support(viewModel);
        }

        [HttpGet]
        [LoggedInUnclassifiedFeature]
        public ViewResult BulkUploadRequest()
        {
            return RazorView<BulkUploadRequest, BulkUploadRequestViewData>(
                new BulkUploadRequestViewData(HttpContext, _linkGenerator, _webConfiguration, CurrentPerson, NeptunePages.GetNeptunePageByPageType(_dbContext, NeptunePageType.BulkUploadRequest)));
        }

        private async Task SendMessage(SupportRequestLog supportRequestLog, string currentUrl, SupportRequestType supportRequestType, Person? requestPerson)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent;
            var subject = $"Support Request for Neptune - {DateTime.UtcNow.ToStringDateTime()}";
            var message = $@"
<div style='font-size: 12px; font-family: Arial'>
    <strong>{subject}</strong><br />
    <br />
    <strong>From:</strong> {supportRequestLog.RequestPersonName} - {supportRequestLog.RequestPersonOrganization ?? "(not provided)"}<br />
    <strong>Email:</strong> {supportRequestLog.RequestPersonEmail}<br />
    <strong>Phone:</strong> {supportRequestLog.RequestPersonPhone ?? "(not provided)"}<br />
    <br />
    <strong>Subject:</strong> {supportRequestType.SupportRequestTypeDisplayName}<br />
    <br />
    <strong>Description:</strong><br />
    {supportRequestLog.RequestDescription.HtmlEncodeWithBreaks()}
    <br />
    <br />
    <br />
    <div style='font-size: 10px; color: gray'>
    OTHER DETAILS:<br />
    LOGIN: {(requestPerson != null ? $"{requestPerson.GetFullNameFirstLast()} (UserID {requestPerson.PersonID})" : "(anonymous user)")}<br />
    IP ADDRESS: {ipAddress}<br />
    USERAGENT: {userAgent}<br />
    URL FROM: {currentUrl}<br />
    <br />
    </div>
    <div>You received this email because you are set up as a point of contact for support - if that's not correct, let us know: {"support@sitkatech.com"}</div>.
</div>
";
            // Create Notification
            var mailMessage = new MailMessage { From = new MailAddress(_webConfiguration.DoNotReplyEmail), Subject = subject, Body = message, IsBodyHtml = true };

            // Reply-To Header
            mailMessage.ReplyToList.Add(supportRequestLog.RequestPersonEmail);

            // TO field
            SetEmailRecipientsOfSupportRequest(_dbContext, mailMessage);

            await sitkaSmtpClientService.Send(mailMessage);
        }


        private static void SetEmailRecipientsOfSupportRequest(NeptuneDbContext dbContext, MailMessage mailMessage)
        {
            var supportPersonEmails = People.GetEmailAddressesForAdminsThatReceiveSupportEmails(dbContext).ToList();

            if (!supportPersonEmails.Any())
            {
                var defaultSupportPerson = People.GetByID(dbContext, 2);
                supportPersonEmails.Add(defaultSupportPerson.Email);
                mailMessage.Body = $"<p style=\"font-weight:bold\">Note: No users are currently configured to receive support emails. Defaulting to User: {defaultSupportPerson}</p>{mailMessage.Body}";
            }
            foreach (var supportPerson in supportPersonEmails)
            {
                mailMessage.To.Add(supportPerson);
            }
        }
    }
}
