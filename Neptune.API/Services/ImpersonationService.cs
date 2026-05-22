using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.Helpers;

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
            var globalID = httpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.Sub)?.Value;
            // People.GetByGlobalID is AsNoTracking — fine for reads but the SaveChangesAsync
            // below needs a tracked entity. Use FindAsync against the looked-up PersonID to
            // get a tracked instance for mutation.
            var originalUser = People.GetByGlobalID(dbContext, globalID);

            if (environment.IsProduction() || originalUser == null)
            {
                return originalUser?.AsDto();
            }

            var tracked = await dbContext.People.FindAsync(originalUser.PersonID);
            tracked.ImpersonatedPersonID = targetPersonID;
            await dbContext.SaveChangesAsync();

            var target = People.GetByID(dbContext, targetPersonID);
            return target?.AsDto();
        }

        public async Task<PersonDto?> StopImpersonationAsync(NeptuneDbContext dbContext, HttpContext httpContext)
        {
            var globalID = httpContext.User.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.Sub)?.Value;
            var originalUser = People.GetByGlobalID(dbContext, globalID);

            if (originalUser == null)
            {
                return null;
            }

            var tracked = await dbContext.People.FindAsync(originalUser.PersonID);
            tracked.ImpersonatedPersonID = null;
            await dbContext.SaveChangesAsync();

            return People.GetByID(dbContext, originalUser.PersonID)?.AsDto();
        }
    }
}
