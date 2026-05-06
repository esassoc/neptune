using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class RegionalSubbasinRevisionRequestCloseFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue("regionalSubbasinRevisionRequestID", out var idObj) || !int.TryParse(idObj?.ToString(), out var regionalSubbasinRevisionRequestID))
            {
                return;
            }

            if (user == null || user.IsAnonymousOrUnassigned())
            {
                context.Result = new ForbidResult();
                return;
            }

            if (user.IsAdministrator())
            {
                return;
            }

            var revisionRequest = RegionalSubbasinRevisionRequests.GetByID(dbContext, regionalSubbasinRevisionRequestID);
            if (user.PersonID == revisionRequest.RequestPersonID)
            {
                return;
            }

            context.Result = new ForbidResult();
        }
    }
}
