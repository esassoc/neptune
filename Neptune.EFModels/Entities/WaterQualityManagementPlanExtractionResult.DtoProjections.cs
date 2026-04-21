using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanExtractionResultProjections
{
    public static readonly Expression<Func<WaterQualityManagementPlanExtractionResult, WaterQualityManagementPlanExtractionResultDto>> AsDto = x => new WaterQualityManagementPlanExtractionResultDto
    {
        WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
        WaterQualityManagementPlanDocumentID = x.WaterQualityManagementPlanDocumentID,
        StormwaterJurisdictionID = x.WaterQualityManagementPlan.StormwaterJurisdictionID,
        ExtractionResultJson = x.ExtractionResultJson,
        ExtractedAt = x.ExtractedAt,
        FileResourceGuid = x.WaterQualityManagementPlanDocument.FileResource.FileResourceGUID.ToString(),
        DraftOverlayJson = x.DraftOverlayJson,
        DraftUpdatedDate = x.DraftUpdatedDate,
        DraftUpdatedByFullName = x.DraftUpdatedByPerson != null ? x.DraftUpdatedByPerson.FirstName + " " + x.DraftUpdatedByPerson.LastName : null,
        ApprovedDate = x.ApprovedDate,
        ApprovedByFullName = x.ApprovedByPerson != null ? x.ApprovedByPerson.FirstName + " " + x.ApprovedByPerson.LastName : null,
    };
}
