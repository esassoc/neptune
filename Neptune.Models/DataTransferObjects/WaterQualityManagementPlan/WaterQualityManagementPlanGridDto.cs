namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanGridDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; }
    public string? WaterQualityManagementPlanPriorityDisplayName { get; set; }
    public string? WaterQualityManagementPlanStatusDisplayName { get; set; }
    public string? WaterQualityManagementPlanDevelopmentTypeDisplayName { get; set; }
    public string? WaterQualityManagementPlanLandUseDisplayName { get; set; }
    public string? WaterQualityManagementPlanPermitTermDisplayName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? DateOfConstruction { get; set; }
    public string? HydromodificationAppliesTypeDisplayName { get; set; }
    public string? HydrologicSubareaName { get; set; }
    public string? MaintenanceContactOrganization { get; set; }
    public string? MaintenanceContactName { get; set; }
    public string? MaintenanceContactPhone { get; set; }
    public string? MaintenanceContactAddress { get; set; }
    public int TreatmentBMPCount { get; set; }
    public int QuickBMPCount { get; set; }
    public string? WaterQualityManagementPlanModelingApproachDisplayName { get; set; }
    public int DocumentCount { get; set; }
    public bool? HasRequiredDocuments { get; set; }
    public string? RecordNumber { get; set; }
    public decimal? RecordedWQMPAreaInAcres { get; set; }
    public double? CalculatedWQMPAcreage { get; set; }
    public string AssociatedAPNs { get; set; }
    public DateTime? VerificationDate { get; set; }
    public string? TrashCaptureStatusTypeDisplayName { get; set; }
    public int? TrashCaptureEffectiveness { get; set; }
    public bool HasBoundary { get; set; }
}
