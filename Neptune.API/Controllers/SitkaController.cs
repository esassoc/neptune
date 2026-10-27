using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    public abstract class SitkaController<T>(
        NeptuneDbContext dbContext,
        ILogger<T> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : ControllerBase
    {
        protected readonly NeptuneDbContext DbContext = dbContext;
        protected readonly ILogger<T> Logger = logger;
        protected readonly NeptuneConfiguration NeptuneConfiguration = neptuneConfiguration.Value;

        protected PersonDto CallingUser => UserContext.GetUserAsDtoFromHttpContext(DbContext, HttpContext);

        // NPT-1135: for endpoints that take the jurisdiction in the body or form, where a route-keyed
        // JurisdictionScopedFeature can't see it. Admin/SitkaAdmin pass; everyone else must be assigned.
        protected Task<bool> CallerIsAssignedToJurisdictionAsync(int stormwaterJurisdictionID)
        {
            return StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(DbContext, CallingUser, stormwaterJurisdictionID);
        }

        protected Task<bool> CallerIsAssignedToJurisdictionsAsync(IEnumerable<int> stormwaterJurisdictionIDs)
        {
            return StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionsAsync(DbContext, CallingUser, stormwaterJurisdictionIDs);
        }

        protected ActionResult RequireNotNullThrowNotFound(object theObject, string objectType, object objectID)
        {
            return ThrowNotFound(theObject, objectType, objectID, out var actionResult) ? actionResult : Ok(theObject);
        }

        protected bool ThrowNotFound(object theObject, string objectType, object objectID, out ActionResult actionResult)
        {
            if (theObject == null)
            {
                var notFoundMessage = $"{objectType} with ID {objectID} does not exist!";
                Logger.LogError(notFoundMessage);
                {
                    actionResult = NotFound(notFoundMessage);
                    return true;
                }
            }

            actionResult = null;
            return false;
        }
    }
}