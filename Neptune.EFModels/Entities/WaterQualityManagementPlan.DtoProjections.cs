using System.Linq.Expressions;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanProjections
{
    public static readonly Expression<Func<WaterQualityManagementPlan, WaterQualityManagementPlanDto>> AsDto = plan => new WaterQualityManagementPlanDto
    {
        WaterQualityManagementPlanID = plan.WaterQualityManagementPlanID,
        StormwaterJurisdictionID = plan.StormwaterJurisdictionID,
        StormwaterJurisdictionOrganizationName = plan.StormwaterJurisdiction.Organization.OrganizationName,
        WaterQualityManagementPlanLandUseID = plan.WaterQualityManagementPlanLandUseID,
        WaterQualityManagementPlanLandUseDisplayName = plan.WaterQualityManagementPlanLandUse != null
            ? plan.WaterQualityManagementPlanLandUse.WaterQualityManagementPlanLandUseDisplayName
            : null,
        WaterQualityManagementPlanPriorityID = plan.WaterQualityManagementPlanPriorityID,
        WaterQualityManagementPlanPriorityDisplayName = plan.WaterQualityManagementPlanPriority != null
            ? plan.WaterQualityManagementPlanPriority.WaterQualityManagementPlanPriorityDisplayName
            : null,
        WaterQualityManagementPlanStatusID = plan.WaterQualityManagementPlanStatusID,
        WaterQualityManagementPlanStatusDisplayName = plan.WaterQualityManagementPlanStatus != null
            ? plan.WaterQualityManagementPlanStatus.WaterQualityManagementPlanStatusDisplayName
            : null,
        WaterQualityManagementPlanDevelopmentTypeID = plan.WaterQualityManagementPlanDevelopmentTypeID,
        WaterQualityManagementPlanDevelopmentTypeDisplayName = plan.WaterQualityManagementPlanDevelopmentType != null
            ? plan.WaterQualityManagementPlanDevelopmentType.WaterQualityManagementPlanDevelopmentTypeDisplayName
            : null,
        WaterQualityManagementPlanName = plan.WaterQualityManagementPlanName,
        ApprovalDate = plan.ApprovalDate,
        MaintenanceContactName = plan.MaintenanceContactName,
        MaintenanceContactOrganization = plan.MaintenanceContactOrganization,
        MaintenanceContactPhone = plan.MaintenanceContactPhone,
        MaintenanceContactAddress1 = plan.MaintenanceContactAddress1,
        MaintenanceContactAddress2 = plan.MaintenanceContactAddress2,
        MaintenanceContactCity = plan.MaintenanceContactCity,
        MaintenanceContactState = plan.MaintenanceContactState,
        MaintenanceContactZip = plan.MaintenanceContactZip,
        WaterQualityManagementPlanPermitTermID = plan.WaterQualityManagementPlanPermitTermID,
        WaterQualityManagementPlanPermitTermDisplayName = plan.WaterQualityManagementPlanPermitTerm != null
            ? plan.WaterQualityManagementPlanPermitTerm.WaterQualityManagementPlanPermitTermDisplayName
            : null,
        HydromodificationAppliesTypeID = plan.HydromodificationAppliesTypeID,
        HydromodificationAppliesTypeDisplayName = plan.HydromodificationAppliesType != null
            ? plan.HydromodificationAppliesType.HydromodificationAppliesTypeDisplayName
            : null,
        DateOfConstruction = plan.DateOfConstruction,
        HydrologicSubareaID = plan.HydrologicSubareaID,
        HydrologicSubareaName = plan.HydrologicSubarea != null
            ? plan.HydrologicSubarea.HydrologicSubareaName
            : null,
        RecordNumber = plan.RecordNumber,
        RecordedWQMPAreaInAcres = plan.RecordedWQMPAreaInAcres,
        TrashCaptureStatusTypeID = plan.TrashCaptureStatusTypeID,
        TrashCaptureEffectiveness = plan.TrashCaptureEffectiveness,
        TrashCaptureStatusTypeDisplayName = plan.TrashCaptureStatusType != null
            ? plan.TrashCaptureStatusType.TrashCaptureStatusTypeDisplayName
            : null,
        WaterQualityManagementPlanModelingApproachID = plan.WaterQualityManagementPlanModelingApproachID,
        LastNereidLogID = plan.LastNereidLogID,
        WaterQualityManagementPlanBoundaryNotes = plan.WaterQualityManagementPlanBoundaryNotes,
        // BBox requires NTS EnvelopeInternal which can't be translated to SQL — populated post-query
        WaterQualityManagementPlanBoundaryBBox = null,
        CalculatedWQMPAcreage = plan.WaterQualityManagementPlanBoundary != null && plan.WaterQualityManagementPlanBoundary.GeometryNative != null
            ? (double?)Math.Round(plan.WaterQualityManagementPlanBoundary.GeometryNative.Area * Constants.SquareMetersToAcres, 1)
            : null,
        Parcels = plan.WaterQualityManagementPlanParcels.Select(x => new ParcelDisplayDto
        {
            ParcelID = x.ParcelID,
            ParcelNumber = x.Parcel.ParcelNumber,
        }).ToList(),
        TreatmentBMPs = plan.TreatmentBMPs.Select(x => new TreatmentBMPMinimalDto
        {
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
            Notes = x.Notes,
            DelineationStatus = x.Delineation != null
                ? (x.Delineation.IsVerified ? "Verified" : "Provisional")
                : "None",
            Area = x.Delineation != null && x.Delineation.DelineationGeometry != null
                ? (double?)Math.Round(x.Delineation.DelineationGeometry.Area * Constants.SquareMetersToAcres, 2)
                : null,
            Latitude = x.LocationPoint4326 != null ? (double?)x.LocationPoint4326.Coordinate.Y : null,
            Longitude = x.LocationPoint4326 != null ? (double?)x.LocationPoint4326.Coordinate.X : null
        }).ToList()
    };
}
