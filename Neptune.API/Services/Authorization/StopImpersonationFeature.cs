using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    // Always-allow at the role layer (the role-check pre-flight uses the authenticated user,
    // not the impersonated one, so we can't gate via the standard role list). The actual
    // gate happens in OnAuthorizationCore: 403 unless the calling JWT belongs to an
    // Admin/SitkaAdmin AND they currently have ImpersonatedPersonID set.
    public class StopImpersonationFeature : BaseAuthorizationAttribute
    {
        public StopImpersonationFeature() : base(new RoleEnum[] { })
        {
        }

        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? person)
        {
            var isAdmin = person?.RoleID == (int)RoleEnum.Admin || person?.RoleID == (int)RoleEnum.SitkaAdmin;
            var isImpersonating = person?.ImpersonatedPersonID != null;

            if (!isAdmin || !isImpersonating)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
            }
        }
    }
}
