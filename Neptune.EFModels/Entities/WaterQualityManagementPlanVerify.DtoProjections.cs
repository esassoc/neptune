using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanVerifyProjections
{
    public static readonly Expression<Func<WaterQualityManagementPlanVerify, WaterQualityManagementPlanVerifyGridDto>> AsGridDto = x => new WaterQualityManagementPlanVerifyGridDto
    {
        WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
        VerificationDate = x.VerificationDate,
        LastEditedDate = x.LastEditedDate,
        LastEditedByPersonFullName = x.LastEditedByPerson.FirstName + " " + x.LastEditedByPerson.LastName,
        WaterQualityManagementPlanVerifyTypeID = x.WaterQualityManagementPlanVerifyTypeID,
        WaterQualityManagementPlanVisitStatusID = x.WaterQualityManagementPlanVisitStatusID,
        WaterQualityManagementPlanVerifyStatusID = x.WaterQualityManagementPlanVerifyStatusID,
        IsDraft = x.IsDraft,
    };

    public static readonly Expression<Func<WaterQualityManagementPlanVerify, WaterQualityManagementPlanVerifyIndexGridDto>> AsIndexGridDto = x => new WaterQualityManagementPlanVerifyIndexGridDto
    {
        WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
        WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
        WaterQualityManagementPlanName = x.WaterQualityManagementPlan.WaterQualityManagementPlanName,
        StormwaterJurisdictionID = x.WaterQualityManagementPlan.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.WaterQualityManagementPlan.StormwaterJurisdiction.Organization.OrganizationName,
        VerificationDate = x.VerificationDate,
        LastEditedDate = x.LastEditedDate,
        LastEditedByPersonFullName = x.LastEditedByPerson.FirstName + " " + x.LastEditedByPerson.LastName,
        WaterQualityManagementPlanVerifyTypeID = x.WaterQualityManagementPlanVerifyTypeID,
        WaterQualityManagementPlanVisitStatusID = x.WaterQualityManagementPlanVisitStatusID,
        WaterQualityManagementPlanVerifyStatusID = x.WaterQualityManagementPlanVerifyStatusID,
        SourceControlCondition = x.SourceControlCondition,
        EnforcementOrFollowupActions = x.EnforcementOrFollowupActions,
        IsDraft = x.IsDraft,
    };

    public static readonly Expression<Func<WaterQualityManagementPlanVerify, WaterQualityManagementPlanVerifyDetailDto>> AsDetailDto = x => new WaterQualityManagementPlanVerifyDetailDto
    {
        WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
        WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
        WaterQualityManagementPlanVerifyTypeID = x.WaterQualityManagementPlanVerifyTypeID,
        WaterQualityManagementPlanVisitStatusID = x.WaterQualityManagementPlanVisitStatusID,
        WaterQualityManagementPlanVerifyStatusID = x.WaterQualityManagementPlanVerifyStatusID,
        VerificationDate = x.VerificationDate,
        LastEditedDate = x.LastEditedDate,
        LastEditedByPersonFullName = x.LastEditedByPerson.FirstName + " " + x.LastEditedByPerson.LastName,
        SourceControlCondition = x.SourceControlCondition,
        EnforcementOrFollowupActions = x.EnforcementOrFollowupActions,
        IsDraft = x.IsDraft,
        FileResourceGUID = x.FileResource != null ? x.FileResource.FileResourceGUID.ToString() : null,
        TreatmentBMPs = x.WaterQualityManagementPlanVerifyTreatmentBMPs.Select(y => new WaterQualityManagementPlanVerifyTreatmentBMPSimpleDto
        {
            WaterQualityManagementPlanVerifyTreatmentBMPID = y.WaterQualityManagementPlanVerifyTreatmentBMPID,
            WaterQualityManagementPlanVerifyID = y.WaterQualityManagementPlanVerifyID,
            TreatmentBMPID = y.TreatmentBMPID,
            IsAdequate = y.IsAdequate,
            WaterQualityManagementPlanVerifyTreatmentBMPNote = y.WaterQualityManagementPlanVerifyTreatmentBMPNote,
            TreatmentBMPName = y.TreatmentBMP.TreatmentBMPName,
            TreatmentBMPType = y.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName,
        }).ToList(),
        QuickBMPs = x.WaterQualityManagementPlanVerifyQuickBMPs.Select(y => new WaterQualityManagementPlanVerifyQuickBMPDto
        {
            WaterQualityManagementPlanVerifyQuickBMPID = y.WaterQualityManagementPlanVerifyQuickBMPID,
            WaterQualityManagementPlanVerifyID = y.WaterQualityManagementPlanVerifyID,
            QuickBMPID = y.QuickBMPID,
            IsAdequate = y.IsAdequate,
            WaterQualityManagementPlanVerifyQuickBMPNote = y.WaterQualityManagementPlanVerifyQuickBMPNote,
            QuickBMPName = y.QuickBMP.QuickBMPName,
            TreatmentBMPType = y.QuickBMP.TreatmentBMPType.TreatmentBMPTypeName,
        }).ToList(),
        SourceControlBMPs = x.WaterQualityManagementPlanVerifySourceControlBMPs.Select(y => new VerifySourceControlBMPDetailDto
        {
            WaterQualityManagementPlanVerifySourceControlBMPID = y.WaterQualityManagementPlanVerifySourceControlBMPID,
            SourceControlBMPID = y.SourceControlBMPID,
            SourceControlBMPAttributeName = y.SourceControlBMP.SourceControlBMPAttribute.SourceControlBMPAttributeName,
            SourceControlBMPAttributeCategoryID = y.SourceControlBMP.SourceControlBMPAttribute.SourceControlBMPAttributeCategoryID,
            WaterQualityManagementPlanSourceControlCondition = y.WaterQualityManagementPlanSourceControlCondition,
        }).ToList(),
    };
}
