using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Services
{
    // Impersonation is non-production only. The Person.ImpersonatedPersonID column carries
    // the server-side session state; no Auth0 changes are required and the JWT bearer token
    // is unchanged across impersonation. Adapted from WADNR's ImpersonationService.
    public class ImpersonationService(IWebHostEnvironment environment)
    {
        public PersonDto GetEffectiveUser(NeptuneDbContext dbContext, PersonDto authenticatedUser)
        {
            if (environment.IsProduction() || authenticatedUser?.ImpersonatedPersonID == null)
            {
                return authenticatedUser;
            }

            var impersonatedUser = People.GetByID(dbContext, authenticatedUser.ImpersonatedPersonID.Value);
            return impersonatedUser?.AsDto() ?? authenticatedUser;
        }

        public async Task<PersonDto?> ImpersonateUserAsync(NeptuneDbContext dbContext, HttpContext httpContext, int targetPersonID)
        {
            // Use UserContext to resolve the authenticated user — handles both the standard
            // sub-claim lookup AND the client-credentials path, AND guards against null/empty
            // globalID matching multiple NULL-GlobalID rows.
            var authenticatedUser = UserContext.GetUserFromHttpContext(dbContext, httpContext);

            if (environment.IsProduction() || authenticatedUser == null)
            {
                return authenticatedUser?.AsDto();
            }

            // People.GetByGlobalID returns AsNoTracking — FindAsync gets a tracked entity for
            // mutation. Guard against the (unlikely) row-deleted-between-calls race.
            var tracked = await dbContext.People.FindAsync(authenticatedUser.PersonID);
            if (tracked == null)
            {
                return null;
            }
            tracked.ImpersonatedPersonID = targetPersonID;
            await dbContext.SaveChangesAsync();

            var target = People.GetByID(dbContext, targetPersonID);
            return target?.AsDto();
        }

        public async Task<PersonDto?> StopImpersonationAsync(NeptuneDbContext dbContext, HttpContext httpContext)
        {
            var authenticatedUser = UserContext.GetUserFromHttpContext(dbContext, httpContext);

            // Prod gate — no-op (return the authenticated user) without mutating state. Matches
            // the gates on GetEffectiveUser/ImpersonateUserAsync so prod behavior is uniformly
            // "impersonation does nothing." Server-side defense-in-depth even though the SPA
            // also hides the entry point in prod.
            if (environment.IsProduction() || authenticatedUser == null)
            {
                return authenticatedUser?.AsDto();
            }

            var tracked = await dbContext.People.FindAsync(authenticatedUser.PersonID);
            if (tracked == null)
            {
                return null;
            }
            tracked.ImpersonatedPersonID = null;
            await dbContext.SaveChangesAsync();

            return People.GetByID(dbContext, authenticatedUser.PersonID)?.AsDto();
        }
    }
}
