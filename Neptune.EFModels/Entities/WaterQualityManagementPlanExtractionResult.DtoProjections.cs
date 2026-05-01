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
        WaterQualityManagementPlanName = x.WaterQualityManagementPlan.WaterQualityManagementPlanName,
        ExtractionResultJson = x.ExtractionResultJson,
        ExtractedAt = x.ExtractedAt,
        FileResourceGuid = x.WaterQualityManagementPlanDocument.FileResource.FileResourceGUID.ToString(),
    };
}
