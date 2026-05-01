using System;

namespace Neptune.Models.DataTransferObjects;

public class MaintenanceRecordGridDto
{
    public int MaintenanceRecordID { get; set; }
    public int FieldVisitID { get; set; }
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public DateTime VisitDate { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; } = null!;
    public int? WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int PerformedByPersonID { get; set; }
    public string? PerformedByPersonName { get; set; }
    public int? MaintenanceRecordTypeID { get; set; }
    public string? MaintenanceRecordTypeDisplayName { get; set; }
    public string? MaintenanceRecordDescription { get; set; }

    // Maintenance observation columns mirroring vMaintenanceRecordDetailed
    public string? StructuralRepairConducted { get; set; }
    public string? MechanicalRepairConducted { get; set; }
    public string? InfiltrationSurfaceRestored { get; set; }
    public string? FiltrationSurfaceRestored { get; set; }
    public string? MediaReplaced { get; set; }
    public string? MulchAdded { get; set; }
    public string? PercentTrash { get; set; }
    public string? PercentGreenWaste { get; set; }
    public string? PercentSediment { get; set; }
    public string? AreaReseeded { get; set; }
    public string? VegetationPlanted { get; set; }
    public string? SurfaceAndBankErosionRepaired { get; set; }
    public string? TotalMaterialVolumeRemovedCubicFeet { get; set; }
    public string? TotalMaterialVolumeRemovedGallons { get; set; }
}
