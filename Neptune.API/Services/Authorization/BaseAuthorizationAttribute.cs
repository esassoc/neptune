using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public abstract class BaseAuthorizationAttribute(IEnumerable<RoleEnum> grantedRoles)
        : AuthorizeAttribute, IAuthorizationFilter
    {
        public int Order { get; set; } = 0; // Default order, higher than EntityNotFoundAttribute

        // NPT-1104 rework: gates evaluate the EFFECTIVE (impersonated) user, so impersonating a
        // lower-privileged user faithfully exercises authorization. Previously the UI wore the
        // impersonated identity while every gate passed as the authenticated admin, making
        // authorization bugs invisible under impersonation. The impersonation start/stop features
        // override this to true — those actions must authorize the REAL admin even while wearing a
        // low-privilege identity (otherwise you couldn't stop impersonating). Impersonation no-ops
        // in production, so production authorization is unchanged.
        protected virtual bool EvaluateAuthenticatedUserOnly => false;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated)
            {
                return;
            }

            var dbContextService = context.HttpContext.RequestServices.GetService(typeof(NeptuneDbContext));
            if (dbContextService == null || !(dbContextService is NeptuneDbContext dbContext))
            {
                throw new ApplicationException(
                    "Could not find injected NeptuneDbRepository. OnAuthorization.cs needs your help!");
            }

            var person = UserContext.GetUserFromHttpContext(dbContext, context.HttpContext);

            if (!EvaluateAuthenticatedUserOnly)
            {
                var impersonationService = context.HttpContext.RequestServices.GetService(typeof(ImpersonationService)) as ImpersonationService;
                person = impersonationService?.GetEffectivePerson(dbContext, person) ?? person;
            }

            var isAuthorized = person != null && (grantedRoles.Any(x => (int)x == person.RoleID) || !grantedRoles.Any());
            if (!isAuthorized)
            {
                context.Result = new StatusCodeResult((int)System.Net.HttpStatusCode.Forbidden);
                return;
            }

            // Call extension point for entity/context logic
            OnAuthorizationCore(context, dbContext, person);
        }

        // Extension point for derived classes
        protected virtual void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? person)
        {
            // Default: do nothing
        }
    }
}
