using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vWaterQualityManagementPlanDetailed
{
    public int WaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string StormwaterJurisdictionName { get; set; } = null!;

    public int StormwaterJurisdictionPublicWQMPVisibilityTypeID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovalDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateOfConstruction { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? RecordNumber { get; set; }

    public int? WaterQualityManagementPlanLandUseID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanLandUseDisplayName { get; set; }

    public int? WaterQualityManagementPlanPriorityID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanPriorityDisplayName { get; set; }

    public int? WaterQualityManagementPlanStatusID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanStatusDisplayName { get; set; }

    public int? WaterQualityManagementPlanDevelopmentTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanDevelopmentTypeDisplayName { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactName { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactOrganization { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactPhone { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactAddress1 { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactAddress2 { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactCity { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactState { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? MaintenanceContactZip { get; set; }

    public int? WaterQualityManagementPlanPermitTermID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanPermitTermDisplayName { get; set; }

    public int? HydromodificationAppliesTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? HydromodificationAppliesTypeDisplayName { get; set; }

    public int? HydrologicSubareaID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? HydrologicSubareaName { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? RecordedWQMPAreaInAcres { get; set; }

    public double? CalculatedWQMPAcreage { get; set; }

    public int? TrashCaptureStatusTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TrashCaptureStatusTypeDisplayName { get; set; }

    public int? TrashCaptureEffectiveness { get; set; }

    public int? WaterQualityManagementPlanModelingApproachID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanModelingApproachDisplayName { get; set; }

    public int TreatmentBMPCount { get; set; }

    public int QuickBMPCount { get; set; }

    public int SourceControlBMPCount { get; set; }

    public int DocumentCount { get; set; }

    [Unicode(false)]
    public string AssociatedAPNs { get; set; } = null!;

    public bool? HasRequiredDocuments { get; set; }

    public int? WaterQualityManagementPlanVerifyID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? VerificationDate { get; set; }

    public bool HasBoundary { get; set; }
}
