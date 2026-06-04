using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.WebService;

namespace Neptune.ExternalAPI.Controllers;

[Authorize]
[ApiController]
[Tags("Treatment Facilities")]
[Route("treatment-bmp")]
public class TreatmentBMPController(NeptuneDbContext dbContext) : ControllerBase
{
    [HttpGet("parameterization")]
    [EndpointSummary("Treatment Facility Parameterization")]
    [EndpointDescription("This table can be joined to the 'Treatment Facility Attributes' table to indicate if a facility is fully parameterized and ready to be computed in the Modeling Module. The BMP Inventory and Modeling Module in the OCST website provide new indicators and alerts to help determine which attributes are missing for a specific facility.")]
    [ProducesResponseType(typeof(IEnumerable<TreatmentBMPParameterizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<TreatmentBMPParameterizationDto>> Parameterization()
    {
        var delineations = vTreatmentBMPUpstreams.ListWithDelineationAsDictionary(dbContext);
        var treatmentBMPModelingAttributes = vTreatmentBMPModelingAttributes.ListAsDictionary(dbContext);
        var data = TreatmentBMPs.GetNonPlanningModuleBMPs(dbContext)
            .Where(x => x.TreatmentBMPType.IsAnalyzedInModelingModule)
            .ToList()
            .Select(x =>
            {
                var isFullyParameterized = x.IsFullyParameterized(
                    delineations[x.TreatmentBMPID],
                    treatmentBMPModelingAttributes.TryGetValue(x.TreatmentBMPID, out var attribute) ? attribute : null) ? "Yes" : "No";
                return new TreatmentBMPParameterizationDto
                {
                    TreatmentBMPID = x.TreatmentBMPID,
                    TreatmentBMPName = x.TreatmentBMPName,
                    TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
                    FullyParameterized = isFullyParameterized,
                    IsReadyForModeling = isFullyParameterized
                };
            });
        return Ok(data);
    }

    [HttpGet("attributes")]
    [EndpointSummary("Treatment Facility Attributes, Centralized BMP Attributes")]
    [EndpointDescription("This table contains the Modeling Attributes that have been entered for each Facility. Each row is a single facility and its physical attributes. Null values for a modeling parameter in this table does not necessarily indicate that the BMP is missing data. This is because there are usually only a few parameters required for each bmp type, and because there are many types of BMPs in this table. See the 'Treatment Facility Parameterization' table for the indicator that the BMP is missing data.\n\nThis table also includes additional information to help locate and filter the facilities such as lat / lon, watershed, and jurisdiction.\n\nThe second table, 'Centralized BMP Attrs' is identical to the first, except it's filtered to just the centralized facility delineation types. This is to facilitate reporting calculations related to facility tributary area for which we make different calculations if the facility is centralized or distributed.")]
    [ProducesResponseType(typeof(IEnumerable<TreatmentBMPAttributesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<TreatmentBMPAttributesDto>> Attributes()
    {
        var data = dbContext.vPowerBITreatmentBMPs.Select(x => new TreatmentBMPAttributesDto
        {
            PrimaryKey = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            Jurisdiction = x.Jurisdiction,
            LocationLon = x.LocationLon,
            LocationLat = x.LocationLat,
            Watershed = x.Watershed,
            DelineationType = x.DelineationType,
            WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
            UpstreamTreatmentBMPID = x.UpstreamTreatmentBMPID,
            AverageDivertedFlowrate = x.AverageDivertedFlowrate,
            AverageTreatmentFlowrate = x.AverageTreatmentFlowrate,
            DesignDryWeatherTreatmentCapacity = x.DesignDryWeatherTreatmentCapacity,
            DesignLowFlowDiversionCapacity = x.DesignLowFlowDiversionCapacity,
            DesignMediaFiltrationRate = x.DesignMediaFiltrationRate,
            DiversionRate = x.DiversionRate,
            DrawdownTimeforWQDetentionVolume = x.DrawdownTimeForWQDetentionVolume,
            EffectiveFootprint = x.EffectiveFootprint,
            EffectiveRetentionDepth = x.EffectiveRetentionDepth,
            InfiltrationDischargeRate = x.InfiltrationDischargeRate,
            InfiltrationSurfaceArea = x.InfiltrationSurfaceArea,
            MediaBedFootprint = x.MediaBedFootprint,
            PermanentPoolorWetlandVolume = x.PermanentPoolOrWetlandVolume,
            RoutingConfiguration = x.RoutingConfiguration ?? RoutingConfiguration.Online.RoutingConfigurationDisplayName,
            StorageVolumeBelowLowestOutletElevation = x.StorageVolumeBelowLowestOutletElevation,
            SummerHarvestedWaterDemand = x.SummerHarvestedWaterDemand,
            TimeOfConcentration = x.TimeOfConcentration ?? TimeOfConcentration.FiveMinutes.TimeOfConcentrationDisplayName,
            DrawdownTimeForDetentionVolume = x.DrawdownTimeForDetentionVolume,
            TotalEffectiveBMPVolume = x.TotalEffectiveBMPVolume,
            TotalEffectiveDrywellBMPVolume = x.TotalEffectiveDrywellBMPVolume,
            TreatmentRate = x.TreatmentRate,
            UnderlyingHydrologicSoilGroup = x.UnderlyingHydrologicSoilGroup,
            UnderlyingInfiltrationRate = x.UnderlyingInfiltrationRate,
            WaterQualityDetentionVolume = x.ExtendedDetentionSurchargeVolume,
            WettedFootprint = x.WettedFootprint,
            WinterHarvestedWaterDemand = x.WinterHarvestedWaterDemand
        });
        return Ok(data);
    }

    [HttpGet("load-generating-unit-mapping")]
    [EndpointSummary("Centralized BMP Land Use Relationship")]
    [EndpointDescription("This table is a utility table to enable upstream summary reporting of centralized facilities. Centralized facilities are a special case, since the area they treat may also be treated by a distributed facility, or a WQMP, or even by another centralized BMP upstream. This table is the result of visiting each centralized facility and checking which 'slivers' from the 'Land Use' table are upstream of the current centralized facility. This relationship allows the report to accurately aggregate the area treated by each centralized facility individually. This relationship drives the upstream area calculations on the Centralized Facilities dashboard of the PowerBI file (dated March, 2020).")]
    [ProducesResponseType(typeof(IEnumerable<CentralizedBMPLoadGeneratingUnitMappingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public ActionResult<IEnumerable<CentralizedBMPLoadGeneratingUnitMappingDto>> LoadGeneratingUnitMapping()
    {
        var data = dbContext.vPowerBICentralizedBMPLoadGeneratingUnits.Select(x => new CentralizedBMPLoadGeneratingUnitMappingDto
        {
            LoadGeneratingUnitID = x.LoadGeneratingUnitID,
            TreatmentBMPID = x.TreatmentBMPID
        });
        return Ok(data);
    }
}
