using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("trash-generating-units")]
public class TrashGeneratingUnitController(
    NeptuneDbContext dbContext,
    ILogger<TrashGeneratingUnitController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    GDALAPIService gdalApiService)
    : SitkaController<TrashGeneratingUnitController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<TrashGeneratingUnitGridDto>>> ListForCallingUser()
    {
        var trashGeneratingUnitGridDtos = await TrashGeneratingUnits.ListAsGridDtoAsync(DbContext, CallingUser);
        return Ok(trashGeneratingUnitGridDtos);
    }

    [HttpGet("{trashGeneratingUnitID}")]
    [AllowAnonymous]
    public async Task<ActionResult<TrashGeneratingUnitDto>> Get([FromRoute] int trashGeneratingUnitID)
    {
        var trashGeneratingUnitDto = await DbContext.vTrashGeneratingUnitLoadStatistics
            .Where(x => x.TrashGeneratingUnitID == trashGeneratingUnitID).Select(x => new TrashGeneratingUnitDto()
            {
                TrashGeneratingUnitID = x.TrashGeneratingUnitID,
                TrashCaptureEffectivenessBMP = x.TrashCaptureEffectivenessBMP,
                TreatmentBMPName = x.TreatmentBMPName,
                TreatmentBMPTypeName = x.TreatmentBMPTypeName,
                TreatmentBMPID = x.TreatmentBMPID,
                TrashCaptureStatusBMP = x.TrashCaptureStatusBMP,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                StormwaterJurisdictionName = x.OrganizationName,
                BaselineLoadingRate = x.BaselineLoadingRate,
                ProgressLoadingRate = x.ProgressLoadingRate,
                LoadingRateDelta = x.LoadingRateDelta,
                LandUseType = x.LandUseType,
                PermitClass = x.PermitClass,
                CurrentLoadingRate = x.CurrentLoadingRate,
                OnlandVisualTrashAssessmentAreaID = x.OnlandVisualTrashAssessmentAreaID,
                OnlandVisualTrashAssessmentAreaName = x.OnlandVisualTrashAssessmentAreaName,
                OnlandVisualTrashAssessmentAreaBaselineScore = x.OnlandVisualTrashAssessmentAreaBaselineScore,
                OnlandVisualTrashAssessmentAreaProgressScore = x.OnlandVisualTrashAssessmentAreaProgressScore,
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = x.WaterQualityManagementPlanName,
                TrashCaptureStatusWQMP = x.TrashCaptureStatusWQMP,
                TrashCaptureEffectivenessWQMP = x.TrashCaptureEffectivenessWQMP,
                LastUpdateDate = x.LastUpdateDate,
                Area = x.Area * Constants.SquareMetersToAcres,
                AssessmentDate = x.MostRecentAssessmentDate,
                CompletedBaselineAssessmentCount = x.CompletedBaselineAssessmentCount,
                CompletedProgressAssessmentCount = x.CompletedProgressAssessmentCount
            }).FirstOrDefaultAsync();
        
        return Ok(trashGeneratingUnitDto);
    }

    [HttpGet("last-update-date")]
    [AllowAnonymous]
    public async Task<ActionResult<DateTime>> GetLastUpdateDate()
    {
        var lastUpdateDate = (await DbContext.vTrashGeneratingUnitLoadStatistics.FirstOrDefaultAsync())?.LastUpdateDate;
        return Ok(lastUpdateDate);
    }

    // NPT-1134: same roles as the grid endpoint, and unlike the role-only Land Use Block / OVTA Area /
    // Delineation downloads, the requested jurisdiction must be one the caller can see in the grid.
    [HttpPost("download-gdb")]
    [JurisdictionEditFeature]
    [Produces("application/zip")]
    public async Task<IActionResult> DownloadGdb([FromBody] TrashGeneratingUnitGdbDownloadRequestDto dto)
    {
        if (!await TrashGeneratingUnitGdbExport.CanDownloadJurisdictionAsync(DbContext, CallingUser.PersonID, dto.StormwaterJurisdictionID))
        {
            return Forbid();
        }

        var (bytes, fileName) = await TrashGeneratingUnitGdbExport.BuildJurisdictionGdbExportAsync(DbContext, gdalApiService, dto.StormwaterJurisdictionID);
        return File(bytes, "application/zip", fileName);
    }
}
