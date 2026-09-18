using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class LandUseBlocks
{
    /// <summary>
    /// NPT-1077: project a validated <see cref="LandUseBlockStaging"/> row into the
    /// production <see cref="LandUseBlock"/> entity. Income values are copied verbatim — the
    /// legacy job's branching on <c>LandUseForTGR</c> that zeroed out non-RESIDENTIAL/RETAIL rows
    /// has been removed. The caller (background job) must have already validated lookup-name
    /// matches via <see cref="LandUseBlockStagings.ValidateStagings"/>; lookup misses here throw
    /// because they'd indicate a bypass of the validation gate.
    /// </summary>
    public static LandUseBlock FromStaging(LandUseBlockStaging staging)
    {
        return new LandUseBlock
        {
            StormwaterJurisdictionID = staging.StormwaterJurisdictionID,
            PriorityLandUseTypeID = PriorityLandUseType.All
                .Single(x => string.Equals(x.PriorityLandUseTypeDisplayName, staging.PriorityLandUseType, StringComparison.InvariantCultureIgnoreCase))
                .PriorityLandUseTypeID,
            PermitTypeID = PermitType.All
                .Single(x => string.Equals(x.PermitTypeDisplayName, staging.PermitType, StringComparison.InvariantCultureIgnoreCase))
                .PermitTypeID,
            LandUseDescription = staging.LandUseDescription,
            TrashGenerationRate = staging.TrashGenerationRate,
            LandUseForTGR = staging.LandUseForTGR,
            // NPT-1077: preserve both income values verbatim. The legacy job zeroed these based on
            // LandUseForTGR — display-only fields shouldn't be overwritten.
            MedianHouseholdIncomeResidential = staging.MedianHouseholdIncomeResidential,
            MedianHouseholdIncomeRetail = staging.MedianHouseholdIncomeRetail,
            LandUseBlockGeometry = staging.Geometry,
            LandUseBlockGeometry4326 = staging.Geometry.ProjectTo4326(),
        };
    }

    public static LandUseBlock GetByIDWithChangeTracking(NeptuneDbContext dbContext, int landUseBlockID)
    {
        var landUseBlock = dbContext.LandUseBlocks
            .SingleOrDefault(x => x.LandUseBlockID == landUseBlockID);
        Check.RequireNotNull(landUseBlock, $"Land Use Block with ID {landUseBlockID} not found!");
        return landUseBlock;
    }
    public static List<LandUseBlockGridDto> List(NeptuneDbContext dbContext)
    {
        var landUseBlocks = dbContext.LandUseBlocks.AsNoTracking()
            .Include(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
            .Include(x => x.TrashGeneratingUnits)
            .Select(x => x.AsGridDto()).ToList();
        return landUseBlocks;
    }

    public static bool JurisdictionHasLandUseBlocks(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        return dbContext.LandUseBlocks.AsNoTracking()
            .Any(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
    }

    public static Geometry UnionAggregateByLandUseBlockIDs(NeptuneDbContext dbContext, IEnumerable<int> landUseBlockIDs, int stormwaterJurisdictionID)
    {
        // Scoped to the caller's jurisdiction so a client can't drive cross-jurisdiction unions
        // by posting arbitrary IDs. Trust boundary is enforced here, in the helper, so any
        // future caller is constrained without re-implementing the filter.
        return dbContext.LandUseBlocks.AsNoTracking()
            .Where(x => landUseBlockIDs.Contains(x.LandUseBlockID)
                        && x.StormwaterJurisdictionID == stormwaterJurisdictionID
                        && x.LandUseBlockGeometry != null)
            .Select(x => x.LandUseBlockGeometry).ToList()
            .UnionListGeometries();
    }

    public static IQueryable<LandUseBlock> GetIntersected(NeptuneDbContext dbContext, Geometry geometryToIntersect, int stormwaterJurisdictionID)
    {
        return dbContext.LandUseBlocks.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID
                        && x.LandUseBlockGeometry.Intersects(geometryToIntersect));
    }

    /// <summary>
    /// NPT-1128 rework: Land Use Blocks that genuinely overlap each OVTA Area, for the OVTA Area grid and GDB
    /// export. Reads dbo.vOnlandVisualTrashAssessmentAreaLandUseBlock, which does the spatial join (same
    /// jurisdiction, STIntersects, overlap area > 10 m^2 so shared-edge neighbours are excluded) with the spatial
    /// index hint the optimizer otherwise ignores. Deliberately not derived from TrashGeneratingUnit, which is
    /// filtered to Phase I MS4 blocks and only refreshed by the overlay pipeline.
    /// See docs/ovta-area-land-use-spatial-join.md.
    /// </summary>
    public static Dictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>> ListByOnlandVisualTrashAssessmentAreaID(
        NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDs)
    {
        var jurisdictionIDs = stormwaterJurisdictionIDs.Distinct().ToList();
        if (jurisdictionIDs.Count == 0)
        {
            return new Dictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>>();
        }

        return dbContext.vOnlandVisualTrashAssessmentAreaLandUseBlocks.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .Select(x => new { x.OnlandVisualTrashAssessmentAreaID, x.LandUseBlockID, x.PriorityLandUseTypeID })
            .ToList()
            .GroupBy(x => x.OnlandVisualTrashAssessmentAreaID)
            .ToDictionary(g => g.Key,
                g => g.Select(x => new OnlandVisualTrashAssessmentAreaLandUseBlock(x.LandUseBlockID, x.PriorityLandUseTypeID)).ToList());
    }

    public static async Task Update(NeptuneDbContext dbContext, LandUseBlock landUseBlock, LandUseBlockUpsertDto landUseBlockUpsertDto, int personID)
    {
        landUseBlock.PriorityLandUseTypeID = landUseBlockUpsertDto.PriorityLandUseTypeID;
        landUseBlock.TrashGenerationRate = landUseBlockUpsertDto.TrashGenerationRate;
        landUseBlock.LandUseDescription = landUseBlockUpsertDto.LandUseDescription;
        landUseBlock.MedianHouseholdIncomeResidential = landUseBlockUpsertDto.MedianHouseholdIncomeResidential;
        landUseBlock.MedianHouseholdIncomeRetail = landUseBlockUpsertDto.MedianHouseholdIncomeRetail;
        landUseBlock.PermitTypeID = landUseBlockUpsertDto.PermitTypeID;
        landUseBlock.UpdatePersonID = personID;
        landUseBlock.DateUpdated = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
}

/// <summary>One Land Use Block overlapping an OVTA Area (see <see cref="LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID"/>).</summary>
public record OnlandVisualTrashAssessmentAreaLandUseBlock(int LandUseBlockID, int? PriorityLandUseTypeID);
