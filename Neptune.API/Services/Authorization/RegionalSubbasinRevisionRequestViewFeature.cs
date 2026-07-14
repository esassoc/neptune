using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class RegionalSubbasinRevisionRequestViewFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue("regionalSubbasinRevisionRequestID", out var idObj) || !int.TryParse(idObj?.ToString(), out var regionalSubbasinRevisionRequestID))
            {
                return;
            }

            // Null-tolerant fetches: authorization filters run before EntityNotFoundAttribute,
            // so a throwing lookup surfaces a bogus route id as a 500 instead of a 404.
            var revisionRequest = RegionalSubbasinRevisionRequests.GetByIDForFeatureContextCheck(dbContext, regionalSubbasinRevisionRequestID);
            var treatmentBMP = revisionRequest == null ? null : TreatmentBMPs.GetByIDForFeatureContextCheck(dbContext, revisionRequest.TreatmentBMPID);
            if (revisionRequest == null || treatmentBMP == null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (user == null || user.IsAnonymousOrUnassigned())
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!user.IsAssignedToStormwaterJurisdiction(treatmentBMP.StormwaterJurisdictionID))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
