using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.EFModels.Nereid;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static partial class WaterQualityManagementPlans
{
    private static IQueryable<WaterQualityManagementPlan> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlans
            .Include(x => x.WaterQualityManagementPlanBoundary)
            .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
            .Include(x => x.WaterQualityManagementPlanParcels).ThenInclude(x => x.Parcel)
            .Include(x => x.TreatmentBMPs).ThenInclude(x => x.Delineation);
    }

    public static WaterQualityManagementPlan GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanID)
    {
        var waterQualityManagementPlan = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        Check.RequireNotNull(waterQualityManagementPlan,
            $"WaterQualityManagementPlan with ID {waterQualityManagementPlanID} not found!");
        return waterQualityManagementPlan;
    }

    public static WaterQualityManagementPlan GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, waterQualityManagementPlanPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlan GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var waterQualityManagementPlan = GetImpl(dbContext)
            .Include(x => x.TreatmentBMPs)
            .ThenInclude(x => x.TreatmentBMPType)
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.StormwaterJurisdictionGeometry)
            .Include(x => x.HydrologicSubarea)
            .AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        Check.RequireNotNull(waterQualityManagementPlan,
            $"WaterQualityManagementPlan with ID {waterQualityManagementPlanID} not found!");
        return waterQualityManagementPlan;
    }

    public static WaterQualityManagementPlan GetByID(NeptuneDbContext dbContext,
        WaterQualityManagementPlanPrimaryKey waterQualityManagementPlanPrimaryKey)
    {
        return GetByID(dbContext, waterQualityManagementPlanPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlan GetByIDForFeatureContextCheck(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var waterQualityManagementPlan = dbContext.WaterQualityManagementPlans
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        Check.RequireNotNull(waterQualityManagementPlan, $"WaterQualityManagementPlan with ID {waterQualityManagementPlanID} not found!");
        return waterQualityManagementPlan;
    }

    public static List<WaterQualityManagementPlan> ListViewableByPerson(NeptuneDbContext dbContext, Person person)
    {
        var stormwaterJurisdictionIDsPersonCanView = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, person);

        //These users can technically see all Jurisdictions, just potentially not the WQMPs inside them
        var waterQualityManagementPlans = GetImpl(dbContext)
            .AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID));
        if (person.IsAnonymousOrUnassigned())
        {
            var publicWaterQualityManagementPlans = waterQualityManagementPlans.Where(x =>
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Active ||
                x.WaterQualityManagementPlanStatusID ==
                (int)WaterQualityManagementPlanStatusEnum.Inactive &&
                x.StormwaterJurisdiction.StormwaterJurisdictionPublicWQMPVisibilityTypeID ==
                (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.ActiveAndInactive).ToList();
            return publicWaterQualityManagementPlans;
        }

        return waterQualityManagementPlans.ToList();
    }


    public static IEnumerable<WaterQualityManagementPlan> ListBaseEntityByStormwaterJurisdictionID(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        return dbContext.WaterQualityManagementPlans
            .AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID).ToList();
    }

    public static async Task<List<WaterQualityManagementPlanDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var dtos = await dbContext.WaterQualityManagementPlans
            .AsNoTracking()
            .Select(WaterQualityManagementPlanProjections.AsDto)
            .ToListAsync();
        await PopulateBoundaryBBoxes(dbContext, dtos);
        return dtos;
    }

    public static async Task<List<WaterQualityManagementPlanDisplayDto>> ListAsDisplayDtoAsync(NeptuneDbContext dbContext)
    {
        var wqmps = await dbContext.WaterQualityManagementPlans.AsNoTracking()
            .ToListAsync();

        var displayDtos = wqmps
            .OrderBy(x => x.WaterQualityManagementPlanName)
            .Select(x => new WaterQualityManagementPlanDisplayDto()
            {
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = x.WaterQualityManagementPlanName
            })
            .ToList();

        return displayDtos;
    }

    public static async Task<WaterQualityManagementPlanDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var dto = await dbContext.WaterQualityManagementPlans
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(WaterQualityManagementPlanProjections.AsDto)
            .FirstOrDefaultAsync();
        if (dto != null)
        {
            await PopulateBoundaryBBox(dbContext, dto);
            await PopulateBMPParameterizationFlags(dbContext, dto);
        }
        return dto;
    }

    public static async Task<WaterQualityManagementPlan?> GetByNameAndJurisdiction(NeptuneDbContext dbContext, string name, int stormwaterJurisdictionID)
    {
        return await dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefaultAsync(x => x.WaterQualityManagementPlanName == name && x.StormwaterJurisdictionID == stormwaterJurisdictionID);
    }

    public static async Task<WaterQualityManagementPlanDto> CreateAsync(NeptuneDbContext dbContext, WaterQualityManagementPlanUpsertDto dto)
    {
        var entity = dto.AsEntity();
        dbContext.WaterQualityManagementPlans.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.WaterQualityManagementPlanID);
    }

    public static async Task<WaterQualityManagementPlanDto?> UpdateAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, WaterQualityManagementPlanUpsertDto dto)
    {
        var entity = await dbContext.WaterQualityManagementPlans.FirstAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.WaterQualityManagementPlanID);
    }

    // NPT-1051: Classifies a Status transition's effect on the modeling pipeline. Pure function,
    // hermetically unit-testable. Same-status saves and Draft <-> Inactive flips are no-ops
    // because neither state participates in the modeling graph. Crossing the Active boundary in
    // either direction requires DB-side cleanup or re-dirtying.
    public static StatusTransitionEffect ClassifyStatusTransition(int? oldStatusID, int? newStatusID)
    {
        if (oldStatusID == newStatusID) return StatusTransitionEffect.None;
        var active = (int)WaterQualityManagementPlanStatusEnum.Active;
        if (oldStatusID == active) return StatusTransitionEffect.LeavingActive;
        if (newStatusID == active) return StatusTransitionEffect.EnteringActive;
        return StatusTransitionEffect.None;
    }

    // NPT-1051: Side effects of a WQMP Status transition. Read-time gating (NereidService graph
    // filter, vTrashGeneratingUnitLoadStatistic CASE, TrashGeneratingUnitHelper) hides non-Active
    // WQMPs from results going forward, but the DB still carries the WQMP's previously-cached
    // NereidResult rows, and the WQMP's neighbors hold solve results that were computed *with*
    // this WQMP in the network. On any transition that crosses the Active boundary we have to
    // (a) invalidate stale rows and (b) dirty the affected nodes so the next DeltaSolveJob
    // recomputes them. Returns true when the caller should enqueue a DeltaSolveJob.
    public static async Task<bool> HandleStatusTransitionAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, int? oldStatusID)
    {
        var newStatusID = await dbContext.WaterQualityManagementPlans
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(x => x.WaterQualityManagementPlanStatusID)
            .FirstAsync();

        switch (ClassifyStatusTransition(oldStatusID, newStatusID))
        {
            case StatusTransitionEffect.LeavingActive:
                // WQMP leaves the modeling graph. Dirty the downstream regional subbasins /
                // centralized BMPs that previously folded this WQMP into their solves so they
                // get recomputed without it; then delete the WQMP's own (now stale) result rows.
                var leavingWqmp = await dbContext.WaterQualityManagementPlans
                    .Include(x => x.LoadGeneratingUnits)
                    .FirstAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
                await NereidUtilities.MarkDownstreamNodeDirty(leavingWqmp, dbContext);
                await dbContext.NereidResults
                    .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
                    .ExecuteDeleteAsync();
                await dbContext.ProjectNereidResults
                    .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
                    .ExecuteDeleteAsync();
                return true;

            case StatusTransitionEffect.EnteringActive:
                // WQMP enters the graph. Dirty it directly so DeltaSolve picks up the new node
                // on its next run.
                var enteringWqmp = await dbContext.WaterQualityManagementPlans
                    .FirstAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
                await NereidUtilities.MarkWqmpDirty(enteringWqmp, dbContext);
                return true;

            default:
                return false;
        }
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var entity = await dbContext.WaterQualityManagementPlans.FirstOrDefaultAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        if (entity == null) return false;
        await entity.DeleteFull(dbContext);
        return true;
    }

    public static async Task<List<WaterQualityManagementPlanDto>> ListWithFinalWQMPDocumentAsync(NeptuneDbContext dbContext)
    {
        // todo: filtering by a hardcoded list of WQMP IDs is temporary until we can get the full list of WQMPs that should be included
        var wqmpIDsToFilterBy = new List<int>
            {
            3066, 2845, 2856, 2857, 2850, 1623, 1632, 2528, 2531, 2343, 2527
        };
        var dtos = await dbContext.WaterQualityManagementPlans
            .Where(x => wqmpIDsToFilterBy.Contains(x.WaterQualityManagementPlanID) && x.WaterQualityManagementPlanDocuments
                .Any(y => y.WaterQualityManagementPlanDocumentTypeID == (int)WaterQualityManagementPlanDocumentTypeEnum.FinalWQMP))
            .AsNoTracking()
            .Select(WaterQualityManagementPlanProjections.AsDto)
            .ToListAsync();
        await PopulateBoundaryBBoxes(dbContext, dtos);
        return dtos;
    }

    private static async Task PopulateBoundaryBBox(NeptuneDbContext dbContext, WaterQualityManagementPlanDto dto)
    {
        var geometry4326 = await dbContext.WaterQualityManagementPlanBoundaries
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == dto.WaterQualityManagementPlanID)
            .Select(x => x.Geometry4326)
            .FirstOrDefaultAsync();

        if (geometry4326 != null)
        {
            var env = geometry4326.EnvelopeInternal;
            dto.WaterQualityManagementPlanBoundaryBBox = $"{env.MinX}, {env.MinY}, {env.MaxX}, {env.MaxY}";
        }
    }

    // Computes the 4 BMP parameterization flags used by the Modeled Performance card's
    // contextual guidance messaging. Only the pair matching the modeling approach is meaningful;
    // the other pair stays false. Mirrors Neptune.WebMvc.Views.WaterQualityManagementPlan.DetailViewData:209-224.
    private static async Task PopulateBMPParameterizationFlags(NeptuneDbContext dbContext, WaterQualityManagementPlanDto dto)
    {
        var isDetailed = dto.WaterQualityManagementPlanModelingApproachID ==
                         (int)WaterQualityManagementPlanModelingApproachEnum.Detailed;

        if (isDetailed)
        {
            var treatmentBMPs = await dbContext.TreatmentBMPs
                .AsNoTracking()
                .Include(x => x.TreatmentBMPType)
                .Where(x => x.WaterQualityManagementPlanID == dto.WaterQualityManagementPlanID)
                .ToListAsync();

            if (!treatmentBMPs.Any()) return;

            var treatmentBMPIDs = treatmentBMPs.Select(x => x.TreatmentBMPID).ToList();

            // IsFullyParameterized expects the delineation of the upstream-most BMP (not the BMP's
            // own direct delineation), so downstream BMPs can inherit an upstream's verified
            // delineation. Mirrors the legacy MVC pattern at WaterQualityManagementPlanController:232.
            var delineationsDict = vTreatmentBMPUpstreams.ListWithDelineationAsDictionaryForTreatmentBMPIDList(dbContext, treatmentBMPIDs);
            var modelingAttrsByID = await dbContext.vTreatmentBMPModelingAttributes
                .AsNoTracking()
                .Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID))
                .ToDictionaryAsync(x => x.TreatmentBMPID);

            bool IsParameterized(TreatmentBMP bmp) =>
                bmp.IsFullyParameterized(
                    delineationsDict.TryGetValue(bmp.TreatmentBMPID, out var del) ? del : null,
                    modelingAttrsByID.TryGetValue(bmp.TreatmentBMPID, out var attr) ? attr : null);

            dto.AnyDetailedBMPsNotFullyParameterized = treatmentBMPs.Any(b => !IsParameterized(b));
            dto.AllDetailedBMPsNotFullyParameterized = treatmentBMPs.All(b => !IsParameterized(b));
        }
        else
        {
            var quickBMPs = await dbContext.QuickBMPs
                .AsNoTracking()
                .Include(x => x.TreatmentBMPType)
                .Where(x => x.WaterQualityManagementPlanID == dto.WaterQualityManagementPlanID)
                .ToListAsync();

            if (!quickBMPs.Any()) return;

            // Mirrors Neptune.WebMvc.Models.QuickBMPModelExtensions.IsFullyParameterized — kept inline to
            // avoid a cross-project reference; if this logic gets reused elsewhere in the SPA stack,
            // lift it into a shared QuickBMPExtensionMethods.cs entry per the static-helpers convention.
            bool IsParameterized(QuickBMP bmp) =>
                bmp is { PercentOfSiteTreated: not null, PercentCaptured: not null, PercentRetained: not null }
                && bmp.TreatmentBMPType.IsAnalyzedInModelingModule;

            dto.AnySimpleBMPsNotFullyParameterized = quickBMPs.Any(b => !IsParameterized(b));
            dto.AllSimpleBMPsNotFullyParameterized = quickBMPs.All(b => !IsParameterized(b));
        }
    }

    private static async Task PopulateBoundaryBBoxes(NeptuneDbContext dbContext, List<WaterQualityManagementPlanDto> dtos)
    {
        if (!dtos.Any()) return;

        var wqmpIDs = dtos.Select(x => x.WaterQualityManagementPlanID).ToList();
        var boundaries = await dbContext.WaterQualityManagementPlanBoundaries
            .AsNoTracking()
            .Where(x => wqmpIDs.Contains(x.WaterQualityManagementPlanID))
            .Select(x => new { x.WaterQualityManagementPlanID, x.Geometry4326 })
            .ToListAsync();

        var boundaryLookup = boundaries
            .Where(x => x.Geometry4326 != null)
            .ToDictionary(x => x.WaterQualityManagementPlanID, x => x.Geometry4326!);

        foreach (var dto in dtos)
        {
            if (boundaryLookup.TryGetValue(dto.WaterQualityManagementPlanID, out var geometry4326))
            {
                var env = geometry4326.EnvelopeInternal;
                dto.WaterQualityManagementPlanBoundaryBBox = $"{env.MinX}, {env.MinY}, {env.MaxX}, {env.MaxY}";
            }
        }
    }

    // NPT-1051: Promotion (Draft → Active) makes the WQMP the binding legal record.
    // Validation mirrors the Basics editor modal's required-field set — only Name needs
    // an explicit check. Jurisdiction, ModelingApproach, and TrashCaptureStatus are also
    // modal-required but are NOT NULL on the entity, so they're guaranteed to be populated
    // by the time we get here. Anything else (record numbers, dates, contact info, hydromod,
    // etc.) is optional in the modal and therefore optional at Promote time.
    public static List<string> ValidateForPromote(WaterQualityManagementPlan entity)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(entity.WaterQualityManagementPlanName)) missing.Add("WQMP Name");
        return missing;
    }
}

public enum StatusTransitionEffect
{
    None,
    LeavingActive,
    EnteringActive,
}