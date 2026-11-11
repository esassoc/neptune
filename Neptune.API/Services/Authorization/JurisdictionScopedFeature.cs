using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// NPT-1129: shared base for entity-scoped gates — role check (from the granted roles) plus a
    /// jurisdiction match against the record named by <see cref="RouteKey"/>. Derived attributes
    /// differ only in the route key, the jurisdiction lookup and the role set.
    /// </summary>
    public abstract class JurisdictionScopedFeature(IEnumerable<RoleEnum> grantedRoles) : BaseAuthorizationAttribute(grantedRoles)
    {
        public static readonly RoleEnum[] EditRoles = [RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor];
        public static readonly RoleEnum[] ManageRoles = [RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager];

        protected abstract string RouteKey { get; }

        /// <summary>
        /// Returns the StormwaterJurisdictionID that owns the record, or null when the record doesn't exist.
        /// </summary>
        protected abstract int? GetStormwaterJurisdictionID(NeptuneDbContext dbContext, int id);

        protected override void OnAuthorizationCore(AuthorizationFilterContext context, NeptuneDbContext dbContext, Person? user)
        {
            if (!context.RouteData.Values.TryGetValue(RouteKey, out var idObj) || !int.TryParse(idObj?.ToString(), out var id))
            {
                return;
            }

            var result = Evaluate(dbContext, user, id);
            if (result != null)
            {
                context.Result = result;
            }
        }

        /// <summary>
        /// Resolves the record's jurisdiction and decides access. Returns null when allowed.
        /// Public so tests can exercise the decision without building an AuthorizationFilterContext.
        /// </summary>
        public IActionResult? Evaluate(NeptuneDbContext dbContext, Person? user, int id)
        {
            var stormwaterJurisdictionID = GetStormwaterJurisdictionID(dbContext, id);

            // Authorization filters run before EntityNotFoundAttribute — translate a
            // nonexistent record into the 404 that attribute would have produced, rather than
            // throwing (which surfaced as a 500).
            if (stormwaterJurisdictionID == null)
            {
                return new NotFoundResult();
            }

            if (user != null && IsAllowedOutsideJurisdiction(dbContext, user, id))
            {
                return null;
            }

            if (user == null || user.IsAnonymousOrUnassigned())
            {
                return new ForbidResult();
            }

            return user.IsAssignedToStormwaterJurisdiction(stormwaterJurisdictionID.Value) ? null : new ForbidResult();
        }

        /// <summary>
        /// Lets a derived gate admit a caller who isn't assigned to the record's jurisdiction (e.g. an OCTA
        /// grant reviewer on a shared project). Checked after the 404 and before the role/jurisdiction match.
        /// </summary>
        protected virtual bool IsAllowedOutsideJurisdiction(NeptuneDbContext dbContext, Person user, int id) => false;
    }
}
