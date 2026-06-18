using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Neptune.Common.Email
{
    public class SitkaSmtpClientService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly SendGridConfiguration _configuration;

        public SitkaSmtpClientService(ISendGridClient sendGridClient, IOptions<SendGridConfiguration> configuration)
        {
            _sendGridClient = sendGridClient ?? throw new ArgumentNullException(nameof(sendGridClient));
            _configuration = configuration.Value;
        }

        /// <summary>
        /// Sends an email including mock mode and address redirection  <see cref="ISendGridConfiguration.SitkaEmailRedirect"/>, then calls onward to <see cref="SendDirectly"/>
        /// </summary>
        /// <param name="message"></param>
        public async Task Send(MailMessage message)
        {
            var messageWithAnyAlterations = AlterMessageIfInRedirectMode(message);
            var messageAfterAlterationsAndCreatingAlternateViews = CreateAlternateViewsIfNeeded(messageWithAnyAlterations);
            await SendDirectly(messageAfterAlterationsAndCreatingAlternateViews);
        }

        private static MailMessage CreateAlternateViewsIfNeeded(MailMessage message)
        {
            if (!message.IsBodyHtml)
            {
                return message;
            }
            // Define the plain text alternate view and add to message
            const string plainTextBody = "You must use an email client that supports HTML messages";

            var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, MediaTypeNames.Text.Plain);

            message.AlternateViews.Add(plainTextView);

            // Define the html alternate view with embedded image and
            // add to message. To reference images attached as linked
            // resources from your HTML message body, use "cid:contentID"
            // in the <img> tag...
            var htmlBody = message.Body;

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
            message.AlternateViews.Add(htmlView);


            return message;
        }


        /// <summary>
        /// Sends an email message at a lower level than <see cref="Send"/>, skipping mock mode and address redirection  <see cref="ISendGridConfiguration.SitkaEmailRedirect"/>
        /// </summary>
        /// <param name="mailMessage"></param>
        public async Task SendDirectly(MailMessage mailMessage)
        {
            var defaultEmailFrom = GetDefaultEmailFrom();

            var sendGridMessage = new SendGridMessage()
            {
                From = new EmailAddress(defaultEmailFrom.Address, defaultEmailFrom.DisplayName),
                Subject = mailMessage.Subject,
                PlainTextContent = mailMessage.IsBodyHtml ? null : mailMessage.Body,
                HtmlContent = mailMessage.IsBodyHtml ? mailMessage.Body : null
            };

            if (mailMessage.To != null && mailMessage.To.Any())
            {
                sendGridMessage.AddTos(mailMessage.To.Select(x => new EmailAddress(x.Address, x.DisplayName)).ToList());
            }

            if (mailMessage.CC != null && mailMessage.CC.Any())
            {
                sendGridMessage.AddCcs(mailMessage.CC.Select(x => new EmailAddress(x.Address, x.DisplayName)).ToList());
            }

            if (mailMessage.Bcc != null && mailMessage.Bcc.Any())
            {
                sendGridMessage.AddBccs(mailMessage.Bcc.Select(x => new EmailAddress(x.Address, x.DisplayName)).ToList());
            }

            var response = await _sendGridClient.SendEmailAsync(sendGridMessage).ConfigureAwait(false);
            // Optionally, handle non-success status or logging
        }

        /// <summary>
        /// Alter message TO, CC, BCC if the setting <see cref="_sitkaEmailRedirect"/> is set
        /// Appends the real to the body
        /// </summary>
        /// <param name="realMailMessage"></param>
        /// <returns></returns>
        private MailMessage AlterMessageIfInRedirectMode(MailMessage realMailMessage)
        {
            var isInRedirectMode = !string.IsNullOrWhiteSpace(_configuration.SitkaEmailRedirect);

            if (!isInRedirectMode)
            {
                return realMailMessage;
            }

            ClearOriginalAddressesAndAppendToBody(realMailMessage, "To", realMailMessage.To);
            ClearOriginalAddressesAndAppendToBody(realMailMessage, "CC", realMailMessage.CC);
            ClearOriginalAddressesAndAppendToBody(realMailMessage, "BCC", realMailMessage.Bcc);

            realMailMessage.To.Add(_configuration.SitkaEmailRedirect);

            return realMailMessage;
        }

        private static void ClearOriginalAddressesAndAppendToBody(MailMessage realMailMessage, string addressType, ICollection<MailAddress> addresses)
        {
            var newline = realMailMessage.IsBodyHtml ? "<br />" : Environment.NewLine;
            var separator = newline + "\t";

            var toExpected = addresses.Aggregate(String.Empty, (s, mailAddress) => s + Environment.NewLine + "\t" + mailAddress.ToString());
            if (!string.IsNullOrWhiteSpace(toExpected))
            {
                var toAppend =
                    $"{newline}{separator}Actual {addressType}:{(realMailMessage.IsBodyHtml ? toExpected.HtmlEncodeWithBreaks() : toExpected)}";
                realMailMessage.Body += toAppend;

                for (var i = 0; i < realMailMessage.AlternateViews.Count; i++)
                {
                    var stream = realMailMessage.AlternateViews[i].ContentStream;
                    using var reader = new StreamReader(stream);
                    var alternateBody = reader.ReadToEnd();
                    alternateBody += toAppend;
                    var newAlternateView = AlternateView.CreateAlternateViewFromString(alternateBody, null, realMailMessage.AlternateViews[i].ContentType.MediaType);
                    realMailMessage.AlternateViews[i].LinkedResources.ToList().ForEach(x => newAlternateView.LinkedResources.Add(x));
                    realMailMessage.AlternateViews[i] = newAlternateView;
                }


            }
            addresses.Clear();
        }

        public string GetSupportNotificationEmailSignature()
        {
            string supportNotificationEmailSignature = $@"<br /><br />
Respectfully, the OC Stormwater Tools team
<br /><br />
***
<br /><br />
You have received this email because you are assigned to receive support notifications within the OC Stormwater Tools. 
<br /><br />
<a href=""mailto:{_configuration.SitkaSupportEmail}"">{_configuration.SitkaSupportEmail}</a>";
            return supportNotificationEmailSignature;
        }

        public MailAddress GetDefaultEmailFrom()
        {
            return new MailAddress("donotreply@sitkatech.net", "OC Stormwater Tools");
        }

        public static void AddCcRecipientsToEmail(MailMessage mailMessage, IEnumerable<string> recipients)
        {
            foreach (var recipient in recipients)
            {
                mailMessage.CC.Add(recipient);
            }
        }
    }
}