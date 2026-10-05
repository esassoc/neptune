using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common.Email;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.Person;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController(
        NeptuneDbContext dbContext,
        ILogger<UserController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        SitkaSmtpClientService sitkaSmtpClientService)
        : SitkaController<UserController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpPost]
        [LoggedInUnclassifiedFeature]
        public async Task<ActionResult<PersonDto>> Create([FromBody] PersonCreateDto personCreateDto)
        {
            // Validate request body; all fields required in Dto except Org Name and Phone
            if (personCreateDto == null)
            {
                return BadRequest();
            }

            var validationMessages = People.ValidateCreateUnassignedPerson(DbContext, personCreateDto);
            validationMessages.ForEach(vm => { ModelState.AddModelError(vm.Type, vm.Message); });

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = People.CreateUnassignedPerson(DbContext, personCreateDto);

            var mailMessage = GenerateUserCreatedEmail(user);
            SitkaSmtpClientService.AddCcRecipientsToEmail(mailMessage,
                        People.GetEmailAddressesForAdminsThatReceiveSupportEmails(DbContext));
            await SendEmailMessage(mailMessage);

            return Ok(user);
        }

        [HttpGet]
        [UserViewFeature]
        public async Task<ActionResult<List<PersonSimpleDto>>> List()
        {
            var people = await People.ListAsSimpleDtoAsync(DbContext);
            return Ok(people);
        }

        [HttpGet("{personID}")]
        [UserViewDetailFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDto>> Get([FromRoute] int personID)
        {
            var person = await People.GetByIDAsDtoAsync(DbContext, personID);
            if (person == null) return NotFound();
            return Ok(person);
        }

        //[HttpPost]
        //[AdminFeature]
        //public async Task<ActionResult<PersonDto>> Create([FromBody] PersonUpsertDto dto)
        //{
        //    var created = await People.CreateAsync(DbContext, dto, dto.Email, Guid.NewGuid());
        //    return CreatedAtAction(nameof(Get), new { personID = created.PersonID }, created);
        //}

        [HttpPut("{personID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDto>> Update([FromRoute] int personID, [FromBody] PersonUpsertDto dto)
        {
            var updated = await People.UpdateAsync(DbContext, personID, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{personID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<IActionResult> Delete([FromRoute] int personID)
        {
            var deleted = await People.DeleteAsync(DbContext, personID);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("{personID}/detail")]
        [UserViewFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDetailDto>> GetDetail([FromRoute] int personID)
        {
            // Caller must be the user themselves OR an Administrator. UserViewFeature already gates
            // anonymous/unassigned users; this just narrows further so an authenticated non-admin
            // cannot view someone else's profile.
            if (CallingUser.PersonID != personID && CallingUser.RoleID != (int)RoleEnum.Admin && CallingUser.RoleID != (int)RoleEnum.SitkaAdmin)
            {
                return Forbid();
            }

            var dto = await People.GetByIDAsDetailDtoAsync(DbContext, personID);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost("{personID}/generate-web-service-token")]
        [UserViewFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<Guid>> GenerateWebServiceToken([FromRoute] int personID)
        {
            // Users may rotate their own token; Admins may rotate anyone's. Same gating shape as
            // GetDetail above — UserViewFeature handles the anonymous/unassigned cutoff.
            if (CallingUser.PersonID != personID && CallingUser.RoleID != (int)RoleEnum.Admin && CallingUser.RoleID != (int)RoleEnum.SitkaAdmin)
            {
                return Forbid();
            }

            var newToken = await People.GenerateAndPersistWebServiceAccessTokenAsync(DbContext, personID);
            return Ok(newToken);
        }

        [HttpPut("{personID}/role")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDto>> UpdateRole([FromRoute] int personID, [FromBody] PersonRoleUpdateDto dto)
        {
            // Admins (non-Sitka) cannot promote anyone above JurisdictionManager. SitkaAdmins may assign any role.
            if (CallingUser.RoleID != (int)RoleEnum.SitkaAdmin)
            {
                var allowedRoleIDs = new[]
                {
                    (int)RoleEnum.Admin,
                    (int)RoleEnum.JurisdictionManager,
                    (int)RoleEnum.JurisdictionEditor,
                    (int)RoleEnum.Unassigned,
                };
                if (!allowedRoleIDs.Contains(dto.RoleID))
                {
                    return BadRequest("You are not allowed to assign the SitkaAdmin role.");
                }
            }

            var updated = await People.UpdateRoleAsync(DbContext, personID, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // NPT-734: Admins invite anyone; Jurisdiction Managers invite Editors/Managers into their own
        // jurisdiction (the role and jurisdiction scoping lives in PersonInvites.ValidateInviteAsync).
        // No Auth0 call: the invitee signs up with this email and People.UpdateClaims links them on first login.
        [HttpPost("invite")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<PersonInviteResultDto>> Invite([FromBody] PersonInviteDto personInviteDto)
        {
            // Implicit [Required] for non-nullable references is suppressed in Startup, so a JSON null body gets here.
            if (personInviteDto == null)
            {
                return BadRequest();
            }

            var inviter = CallingUser;
            var validationMessages = await PersonInvites.ValidateInviteAsync(DbContext, inviter, personInviteDto);
            validationMessages.ForEach(vm => { ModelState.AddModelError(vm.Type, vm.Message); });
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var inviteResult = await PersonInvites.InviteAsync(DbContext, personInviteDto);
            if (inviteResult.DuplicateEmailMessage != null)
            {
                // Lost a race with another invite for the same address after validation passed.
                ModelState.AddModelError(PersonInvites.EmailKey, inviteResult.DuplicateEmailMessage);
                return BadRequest(ModelState);
            }
            var invitedPerson = inviteResult.Person!;

            // The Person row is the invite, so a mail failure doesn't fail the request: the invitee can still sign
            // up from the home page with the same address. The result tells the UI so it doesn't claim it was sent.
            var invitationEmailSent = false;
            try
            {
                var jurisdictionNames = await PersonInvites.ListJurisdictionNamesAsync(DbContext, personInviteDto.StormwaterJurisdictionIDs ?? []);
                var mailMessage = GenerateInviteEmail(invitedPerson, inviter, jurisdictionNames);
                invitationEmailSent = await SendEmailMessage(mailMessage);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "NPT-734: invite email to PersonID {PersonID} failed to send.", invitedPerson.PersonID);
            }

            return Ok(new PersonInviteResultDto { Person = invitedPerson, InvitationEmailSent = invitationEmailSent });
        }

        [HttpPut("{personID}/jurisdictions")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDetailDto>> UpdateJurisdictions([FromRoute] int personID, [FromBody] PersonJurisdictionsUpdateDto dto)
        {
            var updated = await People.UpdateJurisdictionsAsync(DbContext, personID, dto.StormwaterJurisdictionIDs);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPut("{personID}/active-status")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<PersonDetailDto>> UpdateActiveStatus([FromRoute] int personID, [FromBody] PersonActiveStatusUpdateDto dto)
        {
            // Block inactivation when the user is a primary contact for one or more organizations;
            // surface the org names so the caller can reassign before retrying.
            if (!dto.IsActive)
            {
                var primaryContactOrganizations = Organizations.ListByPrimaryContactPersonID(DbContext, personID);
                if (primaryContactOrganizations.Count > 0)
                {
                    var names = string.Join(", ", primaryContactOrganizations.Select(x => x.OrganizationName));
                    return BadRequest($"This user is the primary contact for: {names}. Reassign the primary contact for those organizations before deactivating.");
                }
            }

            var updated = await People.UpdateActiveStatusAsync(DbContext, personID, dto.IsActive);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpGet("{personID}/notifications")]
        [UserViewFeature]
        [EntityNotFoundAttribute(typeof(Person), "personID")]
        public async Task<ActionResult<List<PersonNotificationDto>>> GetNotifications([FromRoute] int personID)
        {
            if (CallingUser.PersonID != personID && CallingUser.RoleID != (int)RoleEnum.Admin && CallingUser.RoleID != (int)RoleEnum.SitkaAdmin)
            {
                return Forbid();
            }
            var notifications = await People.ListNotificationsByPersonIDAsync(DbContext, personID);
            return Ok(notifications);
        }

        private MailMessage GenerateUserCreatedEmail(PersonDto person)
        {
            var messageBody = $@"
<div style='font-size: 12px; font-family: Arial'>
    <strong>OC Stormwater Tools User added:</strong> {person.FirstName} {person.LastName}<br />
    <strong>Added on:</strong> {DateTime.UtcNow}<br />
    <strong>Email:</strong> {person.Email}<br />
    <strong>Phone:</strong> {person.Phone}<br />
    <br />
    <p>
        You may want to <a href=""{NeptuneConfiguration.OcStormwaterToolsBaseUrl}/users/{person.PersonID}"">assign this user a role</a> and associate them with a jurisdiction to allow them to use the site. Or you can leave the user with Unassigned roles if they don't need special privileges.
    </p>
    </div>
    {sitkaSmtpClientService.GetSupportNotificationEmailSignature()}
</div>
";

            var mailMessage = new MailMessage
            {
                Subject = $"New User in OC Stormwater Tools",
                Body = $"Hello,<br /><br />{messageBody}",
            };

            mailMessage.To.Add(sitkaSmtpClientService.GetDefaultEmailFrom());
            return mailMessage;
        }

        private MailMessage GenerateInviteEmail(PersonDto invitedPerson, PersonDto inviter, List<string> jurisdictionNames)
        {
            var encode = (Func<string, string>)WebUtility.HtmlEncode;
            var baseUrl = NeptuneConfiguration.OcStormwaterToolsBaseUrl;
            var inviterName = encode($"{inviter.FirstName} {inviter.LastName}".Trim());
            var roleName = Role.AllLookupDictionary.TryGetValue(invitedPerson.RoleID, out var role) ? role.RoleDisplayName : invitedPerson.RoleName;
            // "an Administrator", "a Jurisdiction Editor"; Unassigned isn't a role worth announcing.
            var rolePhrase = invitedPerson.RoleID == (int)RoleEnum.Unassigned
                ? ""
                : $" as {("AEIOU".Contains(char.ToUpperInvariant(roleName.FirstOrDefault())) ? "an" : "a")} <strong>{encode(roleName)}</strong>";
            var jurisdictionPhrase = jurisdictionNames.Any() ? $" for {encode(string.Join(", ", jurisdictionNames))}" : "";
            var accessLine = rolePhrase + jurisdictionPhrase;
            var email = encode(invitedPerson.Email);

            // County staff are routed to their Microsoft Entra login by email domain (docs/ocpw-county-sso.md),
            // so they have no Neptune password to create and no Auth0 verification email to wait for.
            var isCountyStaff = invitedPerson.Email.EndsWith("@pw.oc.gov", StringComparison.OrdinalIgnoreCase);
            var steps = isCountyStaff
                ? $@"
    <p>Your address is an Orange County Public Works account, so there is no separate password to create.
    <a href=""{baseUrl}"">Open OC Stormwater Tools</a>, choose <strong>Sign In</strong>, and enter <strong>{email}</strong>.
    You will be sent to your usual County sign-in.</p>"
                : $@"
    <ol>
        <li><a href=""{baseUrl}/sign-up"">Create your account</a> using <strong>{email}</strong>. Use this exact address; it is how your account connects to the access set up for you.</li>
        <li>Check your inbox for a verification email and click the link in it. Until you do, signing in will ask you to verify.</li>
        <li><a href=""{baseUrl}"">Sign in to OC Stormwater Tools</a>.</li>
    </ol>
    <p>Already have an OC Stormwater Tools account under a different address? Reply to this email so your access can be moved to it.</p>";

            var messageBody = $@"
<div style='font-size: 14px; font-family: Arial'>
    <p>{inviterName} has invited you to OC Stormwater Tools{accessLine}.</p>
    {steps}
    {sitkaSmtpClientService.GetSupportNotificationEmailSignature()}
</div>
";

            var mailMessage = new MailMessage
            {
                Subject = "Invitation to OC Stormwater Tools",
                Body = $"Hello {encode(invitedPerson.FirstName)},<br /><br />{messageBody}",
            };
            mailMessage.To.Add(new MailAddress(invitedPerson.Email, $"{invitedPerson.FirstName} {invitedPerson.LastName}".Trim()));
            if (!string.IsNullOrWhiteSpace(inviter.Email) && MailAddress.TryCreate(inviter.Email, out var inviterAddress))
            {
                mailMessage.ReplyToList.Add(inviterAddress);
            }
            return mailMessage;
        }

        private async Task<bool> SendEmailMessage(MailMessage mailMessage)
        {
            mailMessage.IsBodyHtml = true;
            mailMessage.From = sitkaSmtpClientService.GetDefaultEmailFrom();
            if (!mailMessage.ReplyToList.Any())
            {
                mailMessage.ReplyToList.Add(NeptuneConfiguration.DoNotReplyEmail);
            }
            return await sitkaSmtpClientService.Send(mailMessage);
        }
    }
}
