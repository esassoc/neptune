using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanVerifyUpsertDto
{
    [Required]
    public int WaterQualityManagementPlanVerifyTypeID { get; set; }
    [Required]
    public int WaterQualityManagementPlanVisitStatusID { get; set; }
    [Required]
    public DateTime VerificationDate { get; set; }
    public int? WaterQualityManagementPlanVerifyStatusID { get; set; }
    [MaxLength(1000)]
    public string SourceControlCondition { get; set; }
    [MaxLength(1000)]
    public string EnforcementOrFollowupActions { get; set; }
    public bool IsDraft { get; set; } = true;
    public List<VerifyTreatmentBMPUpsertDto> TreatmentBMPs { get; set; } = new();
    public List<VerifyQuickBMPUpsertDto> QuickBMPs { get; set; } = new();
    public List<VerifySourceControlBMPUpsertDto> SourceControlBMPs { get; set; } = new();
}

public class VerifyTreatmentBMPUpsertDto
{
    public int TreatmentBMPID { get; set; }
    public bool? IsAdequate { get; set; }
    [MaxLength(500)]
    public string WaterQualityManagementPlanVerifyTreatmentBMPNote { get; set; }
}

public class VerifyQuickBMPUpsertDto
{
    public int QuickBMPID { get; set; }
    public bool? IsAdequate { get; set; }
    [MaxLength(500)]
    public string WaterQualityManagementPlanVerifyQuickBMPNote { get; set; }
}

public class VerifySourceControlBMPUpsertDto
{
    public int SourceControlBMPID { get; set; }
    [MaxLength(1000)]
    public string WaterQualityManagementPlanSourceControlCondition { get; set; }
}
