namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanVerifyDetailDto
{
    public int WaterQualityManagementPlanVerifyID { get; set; }
    public int WaterQualityManagementPlanID { get; set; }
    public int WaterQualityManagementPlanVerifyTypeID { get; set; }
    public string WaterQualityManagementPlanVerifyTypeDisplayName { get; set; }
    public int WaterQualityManagementPlanVisitStatusID { get; set; }
    public string WaterQualityManagementPlanVisitStatusDisplayName { get; set; }
    public int? WaterQualityManagementPlanVerifyStatusID { get; set; }
    public string WaterQualityManagementPlanVerifyStatusDisplayName { get; set; }
    public DateOnly VerificationDate { get; set; }
    public DateTime LastEditedDate { get; set; }
    public string LastEditedByPersonFullName { get; set; }
    public string SourceControlCondition { get; set; }
    public string EnforcementOrFollowupActions { get; set; }
    public bool IsDraft { get; set; }
    public string FileResourceGUID { get; set; }

    public List<WaterQualityManagementPlanVerifyTreatmentBMPSimpleDto> TreatmentBMPs { get; set; } = new();
    public List<WaterQualityManagementPlanVerifyQuickBMPDto> QuickBMPs { get; set; } = new();
    public List<VerifySourceControlBMPDetailDto> SourceControlBMPs { get; set; } = new();
}

public class VerifySourceControlBMPDetailDto
{
    public int WaterQualityManagementPlanVerifySourceControlBMPID { get; set; }
    public int SourceControlBMPID { get; set; }
    public int SourceControlBMPAttributeCategoryID { get; set; }
    public string SourceControlBMPAttributeName { get; set; }
    public string SourceControlBMPAttributeCategoryName { get; set; }
    public string WaterQualityManagementPlanSourceControlCondition { get; set; }
}
