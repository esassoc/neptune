using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class ImpersonateUserFeature : BaseAuthorizationAttribute
    {
        // Impersonation start/stop must authorize the REAL (authenticated) admin, even while
        // they are wearing a low-privilege impersonated identity.
        protected override bool EvaluateAuthenticatedUserOnly => true;

        public ImpersonateUserFeature() : base(new[] { RoleEnum.Admin, RoleEnum.SitkaAdmin })
        {
        }
    }
}
