using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    // Same role list as ImpersonateUserFeature — pre-flight role check operates on the
    // authenticated (real) user, which is exactly the identity we need to authorize the
    // stop action. OnAuthorizationCore tightens to "must currently be impersonating."
    public class StopImpersonationFeature : BaseAuthorizationAttribute
    {
        public StopImpersonationFeature() : base(new[] { RoleEnum.Admin, RoleEnum.SitkaAdmin })
        {
        }

        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? person)
        {
            if (person?.ImpersonatedPersonID == null)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
            }
        }
    }
}
