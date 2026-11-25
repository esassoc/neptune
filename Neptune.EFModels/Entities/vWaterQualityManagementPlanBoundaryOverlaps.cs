using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace Neptune.EFModels.Entities;

public static class vWaterQualityManagementPlanBoundaryOverlaps
{
    /// <summary>
    /// The WQMP Boundary Overlap Report (NPT-1155): each pair of overlapping WQMP boundaries where at
    /// least one of the two plans is in a jurisdiction the person can see. The view decides which
    /// boundaries are compared and what counts as an overlap; whether a plan treats anything is the
    /// solve's own test (<see cref="QuickBMPs.IsSentToModel"/>), so the report and the model agree.
    /// </summary>
    public static async Task<List<WaterQualityManagementPlanBoundaryOverlapGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext, Person currentPerson)
    {
        // every pair is loaded, whoever is asking: a pair outside the person's jurisdictions can still
        // be the one that shows a third, treating plan covers the ground two of their plans share
        var allOverlaps = await dbContext.vWaterQualityManagementPlanBoundaryOverlaps.AsNoTracking().ToListAsync();

        var waterQualityManagementPlanIDs = allOverlaps.Select(x => x.WaterQualityManagementPlanID)
            .Union(allOverlaps.Select(x => x.OverlappingWaterQualityManagementPlanID))
            .ToList();
        var waterQualityManagementPlanIDsThatTreat = await WaterQualityManagementPlans.ListIDsThatTreatAsync(dbContext, waterQualityManagementPlanIDs);

        var overlaps = allOverlaps;
        // administrators see every pair; the viewable-jurisdiction list is built from user assignments,
        // so it would hide a jurisdiction nobody is assigned to
        if (!currentPerson.IsAdministrator())
        {
            var viewableJurisdictionIDs = StormwaterJurisdictionPeople
                .ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, currentPerson).ToHashSet();
            overlaps = allOverlaps
                .Where(x => viewableJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                            || viewableJurisdictionIDs.Contains(x.OverlappingStormwaterJurisdictionID))
                .ToList();
        }

        var pairsWhoseSharedGroundGoesToAnotherPlan = await ListPairsWhoseSharedGroundGoesToAnotherPlanAsync(dbContext, overlaps, allOverlaps, waterQualityManagementPlanIDsThatTreat);

        return overlaps
            .Select(x => AsGridDto(x,
                waterQualityManagementPlanIDsThatTreat.Contains(x.WaterQualityManagementPlanID),
                waterQualityManagementPlanIDsThatTreat.Contains(x.OverlappingWaterQualityManagementPlanID),
                pairsWhoseSharedGroundGoesToAnotherPlan.Contains((x.WaterQualityManagementPlanID, x.OverlappingWaterQualityManagementPlanID))))
            .OrderByDescending(x => x.AreaOfOverlapInAcres)
            .ToList();
    }

    /// <summary>
    /// Pairs of plans that both treat nothing and whose shared ground is covered by other plans that do
    /// treat. The overlay cuts treating plans' ground out of both (OverlayEngine.YieldToTreatingPlans),
    /// so nothing is left to count twice. Boundaries are only loaded when such a pair might exist.
    /// </summary>
    private static async Task<HashSet<(int, int)>> ListPairsWhoseSharedGroundGoesToAnotherPlanAsync(NeptuneDbContext dbContext,
        List<vWaterQualityManagementPlanBoundaryOverlap> overlaps, List<vWaterQualityManagementPlanBoundaryOverlap> allOverlaps, HashSet<int> waterQualityManagementPlanIDsThatTreat)
    {
        var treatingNeighborIDsByPlanID = allOverlaps
            .SelectMany(x => new[]
            {
                (PlanID: x.WaterQualityManagementPlanID, NeighborID: x.OverlappingWaterQualityManagementPlanID),
                (PlanID: x.OverlappingWaterQualityManagementPlanID, NeighborID: x.WaterQualityManagementPlanID),
            })
            .Where(x => waterQualityManagementPlanIDsThatTreat.Contains(x.NeighborID))
            .ToLookup(x => x.PlanID, x => x.NeighborID);

        var candidates = overlaps
            .Where(x => !waterQualityManagementPlanIDsThatTreat.Contains(x.WaterQualityManagementPlanID)
                        && !waterQualityManagementPlanIDsThatTreat.Contains(x.OverlappingWaterQualityManagementPlanID))
            .Select(x => (Overlap: x, TreatingNeighborIDs: treatingNeighborIDsByPlanID[x.WaterQualityManagementPlanID]
                .Intersect(treatingNeighborIDsByPlanID[x.OverlappingWaterQualityManagementPlanID]).ToList()))
            .Where(x => x.TreatingNeighborIDs.Any())
            .ToList();

        var pairs = new HashSet<(int, int)>();
        if (!candidates.Any())
        {
            return pairs;
        }

        var boundaryIDs = candidates
            .SelectMany(x => x.TreatingNeighborIDs.Append(x.Overlap.WaterQualityManagementPlanID).Append(x.Overlap.OverlappingWaterQualityManagementPlanID))
            .Distinct().ToList();
        var boundaries = await dbContext.WaterQualityManagementPlanBoundaries.AsNoTracking()
            .Where(x => boundaryIDs.Contains(x.WaterQualityManagementPlanID) && x.GeometryNative != null)
            .ToDictionaryAsync(x => x.WaterQualityManagementPlanID, x => x.GeometryNative!);

        foreach (var (overlap, treatingNeighborIDs) in candidates)
        {
            if (!boundaries.TryGetValue(overlap.WaterQualityManagementPlanID, out var boundary) ||
                !boundaries.TryGetValue(overlap.OverlappingWaterQualityManagementPlanID, out var overlappingBoundary))
            {
                continue;
            }

            var treatingBoundaries = treatingNeighborIDs.Where(boundaries.ContainsKey).Select(x => boundaries[x]);
            if (SharedAreaLeftAfterTreatingPlans(boundary, overlappingBoundary, treatingBoundaries) <= OverlapFloorInSquareMeters)
            {
                pairs.Add((overlap.WaterQualityManagementPlanID, overlap.OverlappingWaterQualityManagementPlanID));
            }
        }

        return pairs;
    }

    /// <summary>The floor the view uses for an overlap worth listing; a remainder under it is a sliver.</summary>
    public const double OverlapFloorInSquareMeters = 200;

    /// <summary>
    /// The area two boundaries share once the ground of the treating plans is taken out, as the overlay does.
    /// </summary>
    public static double SharedAreaLeftAfterTreatingPlans(Geometry boundary, Geometry overlappingBoundary, IEnumerable<Geometry> treatingBoundaries)
    {
        var shared = GeometryFixer.Fix(boundary).Intersection(GeometryFixer.Fix(overlappingBoundary));
        foreach (var treatingBoundary in treatingBoundaries)
        {
            if (shared.IsEmpty)
            {
                break;
            }

            shared = shared.Difference(GeometryFixer.Fix(treatingBoundary));
        }

        return shared.Area;
    }

    public static WaterQualityManagementPlanBoundaryOverlapGridDto AsGridDto(vWaterQualityManagementPlanBoundaryOverlap overlap, bool treatsRunoff, bool overlappingTreatsRunoff, bool sharedGroundGoesToAnotherPlan = false)
    {
        return new WaterQualityManagementPlanBoundaryOverlapGridDto
        {
            WaterQualityManagementPlanID = overlap.WaterQualityManagementPlanID,
            WaterQualityManagementPlanName = overlap.WaterQualityManagementPlanName,
            StormwaterJurisdictionID = overlap.StormwaterJurisdictionID,
            StormwaterJurisdictionName = overlap.StormwaterJurisdictionName,
            TreatsRunoff = treatsRunoff,
            OverlappingWaterQualityManagementPlanID = overlap.OverlappingWaterQualityManagementPlanID,
            OverlappingWaterQualityManagementPlanName = overlap.OverlappingWaterQualityManagementPlanName,
            OverlappingStormwaterJurisdictionID = overlap.OverlappingStormwaterJurisdictionID,
            OverlappingStormwaterJurisdictionName = overlap.OverlappingStormwaterJurisdictionName,
            OverlappingTreatsRunoff = overlappingTreatsRunoff,
            AreaOfOverlapInAcres = (overlap.OverlapAreaInSquareMeters ?? 0) * Constants.SquareMetersToAcres,
            // the overlay (OverlayEngine.YieldToTreatingPlans) resolves a pair where exactly one plan treats,
            // and a pair of non-treating plans whose shared ground other treating plans cover
            IsCountedTwice = treatsRunoff == overlappingTreatsRunoff && !sharedGroundGoesToAnotherPlan,
        };
    }
}
