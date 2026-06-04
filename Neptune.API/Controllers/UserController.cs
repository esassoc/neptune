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
        You may want to <a href=""{NeptuneConfiguration.OcStormwaterToolsBaseUrl}/Detail/{person.PersonID}"">assign this user a role</a> and associate them with a jurisdiction to allow them to use the site. Or you can leave the user with Unassigned roles if they don't need special privileges.
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

        private async Task SendEmailMessage(MailMessage mailMessage)
        {
            mailMessage.IsBodyHtml = true;
            mailMessage.From = sitkaSmtpClientService.GetDefaultEmailFrom();
            mailMessage.ReplyToList.Add(NeptuneConfiguration.DoNotReplyEmail);
            await sitkaSmtpClientService.Send(mailMessage);
        }
    }
}
