using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;
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

            delineation.IsVerified = dto.IsVerified;
            delineation.DateLastVerified = DateTime.UtcNow;
            delineation.VerifiedByPersonID = currentPerson.PersonID;
            await DbContext.SaveChangesAsync();

            if (delineation.IsVerified)
            {
                await NereidUtilities.MarkDelineationDirty(delineation, DbContext);
            }
            else
            {
                await NereidUtilities.MarkTreatmentBMPDirty(delineation.TreatmentBMP, DbContext);
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
    }
}
