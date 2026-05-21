using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class WaterQualityManagementPlanAnnualReportFeature : BaseAuthorizationAttribute
    {
        public WaterQualityManagementPlanAnnualReportFeature() : base(new[] { RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor })
        {
        }
    }
}
