using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class ImpersonateUserFeature : BaseAuthorizationAttribute
    {
        public ImpersonateUserFeature() : base(new[] { RoleEnum.Admin, RoleEnum.SitkaAdmin })
        {
        }
    }
}
