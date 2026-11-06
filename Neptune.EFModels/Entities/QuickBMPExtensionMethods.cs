using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static partial class QuickBMPExtensionMethods
{
    public static QuickBMPGridDto AsGridDto(this QuickBMP quickBMP, string? treatmentBMPTypeName, vWaterQualityManagementPlanDetailed waterQualityManagementPlan)
    {
        var calculatedBoundaryAcreage = GetCalculatedBoundaryAcreage(waterQualityManagementPlan.CalculatedWQMPAcreage);
        var drainageAreaBasisAcreage = GetDrainageAreaBasisAcreage(calculatedBoundaryAcreage, waterQualityManagementPlan.RecordedWQMPAreaInAcres);
        return new QuickBMPGridDto
        {
            QuickBMPID = quickBMP.QuickBMPID,
            QuickBMPName = quickBMP.QuickBMPName,
            TreatmentBMPTypeID = quickBMP.TreatmentBMPTypeID,
            TreatmentBMPTypeName = treatmentBMPTypeName,
            NumberOfIndividualBMPs = quickBMP.NumberOfIndividualBMPs,
            WaterQualityManagementPlanID = waterQualityManagementPlan.WaterQualityManagementPlanID,
            WaterQualityManagementPlanName = waterQualityManagementPlan.WaterQualityManagementPlanName,
            StormwaterJurisdictionID = waterQualityManagementPlan.StormwaterJurisdictionID,
            StormwaterJurisdictionName = waterQualityManagementPlan.StormwaterJurisdictionName,
            WaterQualityManagementPlanStatusDisplayName = waterQualityManagementPlan.WaterQualityManagementPlanStatusDisplayName,
            HydrologicSubareaName = waterQualityManagementPlan.HydrologicSubareaName,
            PercentOfSiteTreated = quickBMP.PercentOfSiteTreated,
            RecordedWQMPAreaInAcres = waterQualityManagementPlan.RecordedWQMPAreaInAcres,
            CalculatedBoundaryAcreage = calculatedBoundaryAcreage,
            DrainageAreaTreatedAcres = GetDrainageAreaTreatedAcres(quickBMP.PercentOfSiteTreated, drainageAreaBasisAcreage),
            PercentCaptured = quickBMP.PercentCaptured,
            PercentRetained = quickBMP.PercentRetained,
            DryWeatherFlowOverrideID = quickBMP.DryWeatherFlowOverrideID,
            DryWeatherFlowOverrideDisplayName = quickBMP.DryWeatherFlowOverrideID.HasValue && DryWeatherFlowOverride.AllLookupDictionary.TryGetValue(quickBMP.DryWeatherFlowOverrideID.Value, out var dryWeatherFlowOverride)
                ? dryWeatherFlowOverride.DryWeatherFlowOverrideDisplayName
                : null,
            QuickBMPNote = quickBMP.QuickBMPNote,
        };
    }

    /// <summary>
    /// The area of the WQMP's mapped boundary (GIS only, never the user-entered recorded area). The view
    /// reports a missing boundary as 0 acres, so 0 is treated as blank rather than a real area.
    /// </summary>
    public static decimal? GetCalculatedBoundaryAcreage(double? calculatedWQMPAcreage)
    {
        return calculatedWQMPAcreage is > 0 ? (decimal)calculatedWQMPAcreage.Value : null;
    }

    /// <summary>
    /// The area Drainage Area Treated is based on: the boundary area, falling back to the recorded
    /// (user-entered) WQMP area when the WQMP has no boundary (NPT-1137 rework, AP decision 11/4).
    /// </summary>
    public static decimal? GetDrainageAreaBasisAcreage(decimal? calculatedBoundaryAcreage, decimal? recordedWQMPAreaInAcres)
    {
        return calculatedBoundaryAcreage ?? recordedWQMPAreaInAcres;
    }

    public static decimal? GetDrainageAreaTreatedAcres(decimal? percentOfSiteTreated, decimal? wqmpAcreage)
    {
        if (!percentOfSiteTreated.HasValue || !wqmpAcreage.HasValue)
        {
            return null;
        }

        return Math.Round(percentOfSiteTreated.Value / 100m * wqmpAcreage.Value, 2, MidpointRounding.AwayFromZero);
    }
}
