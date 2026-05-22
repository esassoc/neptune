using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("parcels")]
    public class ParcelController(
        NeptuneDbContext dbContext,
        ILogger<ParcelController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<ParcelController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<ParcelGridDto>>> List()
        {
            // ETag-based conditional GET: skip the ~14 MB serialize+download when the parcel
            // table hasn't changed since the client's last fetch. Browsers handle 304 transparently
            // and serve the cached body back to Angular's HttpClient, so the SPA needs no changes.
            var etag = EntityTagHeaderValue.Parse(await Parcels.GetGridVersionETagAsync(DbContext));
            Response.GetTypedHeaders().ETag = etag;
            Response.Headers.CacheControl = "private, no-cache";

            // Use typed headers + EntityTagHeaderValue.Compare so we tolerate clients sending
            // multiple tags or weakly-compared variants (W/) per RFC 7232.
            var ifNoneMatch = Request.GetTypedHeaders().IfNoneMatch;
            if (ifNoneMatch != null && ifNoneMatch.Any(t => t.Compare(etag, useStrongComparison: false)))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            var parcels = await Parcels.ListAsGridDtoAsync(DbContext);
            return Ok(parcels);
        }

        [HttpGet("search")]
        [JurisdictionEditFeature]
        public ActionResult<List<ParcelDisplayDto>> Search([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Ok(new List<ParcelDisplayDto>());
            }

            var results = Parcels.Search(DbContext, term);
            return Ok(results);
        }

        // Used by the WQMP AI-extraction review flow to resolve the list of APN strings
        // accepted by the reviewer into Parcel IDs before calling PUT /{id}/parcels. Returns
        // every requested APN; ParcelID is null on a row that doesn't match any parcel so
        // the caller can surface the misses in an alert.
        [HttpPost("lookup-by-numbers")]
        [JurisdictionEditFeature]
        public ActionResult<List<ParcelLookupResultDto>> LookupByNumbers([FromBody] List<string> parcelNumbers)
        {
            if (parcelNumbers == null || parcelNumbers.Count == 0)
            {
                return Ok(new List<ParcelLookupResultDto>());
            }

            var results = Parcels.LookupByParcelNumbers(DbContext, parcelNumbers);
            return Ok(results);
        }
    }
}
