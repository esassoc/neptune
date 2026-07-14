using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// NPT-1109: entity-scoped edit gate for WQMP-routed endpoints — role check
    /// (Editor and up) plus a jurisdiction match against the routed WQMP. Mirrors
    /// <see cref="TreatmentBMPEditFeature"/>. Replaces [JurisdictionManageFeature] on the
    /// document CRUD and AI-extraction endpoints, which was (a) Manager-only, blocking
    /// JurisdictionEditors from supporting-data CRUD the JE/JM doctrine says they should
    /// have, and (b) jurisdiction-blind, letting any Manager modify any jurisdiction's
    /// WQMP documents.
    /// </summary>
    public class WaterQualityManagementPlanEditFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue("waterQualityManagementPlanID", out var idObj) || !int.TryParse(idObj?.ToString(), out var waterQualityManagementPlanID))
            {
                return;
            }

            var waterQualityManagementPlan = WaterQualityManagementPlans.GetByIDForFeatureContextCheck(dbContext, waterQualityManagementPlanID);

            // Authorization filters run before EntityNotFoundAttribute — translate a
            // nonexistent WQMP into the 404 that attribute would have produced, rather
            // than throwing (which would 500).
            if (waterQualityManagementPlan == null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (user == null || user.IsAnonymousOrUnassigned())
            {
                context.Result = new ForbidResult();
                return;
            }

            if (!user.IsAssignedToStormwaterJurisdiction(waterQualityManagementPlan.StormwaterJurisdictionID))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
