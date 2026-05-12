using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    public class TreatmentBMPEditFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue("treatmentBMPID", out var idObj) || !int.TryParse(idObj?.ToString(), out var treatmentBMPID))
            {
                return;
            }

            var treatmentBMP = TreatmentBMPs.GetByIDForFeatureContextCheck(dbContext, treatmentBMPID);

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
