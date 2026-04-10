using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;

namespace Neptune.API.Controllers;

[ApiController]
[Route("regional-subbasins")]
public class RegionalSubbasinController : SitkaController<RegionalSubbasinController>
{
    public RegionalSubbasinController(
        NeptuneDbContext dbContext,
        ILogger<RegionalSubbasinController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : base(dbContext, logger, neptuneConfiguration)
    {
    }

    [HttpGet]
    [AdminFeature]
    public async Task<ActionResult<List<RegionalSubbasinDto>>> List()
    {
        var regionalSubbasins = await RegionalSubbasins.ListAsDtoAsync(DbContext);
        return Ok(regionalSubbasins);
    }

    [HttpGet("{regionalSubbasinID}")]
    [AdminFeature]
    [EntityNotFoundAttribute(typeof(RegionalSubbasin), "regionalSubbasinID")]
    public async Task<ActionResult<RegionalSubbasinDto>> Get([FromRoute] int regionalSubbasinID)
    {
        var regionalSubbasin = await RegionalSubbasins.GetByIDAsDtoAsync(DbContext, regionalSubbasinID);
        if (regionalSubbasin == null) return NotFound();
        return Ok(regionalSubbasin);
    }

    [HttpGet("{regionalSubbasinID}/hru-characteristics")]
    [EntityNotFound(typeof(RegionalSubbasin), "regionalSubbasinID")]
    [AdminFeature]
    public async Task<ActionResult<List<HRUCharacteristicDto>>> ListHRUCharacteristics([FromRoute] int regionalSubbasinID)
    {
        var hruCharacteristics = await vHRUCharacteristics.ListByRegionalSubbasinAsGridDtoAsync(DbContext, regionalSubbasinID);
        return Ok(hruCharacteristics);
    }

    [HttpGet("{regionalSubbasinID}/load-generating-units")]
    [EntityNotFound(typeof(RegionalSubbasin), "regionalSubbasinID")]
    [AdminFeature]
    public async Task<ActionResult<List<LoadGeneratingUnitGridDto>>> ListLoadGeneratingUnits([FromRoute] int regionalSubbasinID)
    {
        var dtos = await vLoadGeneratingUnits.ListByRegionalSubbasinAsGridDtoAsync(DbContext, regionalSubbasinID);
        return Ok(dtos);
    }

    [HttpPost("/graph-trace-as-feature-collection-from-point")]
    [UserViewFeature]
    public ActionResult<FeatureCollection> GetRegionalSubbasinGraphTraceAsFeatureCollectionFromPoint([FromBody] CoordinateDto coordinateDto)
    {
        var featureCollection = RegionalSubbasins.GetRegionalSubbasinGraphTraceAsFeatureCollection(DbContext, coordinateDto);
        return Ok(featureCollection);
    }
}