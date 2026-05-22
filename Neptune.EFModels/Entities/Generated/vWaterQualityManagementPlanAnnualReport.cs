using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

[Keyless]
public partial class vWaterQualityManagementPlanAnnualReport
{
    public int WaterQualityManagementPlanID { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanName { get; set; }

    public int? WaterQualityManagementPlanStatusID { get; set; }

    public int StormwaterJurisdictionID { get; set; }

    public int StormwaterJurisdictionPublicWQMPVisibilityTypeID { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovalDate { get; set; }

    public int WaterQualityManagementPlanVerifyID { get; set; }

    public int? WaterQualityManagementPlanVerifyStatusID { get; set; }

    public DateOnly WaterQualityManagementPlanVerifyVerificationDate { get; set; }

    [StringLength(1000)]
    [Unicode(false)]
    public string? EnforcementOrFollowupActions { get; set; }

    public int? WaterQualityManagementPlanVerifyTreatmentBMPCount { get; set; }

    public int? WaterQualityManagementPlanVerifyTreatmentBMPIsAdequateCount { get; set; }

    public int? WaterQualityManagementPlanVerifyTreatmentBMPIsDeficientCount { get; set; }

    [StringLength(8000)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanVerifyTreatmentBMPNotes { get; set; }

    public int? WaterQualityManagementPlanVerifyQuickBMPCount { get; set; }

    public int? WaterQualityManagementPlanVerifyQuickBMPIsAdequateCount { get; set; }

    public int? WaterQualityManagementPlanVerifyQuickBMPIsDeficient { get; set; }

    [StringLength(8000)]
    [Unicode(false)]
    public string? WaterQualityManagementPlanVerifyQuickBMPNotes { get; set; }
}
