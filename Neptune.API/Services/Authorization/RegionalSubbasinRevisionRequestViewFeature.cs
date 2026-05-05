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

            var revisionRequest = RegionalSubbasinRevisionRequests.GetByID(dbContext, regionalSubbasinRevisionRequestID);
            var treatmentBMP = TreatmentBMPs.GetByIDForFeatureContextCheck(dbContext, revisionRequest.TreatmentBMPID);

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
