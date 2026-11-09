using Neptune.EFModels.Entities;

namespace Neptune.API.Services.Authorization
{
    /// <summary>
    /// NPT-1145: entity-scoped edit gate for land-use-block-routed endpoints — Editor and up, plus a
    /// jurisdiction match against the routed land use block.
    /// </summary>
    public class LandUseBlockEditFeature() : JurisdictionScopedFeature(EditRoles)
    {
        protected override string RouteKey => "landUseBlockID";

        protected override int? GetStormwaterJurisdictionID(NeptuneDbContext dbContext, int id) =>
            LandUseBlocks.GetStormwaterJurisdictionIDForFeatureContextCheck(dbContext, id);
    }
}
