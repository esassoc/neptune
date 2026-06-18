using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanDocumentExtensionMethods
{
    public static WaterQualityManagementPlanDocumentDto AsDto(this WaterQualityManagementPlanDocument doc)
    {
        return new WaterQualityManagementPlanDocumentDto
        {
            WaterQualityManagementPlanDocumentID = doc.WaterQualityManagementPlanDocumentID,
            WaterQualityManagementPlanID = doc.WaterQualityManagementPlanID,
            WaterQualityManagementPlanName = doc.WaterQualityManagementPlan.WaterQualityManagementPlanName,
            FileResource = doc.FileResource.AsDto(),
            DisplayName = doc.DisplayName,
            Description = doc.Description,
            UploadDate = doc.UploadDate,
            WaterQualityManagementPlanDocumentTypeID = doc.WaterQualityManagementPlanDocumentTypeID
        };
    }

    public static void UpdateFromUpsertDto(this WaterQualityManagementPlanDocument entity, WaterQualityManagementPlanDocumentUpsertDto dto)
    {
        entity.WaterQualityManagementPlanID = dto.WaterQualityManagementPlanID;
        entity.FileResourceID = dto.FileResourceID;
        entity.DisplayName = dto.DisplayName;
        entity.Description = dto.Description;
        entity.WaterQualityManagementPlanDocumentTypeID = dto.WaterQualityManagementPlanDocumentTypeID;
        entity.UploadDate = DateTime.UtcNow;
    }

    public static WaterQualityManagementPlanDocument AsEntity(this WaterQualityManagementPlanDocumentUpsertDto dto)
    {
        return new WaterQualityManagementPlanDocument
        {
            WaterQualityManagementPlanID = dto.WaterQualityManagementPlanID,
            FileResourceID = dto.FileResourceID,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            WaterQualityManagementPlanDocumentTypeID = dto.WaterQualityManagementPlanDocumentTypeID,
            UploadDate = DateTime.UtcNow
        };
    }
}
