using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.Search;

namespace Neptune.API.Controllers;

[ApiController]
[Route("search")]
public class SearchController(NeptuneDbContext dbContext, ILogger<SearchController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<SearchController>(dbContext, logger, neptuneConfiguration)
{
    // Header record search (NPT-1125). JurisdictionEditFeature is role-only, which is exactly "every signed-in
    // role except Unassigned"; jurisdiction scoping happens inside the query, using the same helper as the
    // BMP/WQMP pickers so search never shows a record those flows would hide (Admin/SitkaAdmin: all).
    [HttpGet("index")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<SearchRecordDto>>> ListIndex()
    {
        var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var records = await SearchRecords.ListForJurisdictionsAsync(DbContext, stormwaterJurisdictionIDs);
        return Ok(records);
    }
}
