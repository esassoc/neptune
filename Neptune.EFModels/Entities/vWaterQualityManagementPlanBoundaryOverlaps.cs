using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;

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
        var viewableJurisdictionIDs = StormwaterJurisdictionPeople
            .ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, currentPerson).ToList();

        var overlaps = await dbContext.vWaterQualityManagementPlanBoundaryOverlaps.AsNoTracking()
            .Where(x => viewableJurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        || viewableJurisdictionIDs.Contains(x.OverlappingStormwaterJurisdictionID))
            .ToListAsync();

        var waterQualityManagementPlanIDs = overlaps.Select(x => x.WaterQualityManagementPlanID)
            .Union(overlaps.Select(x => x.OverlappingWaterQualityManagementPlanID))
            .ToList();
        var waterQualityManagementPlanIDsThatTreat = await WaterQualityManagementPlans.ListIDsThatTreatAsync(dbContext, waterQualityManagementPlanIDs);

        return overlaps
            .Select(x => AsGridDto(x,
                waterQualityManagementPlanIDsThatTreat.Contains(x.WaterQualityManagementPlanID),
                waterQualityManagementPlanIDsThatTreat.Contains(x.OverlappingWaterQualityManagementPlanID)))
            .OrderByDescending(x => x.AreaOfOverlapInAcres)
            .ToList();
    }

    public static WaterQualityManagementPlanBoundaryOverlapGridDto AsGridDto(vWaterQualityManagementPlanBoundaryOverlap overlap, bool treatsRunoff, bool overlappingTreatsRunoff)
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
            // only a pair where exactly one plan treats is resolved by the overlay (OverlayEngine.YieldToTreatingPlans)
            IsCountedTwice = treatsRunoff == overlappingTreatsRunoff,
        };
    }
}
