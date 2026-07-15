using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    // API-style authorization filter for TreatmentBMP view permissions
    public class TreatmentBMPViewFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor
    ])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            // Get TreatmentBMPID from route
            if (!context.RouteData.Values.TryGetValue("treatmentBMPID", out var idObj) || !int.TryParse(idObj?.ToString(), out var treatmentBMPID))
            {
                // No context, allow (role-based only)
                return;
            }

            var treatmentBMP = TreatmentBMPs.GetByIDForFeatureContextCheck(dbContext, treatmentBMPID);

            // Authorization filters run before EntityNotFoundAttribute — translate a
            // nonexistent BMP into the 404 that attribute would have produced, rather than
            // throwing (which surfaced as a 500).
            if (treatmentBMP == null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (user.IsAnonymousOrUnassigned() &&
                treatmentBMP.StormwaterJurisdiction.StormwaterJurisdictionPublicBMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicBMPVisibilityTypeEnum.None)
            {
                context.Result = new ForbidResult();
                return;
            }

            // verified BMPs are available for unassigned/anonymous users and therefore all users
            if (treatmentBMP.InventoryIsVerified)
            {
                return; // Allow
            }

            var isAssignedToTreatmentBMP = user.IsAssignedToStormwaterJurisdiction(treatmentBMP.StormwaterJurisdictionID);
            if (!isAssignedToTreatmentBMP)
            {
                context.Result = new ForbidResult();
                return;
            }
            // Allow
        }
    }
}
