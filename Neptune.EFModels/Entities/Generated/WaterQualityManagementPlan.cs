using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("WaterQualityManagementPlan")]
[Index("WaterQualityManagementPlanName", "StormwaterJurisdictionID", Name = "AK_WaterQualityManagementPlan_WaterQualityManagementPlanName_StormwaterJurisdictionID", IsUnique = true)]
public partial class WaterQualityManagementPlan
{
    [Key]
    public int WaterQualityManagementPlanID { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public int? WaterQualityManagementPlanLandUseID { get; set; }

    public int? WaterQualityManagementPlanPriorityID { get; set; }

    public int? WaterQualityManagementPlanStatusID { get; set; }

    public int? WaterQualityManagementPlanDevelopmentTypeID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovalDate { get; set; }

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

    public int? HydromodificationAppliesTypeID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateOfConstruction { get; set; }

    public int? HydrologicSubareaID { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? RecordNumber { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal? RecordedWQMPAreaInAcres { get; set; }

    public int TrashCaptureStatusTypeID { get; set; }

    public int? TrashCaptureEffectiveness { get; set; }

    public int WaterQualityManagementPlanModelingApproachID { get; set; }

    public int? LastNereidLogID { get; set; }

    [StringLength(500)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanBoundaryNotes { get; set; }

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<DirtyModelNode> DirtyModelNodes { get; set; } = new List<DirtyModelNode>();

    [ForeignKey("HydrologicSubareaID")]
    [InverseProperty("WaterQualityManagementPlans")]
    public virtual HydrologicSubarea? HydrologicSubarea { get; set; }

    [ForeignKey("LastNereidLogID")]
    [InverseProperty("WaterQualityManagementPlans")]
    public virtual NereidLog? LastNereidLog { get; set; }

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<LoadGeneratingUnit> LoadGeneratingUnits { get; set; } = new List<LoadGeneratingUnit>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<NereidResult> NereidResults { get; set; } = new List<NereidResult>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<ProjectLoadGeneratingUnit> ProjectLoadGeneratingUnits { get; set; } = new List<ProjectLoadGeneratingUnit>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<ProjectNereidResult> ProjectNereidResults { get; set; } = new List<ProjectNereidResult>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<QuickBMP> QuickBMPs { get; set; } = new List<QuickBMP>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<SourceControlBMP> SourceControlBMPs { get; set; } = new List<SourceControlBMP>();

    [ForeignKey("StormwaterJurisdictionID")]
    [InverseProperty("WaterQualityManagementPlans")]
    public virtual StormwaterJurisdiction StormwaterJurisdiction { get; set; } = null!;

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<TrashGeneratingUnit4326> TrashGeneratingUnit4326s { get; set; } = new List<TrashGeneratingUnit4326>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<TrashGeneratingUnit> TrashGeneratingUnits { get; set; } = new List<TrashGeneratingUnit>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<TreatmentBMP> TreatmentBMPs { get; set; } = new List<TreatmentBMP>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual WaterQualityManagementPlanBoundary? WaterQualityManagementPlanBoundary { get; set; }

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<WaterQualityManagementPlanDocument> WaterQualityManagementPlanDocuments { get; set; } = new List<WaterQualityManagementPlanDocument>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual WaterQualityManagementPlanExtractionResult? WaterQualityManagementPlanExtractionResult { get; set; }

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<WaterQualityManagementPlanParcel> WaterQualityManagementPlanParcels { get; set; } = new List<WaterQualityManagementPlanParcel>();

    [InverseProperty("WaterQualityManagementPlan")]
    public virtual ICollection<WaterQualityManagementPlanVerify> WaterQualityManagementPlanVerifies { get; set; } = new List<WaterQualityManagementPlanVerify>();
}
