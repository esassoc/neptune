using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization;

// Staff only. Unassigned is what a self sign-up gets, so it is deliberately not here: endpoints an
// Unassigned user needs (their own profile, OCTA grant review) use [LoggedInUnclassifiedFeature] with an
// in-method check, or [ProjectViewFeature].
public class UserViewFeature : BaseAuthorizationAttribute
{
    public UserViewFeature() : base(new []{RoleEnum.Admin, RoleEnum.SitkaAdmin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor})
    {
    }
}