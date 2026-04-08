using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    }
}
