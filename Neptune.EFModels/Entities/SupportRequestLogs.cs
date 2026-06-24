/*-----------------------------------------------------------------------
<copyright file="SupportRequestLog.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
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

using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.Email;
using Neptune.Models.DataTransferObjects;
using System.Net.Mail;
using System.Web;

namespace Neptune.EFModels.Entities
{
    public static class SupportRequestLogs
    {
        public static SupportRequestLog Create(Person person)
        {
            var supportRequest = new SupportRequestLog()
            {
                SupportRequestTypeID = SupportRequestType.Other.SupportRequestTypeID
            };
            if (person != null && !person.IsAnonymousUser())
            {
                supportRequest.RequestPersonID = person.PersonID;
                supportRequest.RequestPersonName = person.GetFullNameFirstLast();
                supportRequest.RequestPersonEmail = person.Email;
                if (person.Organization != null)
                {
                    supportRequest.RequestPersonOrganization = person.Organization.OrganizationName;
                }
                supportRequest.RequestPersonPhone = person.Phone;
            }
            return supportRequest;
        }

        public static async Task LogAndEmailAsync(
            NeptuneDbContext dbContext,
            SitkaSmtpClientService sitkaSmtpClientService,
            SupportRequestSubmissionDto submission,
            Person? caller,
            string? ipAddress,
            string? userAgent,
            string fromEmailAddress)
        {
            // Build the SupportRequestLog row, preferring the authenticated caller's identity when present.
            var supportRequestLog = Create(caller);
            supportRequestLog.SupportRequestTypeID = submission.SupportRequestTypeID;
            supportRequestLog.RequestDescription = submission.Description;
            supportRequestLog.RequestDate = DateTime.UtcNow;
            // For anonymous submissions the form-supplied identity is authoritative; for authenticated
            // ones, Create() already populated from the Person entity.
            if (caller == null || caller.IsAnonymousUser())
            {
                supportRequestLog.RequestPersonName = submission.Name;
                supportRequestLog.RequestPersonEmail = submission.Email;
                supportRequestLog.RequestPersonOrganization = submission.Organization;
                supportRequestLog.RequestPersonPhone = submission.Phone;
            }

            await dbContext.SupportRequestLogs.AddAsync(supportRequestLog);
            await dbContext.SaveChangesAsync();

            var supportRequestType = SupportRequestType.AllLookupDictionary.TryGetValue(submission.SupportRequestTypeID, out var t)
                ? t
                : SupportRequestType.Other;

            var subject = $"Support Request for Neptune - {DateTime.UtcNow.ToStringDateTime()}";
            // Every interpolated value below originates from untrusted input (form fields, request headers,
            // submitter identity), so each goes through HtmlEncode before landing in the rendered email body.
            var fromName = HttpUtility.HtmlEncode(supportRequestLog.RequestPersonName ?? string.Empty);
            var fromOrg = HttpUtility.HtmlEncode(supportRequestLog.RequestPersonOrganization ?? "(not provided)");
            var fromEmail = HttpUtility.HtmlEncode(supportRequestLog.RequestPersonEmail ?? string.Empty);
            var fromPhone = HttpUtility.HtmlEncode(supportRequestLog.RequestPersonPhone ?? "(not provided)");
            var typeDisplayName = HttpUtility.HtmlEncode(supportRequestType.SupportRequestTypeDisplayName);
            var loginLine = caller != null && !caller.IsAnonymousUser()
                ? HttpUtility.HtmlEncode($"{caller.GetFullNameFirstLast()} (UserID {caller.PersonID})")
                : "(anonymous user)";
            var encodedIpAddress = HttpUtility.HtmlEncode(ipAddress ?? string.Empty);
            var encodedUserAgent = HttpUtility.HtmlEncode(userAgent ?? string.Empty);
            var encodedCurrentPageUrl = HttpUtility.HtmlEncode(submission.CurrentPageUrl ?? string.Empty);
            var body = $@"
<div style='font-size: 12px; font-family: Arial'>
    <strong>{subject}</strong><br />
    <br />
    <strong>From:</strong> {fromName} - {fromOrg}<br />
    <strong>Email:</strong> {fromEmail}<br />
    <strong>Phone:</strong> {fromPhone}<br />
    <br />
    <strong>Subject:</strong> {typeDisplayName}<br />
    <br />
    <strong>Description:</strong><br />
    {supportRequestLog.RequestDescription.HtmlEncodeWithBreaks()}
    <br />
    <br />
    <br />
    <div style='font-size: 10px; color: gray'>
    OTHER DETAILS:<br />
    LOGIN: {loginLine}<br />
    IP ADDRESS: {encodedIpAddress}<br />
    USERAGENT: {encodedUserAgent}<br />
    URL FROM: {encodedCurrentPageUrl}<br />
    <br />
    </div>
    <div>You received this email because you are set up as a point of contact for support - if that's not correct, let us know: support@sitkatech.com</div>
</div>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmailAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            if (!string.IsNullOrWhiteSpace(supportRequestLog.RequestPersonEmail))
            {
                mailMessage.ReplyToList.Add(supportRequestLog.RequestPersonEmail);
            }

            // Recipients: anyone with ReceiveSupportEmails = true. Fall back to PersonID 2 when the list is empty
            // (mirroring the legacy behavior in HelpController.SetEmailRecipientsOfSupportRequest).
            var supportEmails = People.GetEmailAddressesForAdminsThatReceiveSupportEmails(dbContext).ToList();
            if (supportEmails.Count == 0)
            {
                var defaultSupportPerson = await dbContext.People.AsNoTracking().FirstOrDefaultAsync(x => x.PersonID == 2);
                if (defaultSupportPerson != null)
                {
                    mailMessage.Body = $"<p style=\"font-weight:bold\">Note: No users are currently configured to receive support emails. Defaulting to User: {defaultSupportPerson.Email}</p>{mailMessage.Body}";
                    supportEmails.Add(defaultSupportPerson.Email);
                }
            }
            foreach (var email in supportEmails)
            {
                mailMessage.To.Add(email);
            }

            if (mailMessage.To.Count > 0)
            {
                await sitkaSmtpClientService.Send(mailMessage);
            }
        }
    }
}
