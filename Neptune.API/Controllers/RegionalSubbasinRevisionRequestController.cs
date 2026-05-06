using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common.Email;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("regional-subbasin-revision-requests")]
    public class RegionalSubbasinRevisionRequestController(
        NeptuneDbContext dbContext,
        ILogger<RegionalSubbasinRevisionRequestController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        SitkaSmtpClientService sitkaSmtpClientService,
        GDALAPIService gdalApiService)
        : SitkaController<RegionalSubbasinRevisionRequestController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public ActionResult<List<RegionalSubbasinRevisionRequestDto>> List()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            var dtos = RegionalSubbasinRevisionRequests.ListAsDto(DbContext, currentPerson);
            return Ok(dtos);
        }

        [HttpGet("{regionalSubbasinRevisionRequestID}")]
        [RegionalSubbasinRevisionRequestViewFeature]
        [EntityNotFound(typeof(RegionalSubbasinRevisionRequest), "regionalSubbasinRevisionRequestID")]
        public ActionResult<RegionalSubbasinRevisionRequestDto> Get([FromRoute] int regionalSubbasinRevisionRequestID)
        {
            var dto = RegionalSubbasinRevisionRequests.GetByIDAsDto(DbContext, regionalSubbasinRevisionRequestID);
            if (dto == null)
            {
                return NotFound();
            }
            return Ok(dto);
        }

        [HttpPost("for-treatment-bmp/{treatmentBMPID}")]
        [TreatmentBMPEditFeature]
        [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
        public async Task<ActionResult<RegionalSubbasinRevisionRequestDto>> Create([FromRoute] int treatmentBMPID, [FromBody] RegionalSubbasinRevisionRequestUpsertDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.GeoJson))
            {
                return BadRequest("GeoJson is required.");
            }

            if (RegionalSubbasinRevisionRequests.HasOpenRequestForTreatmentBMP(DbContext, treatmentBMPID))
            {
                return BadRequest("You cannot open a new revision request for this BMP because there is already an open revision request.");
            }

            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            var entity = await RegionalSubbasinRevisionRequests.CreateAsync(DbContext, treatmentBMPID, dto.GeoJson, dto.Notes, currentPerson);

            await SendRSBRevisionRequestSubmittedEmail(currentPerson, entity);

            var resultDto = RegionalSubbasinRevisionRequests.GetByIDAsDto(DbContext, entity.RegionalSubbasinRevisionRequestID);
            return Ok(resultDto);
        }

        [HttpPost("{regionalSubbasinRevisionRequestID}/close")]
        [RegionalSubbasinRevisionRequestCloseFeature]
        [EntityNotFound(typeof(RegionalSubbasinRevisionRequest), "regionalSubbasinRevisionRequestID")]
        public async Task<ActionResult<RegionalSubbasinRevisionRequestDto>> Close([FromRoute] int regionalSubbasinRevisionRequestID, [FromBody] RegionalSubbasinRevisionRequestCloseDto dto)
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            var entity = await RegionalSubbasinRevisionRequests.CloseAsync(DbContext, regionalSubbasinRevisionRequestID, dto.CloseNotes, currentPerson);

            await SendRSBRevisionRequestClosedEmail(currentPerson, entity);

            var resultDto = RegionalSubbasinRevisionRequests.GetByIDAsDto(DbContext, regionalSubbasinRevisionRequestID);
            return Ok(resultDto);
        }

        [HttpGet("{regionalSubbasinRevisionRequestID}/gdb")]
        [RegionalSubbasinRevisionRequestDownloadFeature]
        [EntityNotFound(typeof(RegionalSubbasinRevisionRequest), "regionalSubbasinRevisionRequestID")]
        public async Task<FileResult> Download([FromRoute] int regionalSubbasinRevisionRequestID)
        {
            var entity = RegionalSubbasinRevisionRequests.GetByID(DbContext, regionalSubbasinRevisionRequestID);
            var bytes = await RegionalSubbasinRevisionRequests.GetGdbExportAsync(DbContext, gdalApiService, regionalSubbasinRevisionRequestID);
            var fileName = $"BMP_{entity.TreatmentBMPID}_RevisionRequest.zip";
            return File(bytes, "application/zip", fileName);
        }

        private async Task SendRSBRevisionRequestSubmittedEmail(Person requestPerson, RegionalSubbasinRevisionRequest request)
        {
            var treatmentBMP = TreatmentBMPs.GetByID(DbContext, request.TreatmentBMPID);
            var subject = "A new Regional Subbasin Revision Request was submitted";
            var detailUrl = $"{NeptuneConfiguration.OcStormwaterToolsBaseUrl}/delineation/revision-requests/{request.RegionalSubbasinRevisionRequestID}";
            var organizationName = requestPerson.Organization?.OrganizationName ?? "";

            var body = $@"
<div style='font-size: 12px; font-family: Arial'>
<strong>{subject}</strong><br />
<br />
A new Regional Subbasin Revision Request was just submitted by {requestPerson.FirstName} {requestPerson.LastName} ({organizationName}) for BMP {treatmentBMP.TreatmentBMPName}.
Please review it, make revisions, and close it at your earliest convenience. <a href='{detailUrl}'>View this Request</a>

<div>You received this email because you are assigned to receive Regional Subbasin Revision Request notifications within the Orange County Stormwater Tools.</div>
</div>";

            var mailMessage = new MailMessage
            {
                From = sitkaSmtpClientService.GetDefaultEmailFrom(),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.CC.Add(requestPerson.Email);
            foreach (var recipient in GetPeopleWhoReceiveRSBRevisionRequests())
            {
                mailMessage.To.Add(recipient.Email);
            }

            if (mailMessage.To.Count > 0)
            {
                await sitkaSmtpClientService.Send(mailMessage);
            }
        }

        private async Task SendRSBRevisionRequestClosedEmail(Person closingPerson, RegionalSubbasinRevisionRequest request)
        {
            var treatmentBMP = TreatmentBMPs.GetByID(DbContext, request.TreatmentBMPID);
            var requester = People.GetByID(DbContext, request.RequestPersonID);
            var subject = "A Regional Subbasin Revision Request was closed";
            var delineationMapUrl = $"{NeptuneConfiguration.OcStormwaterToolsBaseUrl}/delineation/delineation-map?treatmentBMPID={treatmentBMP.TreatmentBMPID}";
            var organizationName = closingPerson.Organization?.OrganizationName ?? "";
            var bmpName = treatmentBMP.TreatmentBMPName.Trim();

            var body = $@"
<div style='font-size: 12px; font-family: Arial'>
<strong>{subject}</strong><br />
<br />
The Regional Subbasin Revision Request for BMP {bmpName} was just closed by {closingPerson.FirstName} {closingPerson.LastName} ({organizationName}).
<br /><br />
The changes resulting from this update will be available for your use next Monday. At that time you will be able to revise the delineation for {bmpName}.
<br /><br />
<a href='{delineationMapUrl}'>Revise the delineation for BMP {bmpName} on the Delineation map</a>.
<br /><br />
<div>Thanks for keeping the Regional Subbasin Network up to date within the Orange County Stormwater Tools.</div>
</div>";

            var mailMessage = new MailMessage
            {
                From = sitkaSmtpClientService.GetDefaultEmailFrom(),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(requester.Email);
            foreach (var recipient in GetPeopleWhoReceiveRSBRevisionRequests())
            {
                mailMessage.CC.Add(recipient.Email);
            }

            await sitkaSmtpClientService.Send(mailMessage);
        }

        private List<Person> GetPeopleWhoReceiveRSBRevisionRequests()
        {
            return DbContext.People.AsNoTracking()
                .Where(x => x.ReceiveRSBRevisionRequestEmails && x.IsActive)
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .ToList();
        }
    }
}
