using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;
using Neptune.Jobs.Hangfire;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("delineations")]
    public class DelineationController(
        NeptuneDbContext dbContext,
        ILogger<DelineationController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<DelineationController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public ActionResult<List<DelineationDto>> List()
        {
            var delineationsUpsertDtos = Delineations.ListByPersonIDAsDto(DbContext, CallingUser.PersonID);
            return Ok(delineationsUpsertDtos);
        }

        [HttpGet("for-treatment-bmp/{treatmentBMPID}")]
        [TreatmentBMPViewFeature]
        [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
        public ActionResult<DelineationDto?> GetForTreatmentBMP([FromRoute] int treatmentBMPID)
        {
            var dto = Delineations.GetByTreatmentBMPIDAsDto(DbContext, treatmentBMPID);
            return Ok(dto);
        }

        [HttpPut("for-treatment-bmp/{treatmentBMPID}")]
        [TreatmentBMPEditFeature]
        [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
        public async Task<ActionResult<DelineationDto?>> UpsertForTreatmentBMP([FromRoute] int treatmentBMPID, [FromBody] DelineationUpsertGeoJsonDto dto)
        {
            var treatmentBMP = TreatmentBMPs.GetByIDWithChangeTracking(DbContext, treatmentBMPID);
            var existing = Delineations.GetByTreatmentBMPIDWithChangeTracking(DbContext, treatmentBMPID);

            Geometry? geom4326 = null;
            Geometry? geom2771 = null;
            if (!string.IsNullOrWhiteSpace(dto.GeoJson))
            {
                var feature = GeoJsonSerializer.Deserialize<IFeature>(dto.GeoJson);
                if (feature?.Geometry == null)
                {
                    return BadRequest("GeoJson did not contain a valid geometry.");
                }
                geom4326 = feature.Geometry;
                geom4326.SRID = Proj4NetHelper.WEB_MERCATOR;
                geom2771 = geom4326.ProjectTo2771();
            }

            var oldShape = existing?.DelineationGeometry;
            var newShape = geom2771;

            if (existing != null)
            {
                if (geom4326 != null)
                {
                    existing.DelineationGeometry = geom2771;
                    existing.DelineationGeometry4326 = geom4326;
                    existing.DelineationTypeID = dto.DelineationTypeID;
                    existing.IsVerified = false;
                    existing.DateLastModified = DateTime.UtcNow;
                }
                else
                {
                    await existing.DeleteFull(DbContext);
                    existing = null;
                }
            }
            else if (geom4326 != null)
            {
                existing = new Delineation
                {
                    TreatmentBMPID = treatmentBMPID,
                    DelineationGeometry = geom2771,
                    DelineationGeometry4326 = geom4326,
                    DelineationTypeID = dto.DelineationTypeID,
                    DateLastModified = DateTime.UtcNow,
                    IsVerified = false,
                    HasDiscrepancies = false,
                };
                await DbContext.Delineations.AddAsync(existing);
            }

            await DbContext.SaveChangesAsync();

            if (existing != null)
            {
                // NPT-1115: repair the just-saved geometry if the freehand draw produced a SQL-invalid
                // polygon (self-intersection). Uses SQL Server MakeValid — NTS validity differs from
                // STIsValid, and both the GeoServer WMS layer and the NTS overlay key off STIsValid.
                await DbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pDelineationMakeValid @DelineationID = {0}", existing.DelineationID);
                // Reload so the LGU refresh below unions the actually-stored (now valid) geometry
                // rather than the pre-repair in-memory shape: the original could be NTS-invalid and
                // throw in QueueLGURefreshForArea's Union, and would compute the wrong refresh area.
                await DbContext.Entry(existing).ReloadAsync();
                newShape = existing.DelineationGeometry;
            }

            var treatmentBMPType = TreatmentBMPTypes.GetByIDWithChangeTracking(DbContext, treatmentBMP.TreatmentBMPTypeID);
            if (!(newShape == null && oldShape == null) && treatmentBMPType.TreatmentBMPModelingType != null)
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldShape, newShape, DbContext);
            }

            var resultDto = existing != null ? Delineations.GetByTreatmentBMPIDAsDto(DbContext, treatmentBMPID) : null;
            return Ok(resultDto);
        }

        [HttpPost("{delineationID}/set-verified")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(Delineation), "delineationID")]
        public async Task<ActionResult> SetVerified([FromRoute] int delineationID, [FromBody] DelineationSetVerifiedDto dto)
        {
            var delineation = Delineations.GetByIDWithChangeTracking(DbContext, delineationID);
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);

            if (!currentPerson.IsAssignedToStormwaterJurisdiction(delineation.TreatmentBMP.StormwaterJurisdictionID))
            {
                return Forbid();
            }

            var wasVerified = delineation.IsVerified;
            delineation.IsVerified = dto.IsVerified;
            delineation.DateLastVerified = DateTime.UtcNow;
            delineation.VerifiedByPersonID = currentPerson.PersonID;
            await DbContext.SaveChangesAsync();

            var isExistingCentralized = delineation.DelineationTypeID == (int)DelineationTypeEnum.Centralized
                                        && delineation.TreatmentBMP.ProjectID == null;
            if (isExistingCentralized && wasVerified != delineation.IsVerified)
            {
                // NPT-1155: a provisional centralized delineation is not modeled (see
                // vNereidRegionalSubbasinCentralizedBMP), so this change puts its BMP into the model or
                // takes it out. It has no delineation node; the BMP sits just downstream of its subbasin.
                if (delineation.IsVerified)
                {
                    await NereidUtilities.MarkTreatmentBMPDirty(delineation.TreatmentBMP, DbContext);
                }
                else
                {
                    // the BMP is no longer a node, so its cached results are stale and marking it would
                    // do nothing: re-solve its subbasin, which now flows straight on downstream
                    await DbContext.NereidResults.Where(x => x.TreatmentBMPID == delineation.TreatmentBMPID).ExecuteDeleteAsync();
                    await NereidUtilities.MarkRegionalSubbasinsAndTheirCentralizedBMPsDirty([delineation.TreatmentBMP.RegionalSubbasinID], DbContext);
                }
            }
            else if (delineation.IsVerified)
            {
                await NereidUtilities.MarkDelineationDirty(delineation, DbContext);
            }
            else
            {
                await NereidUtilities.MarkTreatmentBMPDirty(delineation.TreatmentBMP, DbContext);
            }

            // NPT-1155: only verified delineations are Load Generating Unit inputs (see
            // Delineations.ListLoadGeneratingUnitInputs), so a change of status adds or removes this one as an input
            // and the LGUs under it have to be rebuilt.
            if (wasVerified != delineation.IsVerified)
            {
                var lguInputGeometries = await Delineations.ListLoadGeneratingUnitInputGeometriesAsync(DbContext, [delineationID], delineation.IsVerified);
                if (lguInputGeometries.TryGetValue(delineationID, out var lguInputGeometry))
                {
                    await ModelingEngineUtilities.QueueLGURefreshForArea(lguInputGeometry, null, DbContext);
                }
            }

            return NoContent();
        }

        [HttpDelete("for-treatment-bmp/{treatmentBMPID}")]
        [TreatmentBMPEditFeature]
        [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
        public async Task<ActionResult> DeleteForTreatmentBMP([FromRoute] int treatmentBMPID)
        {
            var delineation = Delineations.GetByTreatmentBMPIDWithChangeTracking(DbContext, treatmentBMPID);
            if (delineation == null)
            {
                return NoContent();
            }

            var isDistributed = delineation.DelineationType == DelineationType.Distributed;
            var geometry = delineation.DelineationGeometry;

            await delineation.DeleteFull(DbContext);

            if (isDistributed && geometry != null)
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(geometry, null, DbContext);
            }

            return NoContent();
        }

        [HttpGet("reconciliation-report/context")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<DelineationReconciliationReportContextDto>> GetReconciliationReportContext()
        {
            var lastUpdated = await DbContext.RegionalSubbasins.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdate);
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            return Ok(new DelineationReconciliationReportContextDto
            {
                RegionalSubbasinsLastUpdated = lastUpdated,
                CallerIsAdministrator = currentPerson.IsAdministrator(),
            });
        }

        [HttpGet("reconciliation-report/discrepancies")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<DelineationReconciliationDiscrepancyGridDto>>> GetReconciliationDiscrepancies()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            return Ok(await Delineations.ListDiscrepancyGridDtosAsync(DbContext, currentPerson));
        }

        [HttpGet("reconciliation-report/overlaps")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<DelineationReconciliationOverlapGridDto>>> GetReconciliationOverlaps()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            return Ok(await Delineations.ListOverlapGridDtosAsync(DbContext, currentPerson));
        }

        [HttpPost("reconciliation-report/enqueue-check")]
        [AdminFeature]
        public IActionResult EnqueueReconciliationCheck()
        {
            BackgroundJob.Enqueue<DelineationCheckDiscrepanciesJob>(x => x.RunJob());
            return Ok(new { message = "The job to check BMP delineation discrepancies and overlaps has been queued. Please check back in a few minutes to see updated results." });
        }
    }
}
