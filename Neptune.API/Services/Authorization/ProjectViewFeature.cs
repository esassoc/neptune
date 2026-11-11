using System.Linq;
using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// Read gate for project-routed endpoints: staff assigned to the project's jurisdiction, or an OCTA grant
    /// reviewer on a project shared with the OCTA M2 Tier 2 program. The reviewer flag is independent of role,
    /// so Unassigned is granted here only to reach that check; an Unassigned non-reviewer is still refused.
    /// </summary>
    public class ProjectViewFeature() : JurisdictionScopedFeature([.. EditRoles, RoleEnum.Unassigned])
    {
        protected override string RouteKey => "projectID";

        protected override int? GetStormwaterJurisdictionID(NeptuneDbContext dbContext, int id) =>
            dbContext.Projects.AsNoTracking()
                .Where(x => x.ProjectID == id)
                .Select(x => (int?)x.StormwaterJurisdictionID)
                .SingleOrDefault();

        protected override bool IsAllowedOutsideJurisdiction(NeptuneDbContext dbContext, Person user, int id) =>
            user.IsOCTAGrantReviewer &&
            dbContext.Projects.AsNoTracking().Any(x => x.ProjectID == id && x.ShareOCTAM2Tier2Scores);
    }
}
