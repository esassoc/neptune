using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class OnlandVisualTrashAssessments
{
    private static IQueryable<OnlandVisualTrashAssessment> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.OnlandVisualTrashAssessments
                .Include(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Include(x => x.OnlandVisualTrashAssessmentArea)
                .Include(x => x.OnlandVisualTrashAssessmentObservations)
                .Include(x => x.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes)
                .Include(x => x.CreatedByPerson)
            ;
    }

    public static OnlandVisualTrashAssessment GetByIDWithChangeTracking(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = GetImpl(dbContext)
            .SingleOrDefault(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);
        Check.RequireNotNull(onlandVisualTrashAssessment, $"OnlandVisualTrashAssessment with ID {onlandVisualTrashAssessmentID} not found!");
        return onlandVisualTrashAssessment;
    }

    public static OnlandVisualTrashAssessment GetByID(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.OnlandVisualTrashAssessmentArea)
            .Include(x => x.OnlandVisualTrashAssessmentObservations)
            .Include(x => x.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes)
            .Include(x => x.CreatedByPerson).AsNoTracking()
            .Single(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);
        return onlandVisualTrashAssessment;
    }

    public static List<OnlandVisualTrashAssessment> ListByOnlandVisualTrashAssessmentAreaID(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentAreaID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.OnlandVisualTrashAssessmentAreaID == onlandVisualTrashAssessmentAreaID).OrderByDescending(x => x.CompletedDate).ToList();
    }

    public static List<OnlandVisualTrashAssessmentGridDto> ListByStormwaterJurisdictionIDAsGridDto(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDList)
    {
        return dbContext.OnlandVisualTrashAssessments
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.OnlandVisualTrashAssessmentArea)
            .Include(x => x.CreatedByPerson).AsNoTracking().Where(x => stormwaterJurisdictionIDList.Contains(x.StormwaterJurisdictionID)).ToList()
            .OrderByDescending(x => x.CompletedDate)
            .ThenBy(x => x.OnlandVisualTrashAssessmentArea == null)
            .ThenBy(x => x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentAreaName)
            .Select(x => x.AsGridDto()).ToList();
    }

    public static OnlandVisualTrashAssessment? GetTransectBackingAssessment(NeptuneDbContext dbContext, int? onlandVisualTrashAssessmentAreaID)
    {
        return ListByOnlandVisualTrashAssessmentAreaID(dbContext, onlandVisualTrashAssessmentAreaID.Value).SingleOrDefault(x =>
            x.IsTransectBackingAssessment);
    }

    public static async Task<OnlandVisualTrashAssessmentSimpleDto> CreateNew(NeptuneDbContext dbContext, OnlandVisualTrashAssessmentSimpleDto dto, PersonDto currentUser)
    {
        var onlandVisualTrashAssessment = new OnlandVisualTrashAssessment()
        {
            CreatedByPersonID = currentUser.PersonID,
            CreatedDate = DateTime.UtcNow,
            OnlandVisualTrashAssessmentAreaID = dto.OnlandVisualTrashAssessmentAreaID,
            StormwaterJurisdictionID = dto.StormwaterJurisdictionID,
            OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress
        };

        var assessingNewArea = (dto.AssessingNewArea ?? false);
        onlandVisualTrashAssessment.AssessingNewArea = assessingNewArea || !dto.OnlandVisualTrashAssessmentAreaID.HasValue;

        if (onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID.HasValue)
        {
            var transectBackingAssessment = GetTransectBackingAssessment(dbContext,
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID);

            // ensure the area to which this assessment is assigned ends up with only one transect-backing assessment
            if (transectBackingAssessment != null)
            {
                if (transectBackingAssessment.OnlandVisualTrashAssessmentID != onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID)
                {
                    onlandVisualTrashAssessment.IsTransectBackingAssessment = false;
                }
            }
            else
            {
                onlandVisualTrashAssessment.IsTransectBackingAssessment = true;
            }
        }
        else
        {
            onlandVisualTrashAssessment.IsTransectBackingAssessment = false;
        }

        await dbContext.AddAsync(onlandVisualTrashAssessment);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(onlandVisualTrashAssessment).ReloadAsync();
        
        return onlandVisualTrashAssessment.AsSimpleDto();
    }

    public static async Task Update(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID,
        OnlandVisualTrashAssessmentReviewAndFinalizeDto dto)
    {

        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments
            .Include(x => x.OnlandVisualTrashAssessmentArea)
            .Include(onlandVisualTrashAssessment => onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations)
            .Single(x =>
            x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);

        if (dto.OnlandVisualTrashAssessmentStatusID == (int)OnlandVisualTrashAssessmentStatusEnum.Complete)
        {
            // create the assessment area
            if (onlandVisualTrashAssessment.AssessingNewArea.GetValueOrDefault())
            {
                var onlandVisualTrashAssessmentAreaGeometry2771 = onlandVisualTrashAssessment.DraftGeometry;

                var onlandVisualTrashAssessmentArea = new OnlandVisualTrashAssessmentArea
                {
                    OnlandVisualTrashAssessmentAreaName = dto.OnlandVisualTrashAssessmentAreaName,
                    StormwaterJurisdictionID = onlandVisualTrashAssessment.StormwaterJurisdictionID,
                    OnlandVisualTrashAssessmentAreaGeometry = onlandVisualTrashAssessmentAreaGeometry2771,
                    OnlandVisualTrashAssessmentAreaGeometry4326 = onlandVisualTrashAssessmentAreaGeometry2771.ProjectTo4326()
                };
                await dbContext.OnlandVisualTrashAssessmentAreas.AddAsync(onlandVisualTrashAssessmentArea);
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(onlandVisualTrashAssessmentArea).ReloadAsync();

                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaID;
                onlandVisualTrashAssessment.DraftGeometry = null;
                onlandVisualTrashAssessment.DraftAreaDescription = null;
                onlandVisualTrashAssessment.DraftAreaName = null;


            }

            onlandVisualTrashAssessment.CompletedDate = dto.AssessmentDate;
            onlandVisualTrashAssessment.IsProgressAssessment = dto.IsProgressAssessment ?? false;

            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScoreID = dto.OnlandVisualTrashAssessmentScoreID;
            onlandVisualTrashAssessment.Notes = dto.Notes;
            onlandVisualTrashAssessment.SecondAssessorName = string.IsNullOrWhiteSpace(dto.SecondAssessorName) ? null : dto.SecondAssessorName.Trim();
            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID = dto.OnlandVisualTrashAssessmentStatusID;

            await dbContext.SaveChangesAsync();

            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.AssessmentAreaDescription = dto.AssessmentAreaDescription;

            var onlandVisualTrashAssessments = OnlandVisualTrashAssessments.ListByOnlandVisualTrashAssessmentAreaID(dbContext, onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID.Value);
            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScoreID =
                OnlandVisualTrashAssessmentAreas.CalculateBaselineScoreFromBackingData(onlandVisualTrashAssessments)?
                    .OnlandVisualTrashAssessmentScoreID;

            if (dto.IsProgressAssessment ?? false)
            {
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScoreID = CalculateProgressScore(onlandVisualTrashAssessments)
                        ?.OnlandVisualTrashAssessmentScoreID;
            }

            // Recompute the area's transect line when this save belongs to the backing
            // assessment: either the area has no line yet (first finalize) or this assessment
            // is flagged as the backing one (return-to-edit). Repeat assessments on an area
            // that already has a backing-derived transect leave it alone.
            var wasNeverSet = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.TransectLine == null;
            // Assessments finalized before the IsTransectBackingAssessment flag existed have it set false even
            // though they're the area's backing assessment, so the flag-or-wasNeverSet gate silently skipped the
            // transect update forever. Treat this assessment as backing when no OTHER assessment in the area is
            // flagged backing, and stamp the flag below so subsequent saves take the flag path directly.
            var noOtherBackingExists = !await dbContext.OnlandVisualTrashAssessments
                .AnyAsync(x => x.OnlandVisualTrashAssessmentAreaID == onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID
                          && x.IsTransectBackingAssessment
                          && x.OnlandVisualTrashAssessmentID != onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID);
            var isBackingAssessment = onlandVisualTrashAssessment.IsTransectBackingAssessment || wasNeverSet || noOtherBackingExists;

            if (isBackingAssessment && onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations.Count >= 2)
            {
                var transect = GetTransectLine(onlandVisualTrashAssessment);
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.TransectLine = transect;
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.TransectLine4326 = transect.ProjectTo4326();

                if (wasNeverSet || noOtherBackingExists)
                {
                    // First finalize, or a legacy backing assessment that was never stamped — stamp this
                    // assessment as backing and demote any stale flag elsewhere (defensive for odd states).
                    onlandVisualTrashAssessment.IsTransectBackingAssessment = true;
                    var existingBacking = GetTransectBackingAssessment(dbContext, onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID);
                    if (existingBacking != null && existingBacking.OnlandVisualTrashAssessmentID != onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID)
                    {
                        existingBacking.IsTransectBackingAssessment = false;
                    }
                }
            }
        }
        else
        {
            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentScoreID = dto.OnlandVisualTrashAssessmentScoreID;
            onlandVisualTrashAssessment.Notes = dto.Notes;
            onlandVisualTrashAssessment.SecondAssessorName = string.IsNullOrWhiteSpace(dto.SecondAssessorName) ? null : dto.SecondAssessorName.Trim();
            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentStatusID = dto.OnlandVisualTrashAssessmentStatusID;
            if (onlandVisualTrashAssessment.AssessingNewArea ?? false)
            {
                onlandVisualTrashAssessment.DraftAreaName = dto.OnlandVisualTrashAssessmentAreaName;
                onlandVisualTrashAssessment.DraftAreaDescription = dto.AssessmentAreaDescription;
            }
            await dbContext.SaveChangesAsync();
        }

        await dbContext.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes.Where(x =>
                x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID)
            .ExecuteDeleteAsync();

        var onlandVisualTrashAssessmentPreliminarySourceIdentificationTypesToUpdate =
            dto.PreliminarySourceIdentifications
                .Where(x => x.Selected)
                .Select(x => new OnlandVisualTrashAssessmentPreliminarySourceIdentificationType
                {
                    OnlandVisualTrashAssessmentID = onlandVisualTrashAssessmentID,
                    PreliminarySourceIdentificationTypeID =
                        x.PreliminarySourceIdentificationTypeID,
                    ExplanationIfTypeIsOther = string.IsNullOrWhiteSpace(x.ExplanationIfTypeIsOther) ? null : x.ExplanationIfTypeIsOther
                }).ToList();

        await dbContext.OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes.AddRangeAsync(onlandVisualTrashAssessmentPreliminarySourceIdentificationTypesToUpdate);
        await dbContext.SaveChangesAsync();
    }

    public static async Task RefreshParcels(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments.Single(x =>
            x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);
        onlandVisualTrashAssessment.DraftGeometry = null;
        onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined = false;

        await dbContext.SaveChangesAsync();
    }

    public static async Task UpdateDraftGeometryFromSelection(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID, OnlandVisualTrashAssessmentAddRemoveParcelsDto dto)
    {
        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments.Single(x =>
            x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);

        if (dto.OvtaAreaSourceTypeID == (int)OvtaAreaSourceTypeEnum.LandUseBlock)
        {
            var ids = dto.SelectedLandUseBlockIDs ?? new List<int>();
            onlandVisualTrashAssessment.DraftGeometry = ids.Any()
                ? LandUseBlocks.UnionAggregateByLandUseBlockIDs(dbContext, ids, onlandVisualTrashAssessment.StormwaterJurisdictionID)
                : null;
            onlandVisualTrashAssessment.OvtaAreaSourceTypeID = (int)OvtaAreaSourceTypeEnum.LandUseBlock;
        }
        else
        {
            var ids = dto.SelectedParcelIDs ?? new List<int>();
            onlandVisualTrashAssessment.DraftGeometry = ids.Any()
                ? ParcelGeometries.UnionAggregateByParcelIDs(dbContext, ids)
                : null;
            onlandVisualTrashAssessment.OvtaAreaSourceTypeID = (int)OvtaAreaSourceTypeEnum.Parcel;
        }

        // The user just rebuilt the area from a selection list; any prior manual refinement is
        // discarded so the next visit to Refine starts from the unioned shape.
        onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined = false;

        await dbContext.SaveChangesAsync();
    }

    public static async Task UpdateGeometry(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID, string geometryAsGeoJson)
    {
        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments.Single(x =>
            x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);

        if (String.IsNullOrWhiteSpace(geometryAsGeoJson))
        {
            onlandVisualTrashAssessment.DraftGeometry = null;
            onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined = false;
            await dbContext.SaveChangesAsync();
            return;
        }

        var newGeometry = GeoJsonSerializer.Deserialize<IFeature>(geometryAsGeoJson);
        newGeometry.Geometry.SRID = Proj4NetHelper.WEB_MERCATOR;
        newGeometry.Geometry = newGeometry.Geometry.ProjectTo2771();

        // since this is coming from the browser, we have to transform to State Plane
        onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined = !newGeometry.Equals(onlandVisualTrashAssessment.DraftGeometry);
        onlandVisualTrashAssessment.DraftGeometry = newGeometry.Geometry;
        await dbContext.SaveChangesAsync();
    }

    public static FeatureCollection GetTransectLine4326GeoJson(OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var attributesTable = new AttributesTable
        {
            {
                "OnlandVisualTrashAssessmentID", onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID
            }
        };
        var featureCollection = new FeatureCollection();
        if (onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea?.TransectLine4326 != null)
        {
            var feature = new Feature(onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea.TransectLine4326, attributesTable);
            featureCollection.Add(feature);
        }
        else
        {

            var onlandVisualTrashAssessmentObservations =
                onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations;
            var transectLine4326 = GetTransectLine4326(onlandVisualTrashAssessmentObservations);
            if (transectLine4326 != null)
            {
                var feature = new Feature(transectLine4326, attributesTable);
                featureCollection.Add(feature);
            }
        }

        return featureCollection;
    }

    private static Geometry? GetTransectLine(OnlandVisualTrashAssessment ovta)
    {
        return GetTransectLine(ovta.OnlandVisualTrashAssessmentObservations);
    }

    public static Geometry? GetTransectLine4326(ICollection<OnlandVisualTrashAssessmentObservation> onlandVisualTrashAssessmentObservations)
    {
        return GetTransectLine(onlandVisualTrashAssessmentObservations)?.ProjectTo4326();
    }

    public static Geometry? GetTransectLine(ICollection<OnlandVisualTrashAssessmentObservation> onlandVisualTrashAssessmentObservations)
    {
        if (onlandVisualTrashAssessmentObservations.Count > 1)
        {
            var points = string.Join(",",
                onlandVisualTrashAssessmentObservations.OrderBy(x => x.ObservationDatetime)
                    .Select(x => x.LocationPoint).ToList().Select(x => $"{x.Coordinate.X} {x.Coordinate.Y}")
                    .ToList());

            var linestring = $"LINESTRING ({points})";
            return GeometryHelper.FromWKT(linestring, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
        }

        return null;
    }

    public static IQueryable<ParcelGeometry> GetParcelsViaTransect(this OnlandVisualTrashAssessment ovta,
        NeptuneDbContext dbContext)
    {
        if (!ovta.OnlandVisualTrashAssessmentObservations.Any())
        {
            return new List<ParcelGeometry>().AsQueryable();
        }

        var transect = ovta.OnlandVisualTrashAssessmentObservations.Count == 1
            ? ovta.OnlandVisualTrashAssessmentObservations.Single().LocationPoint // don't attempt to calculate the transect
            : GetTransectLine(ovta.OnlandVisualTrashAssessmentObservations);

        return ParcelGeometries.GetIntersected(dbContext, transect);
    }

    public static List<int> GetParcelIDsForAddOrRemoveParcels(
        this OnlandVisualTrashAssessment onlandVisualTrashAssessment, NeptuneDbContext dbContext)
    {
        if (onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined.GetValueOrDefault())
        {
            return new List<int>();
        }

        // NP 8/22 these are supposed to be in 2771 already, but due to a b*g somewhere, some of them have 4326 as their SRID even though the coords are 2771...
        var draftGeometry = onlandVisualTrashAssessment.DraftGeometry;//.FixSrid(CoordinateSystemHelper.NAD_83_HARN_CA_ZONE_VI_SRID);

        // ... and the wrong SRID would cause this next lookup to fail bigly
        var parcelIDs = draftGeometry == null
            ? onlandVisualTrashAssessment.GetParcelsViaTransect(dbContext).Select(x => x.ParcelID)
            : dbContext.ParcelGeometries.AsNoTracking()
                .Where(x => draftGeometry.Contains(x.GeometryNative)).Select(x => x.ParcelID);

        return parcelIDs.ToList();
    }

    public static List<int> GetLandUseBlockIDsForSelectArea(
        this OnlandVisualTrashAssessment onlandVisualTrashAssessment, NeptuneDbContext dbContext)
    {
        if (onlandVisualTrashAssessment.IsDraftGeometryManuallyRefined.GetValueOrDefault())
        {
            return new List<int>();
        }

        var draftGeometry = onlandVisualTrashAssessment.DraftGeometry;

        // No saved geometry yet → seed from blocks intersecting the transect line, scoped to
        // this OVTA's jurisdiction so we don't pull in blocks the user can't edit.
        if (draftGeometry == null)
        {
            if (!onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations.Any())
            {
                return new List<int>();
            }

            var transect = onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations.Count == 1
                ? onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations.Single().LocationPoint
                : GetTransectLine(onlandVisualTrashAssessment.OnlandVisualTrashAssessmentObservations);

            return transect == null
                ? new List<int>()
                : LandUseBlocks
                    .GetIntersected(dbContext, transect, onlandVisualTrashAssessment.StormwaterJurisdictionID)
                    .Select(x => x.LandUseBlockID).ToList();
        }

        // DraftGeometry exists → return blocks fully contained in it (mirrors the parcel logic).
        return dbContext.LandUseBlocks.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == onlandVisualTrashAssessment.StormwaterJurisdictionID
                        && draftGeometry.Contains(x.LandUseBlockGeometry))
            .Select(x => x.LandUseBlockID).ToList();
    }

    public static OnlandVisualTrashAssessmentScore? CalculateProgressScore(List<OnlandVisualTrashAssessment> onlandVisualTrashAssessments)
    {
        var fiveYearsAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-5));
        var completedAndIsProgressAssessment = onlandVisualTrashAssessments.Where(x =>
            x.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatus.Complete
                .OnlandVisualTrashAssessmentStatusID && x.IsProgressAssessment && x.CompletedDate >= fiveYearsAgo).ToList();

        if (completedAndIsProgressAssessment.Count < 1)
        {
            return null;
        }

        var average = completedAndIsProgressAssessment.OrderByDescending(x => x.CompletedDate).Take(3).Average(x => x.OnlandVisualTrashAssessmentScore.NumericValue);

        var onlandVisualTrashAssessmentScore = OnlandVisualTrashAssessmentScore.All.Single(x => x.NumericValue == Math.Round(average));

        return onlandVisualTrashAssessmentScore;
    }

    public static FeatureCollection GetAssessmentAreaByIDAsFeatureCollection(NeptuneDbContext dbContext, int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessment = dbContext.OnlandVisualTrashAssessments
            .Include(x => x.OnlandVisualTrashAssessmentArea).AsNoTracking()
            .Single(x => x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID);
        return GetAssessmentAreasFeatureCollection(onlandVisualTrashAssessment);
    }

    public static FeatureCollection GetAssessmentAreasFeatureCollection(OnlandVisualTrashAssessment onlandVisualTrashAssessment)
    {
        var attributesTable = new AttributesTable
        {
            { "OnlandVisualTrashAssessmentID", onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID },
        };
        var featureCollection = new FeatureCollection();
        var geometry =
            onlandVisualTrashAssessment.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentAreaGeometry4326 ??
            onlandVisualTrashAssessment.DraftGeometry?.ProjectTo4326();
        if (geometry != null)
        {
            var feature = new Feature(geometry, attributesTable);
            featureCollection.Add(feature);
        }

        return featureCollection;
    }
}