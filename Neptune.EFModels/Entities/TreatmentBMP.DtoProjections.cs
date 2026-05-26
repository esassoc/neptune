using System.Linq.Expressions;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.ManagerDashboard;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPDtoProjections
{
    // Manager Dashboard "Provisional BMP Records" grid. Returns an Expression so the entire
    // projection — including HasPhotos (EXISTS) and BenchmarkAndThresholdsSet (NOT EXISTS via
    // a captured IDs list) — translates to SQL, avoiding the empty-navigation pitfall the
    // original in-memory implementation hit (Copilot review on PR #529).
    //
    // CanDelete depends on the calling Person's role + assigned jurisdictions, which can't be
    // expressed in SQL; the caller stamps it post-materialize.
    public static Expression<Func<TreatmentBMP, TreatmentBMPProvisionalGridDto>> AsProvisionalGridDto(IReadOnlyCollection<int> observationTypeSpecificationIDsThatRequireBenchmark)
    {
        return x => new TreatmentBMPProvisionalGridDto
        {
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
            DateOfLastInventoryVerification = x.DateOfLastInventoryVerification,
            InventoryLastChangedDate = x.InventoryLastChangedDate,
            HasPhotos = x.TreatmentBMPImages.Any(),
            // "All required-benchmark observation types for this BMP type have a matching
            // TreatmentBMPBenchmarkAndThreshold row." Equivalent to the legacy
            // TreatmentBMP.IsBenchmarkAndThresholdsComplete logic, but SQL-translatable —
            // requiring-benchmark spec IDs come from the captured collection so EF doesn't
            // have to translate the static ObservationTypeSpecification lookup.
            BenchmarkAndThresholdsSet = x.TreatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                .Where(t => observationTypeSpecificationIDsThatRequireBenchmark.Contains(t.TreatmentBMPAssessmentObservationType.ObservationTypeSpecificationID))
                .All(t => x.TreatmentBMPBenchmarkAndThresholds.Any(b => b.TreatmentBMPAssessmentObservationTypeID == t.TreatmentBMPAssessmentObservationTypeID)),
            StormwaterJurisdictionID = x.StormwaterJurisdictionID,
            StormwaterJurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationName,
            CanDelete = false, // resolved post-materialize from the calling Person
        };
    }

    public static readonly Expression<Func<TreatmentBMP, TreatmentBMPDto>> AsDto = x => new TreatmentBMPDto
    {
        TreatmentBMPID = x.TreatmentBMPID,
        TreatmentBMPName = x.TreatmentBMPName,
        TreatmentBMPTypeID = x.TreatmentBMPTypeID,
        TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
        StormwaterJurisdictionID = x.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationName,
        OwnerOrganizationID = x.OwnerOrganizationID,
        OwnerOrganizationName = x.OwnerOrganization.OrganizationName,
        YearBuilt = x.YearBuilt,
        Notes = x.Notes,
        InventoryIsVerified = x.InventoryIsVerified,
        ProjectID = x.ProjectID,
        Latitude = x.LocationPoint4326 != null ? (double?)x.LocationPoint4326.Coordinate.Y : null,
        Longitude = x.LocationPoint4326 != null ? (double?)x.LocationPoint4326.Coordinate.X : null,
        SystemOfRecordID = x.SystemOfRecordID,
        WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
        TreatmentBMPLifespanEndDate = x.TreatmentBMPLifespanEndDate,
        RequiredFieldVisitsPerYear = x.RequiredFieldVisitsPerYear,
        RequiredPostStormFieldVisitsPerYear = x.RequiredPostStormFieldVisitsPerYear,
        DateOfLastInventoryVerification = x.DateOfLastInventoryVerification,
        InventoryVerifiedByPersonID = x.InventoryVerifiedByPersonID,
        InventoryLastChangedDate = x.InventoryLastChangedDate,
        TrashCaptureStatusTypeID = x.TrashCaptureStatusTypeID,
        SizingBasisTypeID = x.SizingBasisTypeID,
        TrashCaptureEffectiveness = x.TrashCaptureEffectiveness != null ? x.TrashCaptureEffectiveness.Value.ToString() : null,
        WatershedID = x.WatershedID,
        ModelBasinID = x.ModelBasinID,
        PrecipitationZoneID = x.PrecipitationZoneID,
        UpstreamBMPID = x.UpstreamBMPID,
        RegionalSubbasinID = x.RegionalSubbasinID,
        LastNereidLogID = x.LastNereidLogID,
        TreatmentBMPAssessmentCount = x.TreatmentBMPAssessments.Count,
        // Lookup types: project ID-only DTOs, names resolved client-side via ResolveClientSideLookups
        SizingBasisType = new SizingBasisTypeDto
        {
            SizingBasisTypeID = x.SizingBasisTypeID
        },
        TrashCaptureStatusType = new TrashCaptureStatusTypeDto
        {
            TrashCaptureStatusTypeID = x.TrashCaptureStatusTypeID
        },
        TreatmentBMPLifespanType = x.TreatmentBMPLifespanTypeID != null ? new TreatmentBMPLifeSpanTypeDto
        {
            TreatmentBMPLifeSpanTypeID = x.TreatmentBMPLifespanTypeID.Value
        } : null,
        Watershed = x.Watershed != null ? new WatershedDto
        {
            WatershedID = x.Watershed.WatershedID,
            WatershedName = x.Watershed.WatershedName
        } : null,
        WaterQualityManagementPlan = x.WaterQualityManagementPlan != null ? new WaterQualityManagementPlanSimpleDto
        {
            WaterQualityManagementPlanID = x.WaterQualityManagementPlan.WaterQualityManagementPlanID,
            StormwaterJurisdictionID = x.WaterQualityManagementPlan.StormwaterJurisdictionID,
            WaterQualityManagementPlanLandUseID = x.WaterQualityManagementPlan.WaterQualityManagementPlanLandUseID,
            WaterQualityManagementPlanPriorityID = x.WaterQualityManagementPlan.WaterQualityManagementPlanPriorityID,
            WaterQualityManagementPlanStatusID = x.WaterQualityManagementPlan.WaterQualityManagementPlanStatusID,
            WaterQualityManagementPlanDevelopmentTypeID = x.WaterQualityManagementPlan.WaterQualityManagementPlanDevelopmentTypeID,
            WaterQualityManagementPlanName = x.WaterQualityManagementPlan.WaterQualityManagementPlanName,
            ApprovalDate = x.WaterQualityManagementPlan.ApprovalDate,
            WaterQualityManagementPlanPermitTermID = x.WaterQualityManagementPlan.WaterQualityManagementPlanPermitTermID,
            HydromodificationAppliesTypeID = x.WaterQualityManagementPlan.HydromodificationAppliesTypeID,
            DateOfConstruction = x.WaterQualityManagementPlan.DateOfConstruction,
            HydrologicSubareaID = x.WaterQualityManagementPlan.HydrologicSubareaID,
            RecordNumber = x.WaterQualityManagementPlan.RecordNumber,
            RecordedWQMPAreaInAcres = x.WaterQualityManagementPlan.RecordedWQMPAreaInAcres,
            TrashCaptureStatusTypeID = x.WaterQualityManagementPlan.TrashCaptureStatusTypeID,
            TrashCaptureEffectiveness = x.WaterQualityManagementPlan.TrashCaptureEffectiveness,
            WaterQualityManagementPlanModelingApproachID = x.WaterQualityManagementPlan.WaterQualityManagementPlanModelingApproachID
        } : null,
        RegionalSubbasinRevisionRequests = x.RegionalSubbasinRevisionRequests.Select(r => new RegionalSubbasinRevisionRequestDto
        {
            RegionalSubbasinRevisionRequestID = r.RegionalSubbasinRevisionRequestID,
            RegionalSubbasinRevisionRequestStatusID = r.RegionalSubbasinRevisionRequestStatusID
        }).ToList(),
        Delineation = x.Delineation != null ? new DelineationDto
        {
            DelineationID = x.Delineation.DelineationID,
            DelineationTypeID = x.Delineation.DelineationTypeID,
            IsVerified = x.Delineation.IsVerified,
            DateLastVerified = x.Delineation.DateLastVerified,
            VerifiedByPersonID = x.Delineation.VerifiedByPersonID,
            TreatmentBMPID = x.Delineation.TreatmentBMPID,
            DateLastModified = x.Delineation.DateLastModified,
            HasDiscrepancies = x.Delineation.HasDiscrepancies,
            DelineationArea = x.Delineation.DelineationGeometry != null
                ? (double?)Math.Round(x.Delineation.DelineationGeometry.Area * Constants.SquareMetersToAcres, 2)
                : null
            // Geometry (GeoJSON) and DelineationTypeName are resolved client-side
        } : null
        // UpstreamBMP - resolved post-query via recursive call
        // OtherTreatmentBMPsExistInSubbasin - resolved post-query via separate query
    };
}
