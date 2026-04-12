using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class vWaterQualityManagementPlanDetailedExtensionMethods
    {
        public static WaterQualityManagementPlanGridDto AsGridDto(this vWaterQualityManagementPlanDetailed entity)
        {
            return new WaterQualityManagementPlanGridDto
            {
                WaterQualityManagementPlanID = entity.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = entity.WaterQualityManagementPlanName,
                StormwaterJurisdictionID = entity.StormwaterJurisdictionID,
                StormwaterJurisdictionName = entity.StormwaterJurisdictionName,
                WaterQualityManagementPlanPriorityDisplayName = entity.WaterQualityManagementPlanPriorityDisplayName,
                WaterQualityManagementPlanStatusDisplayName = entity.WaterQualityManagementPlanStatusDisplayName,
                WaterQualityManagementPlanDevelopmentTypeDisplayName = entity.WaterQualityManagementPlanDevelopmentTypeDisplayName,
                WaterQualityManagementPlanLandUseDisplayName = entity.WaterQualityManagementPlanLandUseDisplayName,
                WaterQualityManagementPlanPermitTermDisplayName = entity.WaterQualityManagementPlanPermitTermDisplayName,
                ApprovalDate = entity.ApprovalDate,
                DateOfConstruction = entity.DateOfConstruction,
                HydromodificationAppliesTypeDisplayName = entity.HydromodificationAppliesTypeDisplayName,
                HydrologicSubareaName = entity.HydrologicSubareaName,
                MaintenanceContactOrganization = entity.MaintenanceContactOrganization,
                MaintenanceContactName = entity.MaintenanceContactName,
                MaintenanceContactPhone = entity.MaintenanceContactPhone,
                MaintenanceContactAddress = string.Join(" ",
                    new List<string>
                    {
                        entity.MaintenanceContactAddress1,
                        entity.MaintenanceContactAddress2,
                        entity.MaintenanceContactCity,
                        entity.MaintenanceContactState,
                        entity.MaintenanceContactZip
                    }.Where(x => !string.IsNullOrWhiteSpace(x))),
                TreatmentBMPCount = entity.TreatmentBMPCount,
                QuickBMPCount = entity.QuickBMPCount,
                WaterQualityManagementPlanModelingApproachDisplayName = entity.WaterQualityManagementPlanModelingApproachDisplayName,
                DocumentCount = entity.DocumentCount,
                HasRequiredDocuments = entity.HasRequiredDocuments,
                RecordNumber = entity.RecordNumber,
                RecordedWQMPAreaInAcres = entity.RecordedWQMPAreaInAcres,
                CalculatedWQMPAcreage = entity.CalculatedWQMPAcreage,
                AssociatedAPNs = entity.AssociatedAPNs,
                VerificationDate = entity.VerificationDate,
                TrashCaptureStatusTypeDisplayName = entity.TrashCaptureStatusTypeDisplayName,
                TrashCaptureEffectiveness = entity.TrashCaptureEffectiveness,
                HasBoundary = entity.HasBoundary ?? false,
                IsDraft = entity.WaterQualityManagementPlanStatusID == (int)WaterQualityManagementPlanStatusEnum.Draft
            };
        }
    }
}
