using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// Read gate for project-routed endpoints, mirroring ProjectController.Get: an OCTA grant reviewer may read
    /// a project shared with the OCTA M2 Tier 2 program, and staff may read projects in their own jurisdictions.
    /// Unassigned is granted only so OCTA grant reviewers, who are often Unassigned, get as far as that check.
    /// </summary>
    public class ProjectViewFeature() : BaseAuthorizationAttribute([RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor, RoleEnum.Unassigned])
    {
        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue("projectID", out var idObj) || !int.TryParse(idObj?.ToString(), out var projectID))
            {
                return;
            }

            var project = dbContext.Projects.AsNoTracking()
                .Where(x => x.ProjectID == projectID)
                .Select(x => new { x.StormwaterJurisdictionID, x.ShareOCTAM2Tier2Scores })
                .SingleOrDefault();

            // Authorization filters run before EntityNotFoundAttribute — answer 404 here rather than throw.
            if (project == null)
            {
                context.Result = new NotFoundResult();
                return;
            }

            if (!CanView(user, project.StormwaterJurisdictionID, project.ShareOCTAM2Tier2Scores))
            {
                context.Result = new ForbidResult();
            }
        }

        public static bool CanView(Person? user, int stormwaterJurisdictionID, bool sharedWithOCTAM2Tier2)
        {
            if (user == null)
            {
                return false;
            }

            if (user.IsOCTAGrantReviewer && sharedWithOCTAM2Tier2)
            {
                return true;
            }

            return !user.IsAnonymousOrUnassigned() && user.IsAssignedToStormwaterJurisdiction(stormwaterJurisdictionID);
        }
    }
}
