using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class LoadGeneratingUnitExtensionMethods
    {
        public static LoadGeneratingUnitDto AsDto(this LoadGeneratingUnit entity)
        {
            return new LoadGeneratingUnitDto
            {
                LoadGeneratingUnitID = entity.LoadGeneratingUnitID,
                ModelBasinID = entity.ModelBasinID,
                ModelBasinKey = entity.ModelBasin?.ModelBasinKey,
                RegionalSubbasinID = entity.RegionalSubbasinID,
                RegionalSubbasinName = entity.RegionalSubbasin?.GetDisplayName(),
                DelineationID = entity.DelineationID,
                TreatmentBMPID = entity.Delineation?.TreatmentBMPID,
                TreatmentBMPName = entity.Delineation?.TreatmentBMP?.TreatmentBMPName,
                WaterQualityManagementPlanID = entity.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = entity.WaterQualityManagementPlan?.WaterQualityManagementPlanName,
                IsEmptyResponseFromHRUService = entity.IsEmptyResponseFromHRUService,
                DateHRURequested = entity.DateHRURequested,
                HRULog = entity.HRULog?.AsDto(),
                Area = entity.LoadGeneratingUnitGeometry.Area * Constants.SquareMetersToAcres
            };
        }

        public static LoadGeneratingUnitGridDto AsGridDto(this vLoadGeneratingUnit entity)
        {
            return new LoadGeneratingUnitGridDto
            {
                LoadGeneratingUnitID = entity.LoadGeneratingUnitID,
                ModelBasinID = entity.ModelBasinID,
                ModelBasinKey = entity.ModelBasinKey,
                RegionalSubbasinID = entity.RegionalSubbasinID,
                RegionalSubbasinName = entity.RegionalSubbasinName,
                TreatmentBMPID = entity.TreatmentBMPID,
                TreatmentBMPName = entity.TreatmentBMPName,
                WaterQualityManagementPlanID = entity.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = entity.WaterQualityManagementPlanName,
                IsEmptyResponseFromHRUService = entity.IsEmptyResponseFromHRUService ?? false,
                DateHRURequested = entity.DateHRURequested,
            };
        }
    }
}
