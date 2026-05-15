using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// NPT-984: Manager-only authorization. Tighter than JurisdictionEditFeature — excludes
    /// JurisdictionEditor. Mirrors the frontend's
    /// AuthenticationService.doesCurrentUserHaveJurisdictionManagePermission() role set.
    /// Used to gate destructive / attestation actions on Field Visits and Maintenance Records
    /// (Delete, Verify, Mark Provisional, Return to Edit) so only Jurisdiction Managers (plus
    /// Admin / SitkaAdmin) can call them.
    /// </summary>
    public class JurisdictionManageFeature : BaseAuthorizationAttribute
    {
        public JurisdictionManageFeature() : base(new[] { RoleEnum.JurisdictionManager, RoleEnum.Admin, RoleEnum.SitkaAdmin })
        {
        }
    }
}
