using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Table("WaterQualityManagementPlanVerify")]
public partial class WaterQualityManagementPlanVerify
{
    [Key]
    public int WaterQualityManagementPlanVerifyID { get; set; }

    public int WaterQualityManagementPlanID { get; set; }

    public int WaterQualityManagementPlanVerifyTypeID { get; set; }

    public int WaterQualityManagementPlanVisitStatusID { get; set; }

    public int? FileResourceID { get; set; }

    public int? WaterQualityManagementPlanVerifyStatusID { get; set; }

    public int LastEditedByPersonID { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? SourceControlCondition { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? EnforcementOrFollowupActions { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastEditedDate { get; set; }

    public bool IsDraft { get; set; }

    public DateOnly VerificationDate { get; set; }

    [ForeignKey("FileResourceID")]
    [InverseProperty("WaterQualityManagementPlanVerifies")]
    public virtual FileResource? FileResource { get; set; }

    [ForeignKey("LastEditedByPersonID")]
    [InverseProperty("WaterQualityManagementPlanVerifies")]
    public virtual Person LastEditedByPerson { get; set; } = null!;

    [ForeignKey("WaterQualityManagementPlanID")]
    [InverseProperty("WaterQualityManagementPlanVerifies")]
    public virtual WaterQualityManagementPlan WaterQualityManagementPlan { get; set; } = null!;

    [InverseProperty("WaterQualityManagementPlanVerify")]
    public virtual ICollection<WaterQualityManagementPlanVerifyPhoto> WaterQualityManagementPlanVerifyPhotos { get; set; } = new List<WaterQualityManagementPlanVerifyPhoto>();

    [InverseProperty("WaterQualityManagementPlanVerify")]
    public virtual ICollection<WaterQualityManagementPlanVerifyQuickBMP> WaterQualityManagementPlanVerifyQuickBMPs { get; set; } = new List<WaterQualityManagementPlanVerifyQuickBMP>();

    [InverseProperty("WaterQualityManagementPlanVerify")]
    public virtual ICollection<WaterQualityManagementPlanVerifySourceControlBMP> WaterQualityManagementPlanVerifySourceControlBMPs { get; set; } = new List<WaterQualityManagementPlanVerifySourceControlBMP>();

    [InverseProperty("WaterQualityManagementPlanVerify")]
    public virtual ICollection<WaterQualityManagementPlanVerifyTreatmentBMP> WaterQualityManagementPlanVerifyTreatmentBMPs { get; set; } = new List<WaterQualityManagementPlanVerifyTreatmentBMP>();
}
