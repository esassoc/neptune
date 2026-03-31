namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionOrganizationName { get; set; }
    public int? WaterQualityManagementPlanLandUseID { get; set; }
    public string? WaterQualityManagementPlanLandUseDisplayName { get; set; }
    public int? WaterQualityManagementPlanPriorityID { get; set; }
    public string? WaterQualityManagementPlanPriorityDisplayName { get; set; }
    public int? WaterQualityManagementPlanStatusID { get; set; }
    public string? WaterQualityManagementPlanStatusDisplayName { get; set; }
    public int? WaterQualityManagementPlanDevelopmentTypeID { get; set; }
    public string? WaterQualityManagementPlanDevelopmentTypeDisplayName { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? MaintenanceContactName { get; set; }
    public string? MaintenanceContactOrganization { get; set; }
    public string? MaintenanceContactPhone { get; set; }
    public string? MaintenanceContactAddress1 { get; set; }
    public string? MaintenanceContactAddress2 { get; set; }
    public string? MaintenanceContactCity { get; set; }
    public string? MaintenanceContactState { get; set; }
    public string? MaintenanceContactZip { get; set; }
    public int? WaterQualityManagementPlanPermitTermID { get; set; }
    public string? WaterQualityManagementPlanPermitTermDisplayName { get; set; }
    public int? HydromodificationAppliesTypeID { get; set; }
    public string? HydromodificationAppliesTypeDisplayName { get; set; }
    public DateTime? DateOfConstruction { get; set; }
    public int? HydrologicSubareaID { get; set; }
    public string? HydrologicSubareaName { get; set; }
    public string? RecordNumber { get; set; }
    public decimal? RecordedWQMPAreaInAcres { get; set; }
    public int TrashCaptureStatusTypeID { get; set; }
    public int? TrashCaptureEffectiveness { get; set; }
    public string? TrashCaptureStatusTypeDisplayName { get; set; }
    public int WaterQualityManagementPlanModelingApproachID { get; set; }
    public int? LastNereidLogID { get; set; }
    public string? WaterQualityManagementPlanBoundaryNotes { get; set; }
    public string? WaterQualityManagementPlanBoundaryBBox { get; set; }
    public double? CalculatedWQMPAcreage { get; set; }

    public List<ParcelDisplayDto> Parcels { get; set; }
    public List<TreatmentBMPMinimalDto> TreatmentBMPs { get; set; }
}

public class WaterQualityManagementPlanDisplayDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
}
