using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessments
{
    private static IQueryable<TreatmentBMPAssessment> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPAssessments
            .Include(x => x.FieldVisit)
            .ThenInclude(x => x.PerformedByPerson)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.TreatmentBMPType)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.TreatmentBMPType)
            .ThenInclude(x => x.TreatmentBMPTypeAssessmentObservationTypes)
            .ThenInclude(x => x.TreatmentBMPAssessmentObservationType)
            .Include(x => x.TreatmentBMPObservations)
            .ThenInclude(x => x.TreatmentBMPAssessmentObservationType)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.TreatmentBMPBenchmarkAndThresholds)
            ;
    }

    public static TreatmentBMPAssessment GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        var treatmentBMPAssessment = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID);
        Check.RequireNotNull(treatmentBMPAssessment, $"TreatmentBMPAssessment with ID {treatmentBMPAssessmentID} not found!");
        return treatmentBMPAssessment;
    }

    public static TreatmentBMPAssessment GetByIDWithChangeTracking(NeptuneDbContext dbContext, TreatmentBMPAssessmentPrimaryKey treatmentBMPAssessmentPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, treatmentBMPAssessmentPrimaryKey.PrimaryKeyValue);
    }

    public static TreatmentBMPAssessment GetByID(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        var treatmentBMPAssessment = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID);
        Check.RequireNotNull(treatmentBMPAssessment, $"TreatmentBMPAssessment with ID {treatmentBMPAssessmentID} not found!");
        return treatmentBMPAssessment;
    }

    public static TreatmentBMPAssessment GetByID(NeptuneDbContext dbContext, TreatmentBMPAssessmentPrimaryKey treatmentBMPAssessmentPrimaryKey)
    {
        return GetByID(dbContext, treatmentBMPAssessmentPrimaryKey.PrimaryKeyValue);
    }

    public static List<TreatmentBMPAssessment> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList();
    }

    public static List<TreatmentBMPAssessment> ListByFieldVisitID(NeptuneDbContext dbContext, int fieldVisitID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.FieldVisitID == fieldVisitID).ToList();
    }

    public static TreatmentBMPAssessment? GetByFieldVisitIDAndTreatmentBMPAssessmentType(NeptuneDbContext dbContext, int fieldVisitID, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
    {
        return GetImpl(dbContext).AsNoTracking().SingleOrDefault(x => x.FieldVisitID == fieldVisitID && x.TreatmentBMPAssessmentTypeID == (int) treatmentBMPAssessmentTypeEnum);
    }

    public static TreatmentBMPAssessment? GetByFieldVisitIDAndTreatmentBMPAssessmentTypeWithChangeTracking(NeptuneDbContext dbContext, int fieldVisitID, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
    {
        return GetImpl(dbContext).SingleOrDefault(x => x.FieldVisitID == fieldVisitID && x.TreatmentBMPAssessmentTypeID == (int) treatmentBMPAssessmentTypeEnum);
    }

    public static TreatmentBMPAssessment GetByIDForFeatureContextCheck(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        var treatmentBMPAssessment = dbContext.TreatmentBMPAssessments
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID);
        Check.RequireNotNull(treatmentBMPAssessment, $"TreatmentBMPAssessment with ID {treatmentBMPAssessmentID} not found!");
        return treatmentBMPAssessment;
    }

    /// <summary>
    /// Resolves the static-lookup display names that the Expression projection can't pick
    /// up directly (TreatmentBMPAssessmentType and the observation types' collection method
    /// are static C# lookup classes, not EF-mapped joins).
    /// </summary>
    private static TreatmentBMPAssessmentDetailDto ResolveLookupDisplayNames(TreatmentBMPAssessmentDetailDto dto)
    {
        if (TreatmentBMPAssessmentType.AllLookupDictionary.TryGetValue(dto.TreatmentBMPAssessmentTypeID, out var assessmentType))
        {
            dto.TreatmentBMPAssessmentTypeDisplayName = assessmentType.TreatmentBMPAssessmentTypeDisplayName;
        }
        foreach (var ot in dto.ObservationTypes)
        {
            if (ObservationTypeSpecification.AllLookupDictionary.TryGetValue(ot.ObservationTypeSpecificationID, out var spec))
            {
                ot.ObservationTypeCollectionMethodName = spec.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodName;
            }
        }
        return dto;
    }

    public static async Task<TreatmentBMPAssessmentDetailDto?> GetByIDAsDetailDtoAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        var dto = await dbContext.TreatmentBMPAssessments
            .AsNoTracking()
            .Where(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID)
            .Select(TreatmentBMPAssessmentProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        if (dto == null) return null;
        ResolveLookupDisplayNames(dto);
        await PopulateObservationScoresAsync(dbContext, dto);
        return dto;
    }

    public static async Task<TreatmentBMPAssessmentDetailDto?> GetByFieldVisitIDAndTypeAsDetailDtoAsync(NeptuneDbContext dbContext, int fieldVisitID, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
    {
        var dto = await dbContext.TreatmentBMPAssessments
            .AsNoTracking()
            .Where(x => x.FieldVisitID == fieldVisitID && x.TreatmentBMPAssessmentTypeID == (int)treatmentBMPAssessmentTypeEnum)
            .Select(TreatmentBMPAssessmentProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        if (dto == null) return null;
        ResolveLookupDisplayNames(dto);
        await PopulateObservationScoresAsync(dbContext, dto);
        return dto;
    }

    /// <summary>
    /// NPT-984: post-materialize, compute the per-observation score by loading the assessment
    /// entity (with the BMP + benchmark/threshold tree) and calling FormattedObservationScore()
    /// on each TreatmentBMPObservation. The Expression projection can't do this because
    /// CalculateObservationScore walks the static ObservationTypeCollectionMethod lookup tree
    /// and needs the BMP entity for benchmark context. Stamps the formatted score back onto
    /// each observation DTO by matching on TreatmentBMPObservationID.
    /// </summary>
    private static async Task PopulateObservationScoresAsync(NeptuneDbContext dbContext, TreatmentBMPAssessmentDetailDto dto)
    {
        if (dto.Observations == null || dto.Observations.Count == 0) return;
        var assessment = await GetImpl(dbContext).AsNoTracking()
            .SingleOrDefaultAsync(x => x.TreatmentBMPAssessmentID == dto.TreatmentBMPAssessmentID);
        if (assessment == null) return;
        var scoreByObservationID = assessment.TreatmentBMPObservations
            .ToDictionary(o => o.TreatmentBMPObservationID, o => o.FormattedObservationScore());
        foreach (var observationDto in dto.Observations)
        {
            if (scoreByObservationID.TryGetValue(observationDto.TreatmentBMPObservationID, out var score))
            {
                observationDto.ObservationScore = score;
            }
        }
    }

    /// <summary>
    /// NPT-984: returns every assessment row in the caller's viewable jurisdictions, ordered
    /// most-recent first. Powers the Field Records "Assessments" tab, which lists every
    /// assessment ever recorded (not just the latest per BMP). The previous round folded
    /// this and the Latest-BMP-Assessments listing into one helper that always filtered to
    /// most-recent, which silently regressed the Field Records tab — round 6 splits them.
    /// FailureNotes is intentionally left null here — the Assessments tab doesn't render it,
    /// and computing failure notes scales with the *all-assessments* row count, which would
    /// pull every PassFail observation + deserialize every ObservationData JSON blob on
    /// every page hit (Copilot PR #512 review).
    /// </summary>
    public static List<TreatmentBMPAssessmentGridDto> ListAllAsGridDtoForJurisdictions(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        var jurisdictionIDs = stormwaterJurisdictionIDsPersonCanView.ToList();
        var rows = dbContext.vTreatmentBMPAssessmentDetaileds.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .OrderByDescending(x => x.VisitDate)
            .ToList();
        var emptyFailureNotes = new Dictionary<int, string>();
        return rows.Select(x => ProjectGridDto(x, emptyFailureNotes)).ToList();
    }

    /// <summary>
    /// NPT-984: returns one row per Treatment BMP — the most-recent assessment from a wrapped-up
    /// (FieldVisitStatusID = Complete) visit. Mirrors the legacy MVC pattern
    /// (TreatmentBMPController.TreatmentBMPAssessmentSummaryGridJsonData) by filtering on
    /// vMostRecentTreatmentBMPAssessment.LastAssessmentID. Powers the Latest BMP Assessments page.
    /// </summary>
    public static List<TreatmentBMPAssessmentGridDto> ListLatestAsGridDtoForJurisdictions(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        var jurisdictionIDs = stormwaterJurisdictionIDsPersonCanView.ToList();
        var mostRecentAssessmentIDs = dbContext.vMostRecentTreatmentBMPAssessments.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .Select(x => x.LastAssessmentID)
            .ToList();

        var rows = dbContext.vTreatmentBMPAssessmentDetaileds.AsNoTracking()
            .Where(x => mostRecentAssessmentIDs.Contains(x.TreatmentBMPAssessmentID))
            .OrderByDescending(x => x.VisitDate)
            .ToList();
        var failureNotes = BuildFailureNotesByAssessment(dbContext, mostRecentAssessmentIDs);
        return rows.Select(x => ProjectGridDto(x, failureNotes)).ToList();
    }

    /// <summary>
    /// Loads the PassFail observations whose recorded value was false for the supplied
    /// assessments, deserializes each ObservationData blob, and concatenates the per-observation
    /// notes the same way the legacy MVC view does. ObservationData is JSON text so the failing
    /// filter has to materialize in-memory; the IN clause is bounded by jurisdiction-scope so the
    /// row count stays manageable. Tolerates null/non-bool values so a single malformed payload
    /// doesn't 500 the whole page (Copilot PR #507 #1, #2).
    /// </summary>
    private static Dictionary<int, string> BuildFailureNotesByAssessment(NeptuneDbContext dbContext, IReadOnlyCollection<int> assessmentIDs)
    {
        if (assessmentIDs.Count == 0) return new Dictionary<int, string>();

        static bool IsFailingPassFailValue(object? observationValue)
        {
            return observationValue switch
            {
                bool b => !b,
                string s when bool.TryParse(s, out var parsed) => !parsed,
                _ => false,
            };
        }

        var failingObservations = dbContext.TreatmentBMPObservations.AsNoTracking()
            .Include(x => x.TreatmentBMPAssessmentObservationType)
            .Where(x => assessmentIDs.Contains(x.TreatmentBMPAssessmentID))
            .ToList()
            .Where(x => x.TreatmentBMPAssessmentObservationType.ObservationTypeSpecification.ObservationTypeCollectionMethodID == (int)ObservationTypeCollectionMethodEnum.PassFail
                        && x.GetPassFailObservationData().SingleValueObservations.Any(y => IsFailingPassFailValue(y.ObservationValue)))
            .ToList();

        return failingObservations
            .GroupBy(x => x.TreatmentBMPAssessmentID)
            .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(x =>
            {
                var noteParts = x.GetPassFailObservationData()
                    .SingleValueObservations
                    .Select(y => y.Notes)
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(". ", noteParts);
                if (string.IsNullOrWhiteSpace(joined)) joined = "[None provided]";
                return $"{x.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName} Failure Notes: {joined}";
            }).OrderBy(s => s)));
    }

    private static TreatmentBMPAssessmentGridDto ProjectGridDto(vTreatmentBMPAssessmentDetailed x, IReadOnlyDictionary<int, string> failureNotesByAssessment)
    {
        return new TreatmentBMPAssessmentGridDto
        {
            TreatmentBMPAssessmentID = x.TreatmentBMPAssessmentID,
            FieldVisitID = x.FieldVisitID,
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
            VisitDate = x.VisitDate,
            StormwaterJurisdictionID = x.StormwaterJurisdictionID,
            StormwaterJurisdictionName = x.StormwaterJurisdictionName,
            WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
            WaterQualityManagementPlanName = x.WaterQualityManagementPlanName,
            PerformedByPersonID = x.PerformedByPersonID,
            PerformedByPersonName = x.PerformedByPersonName,
            FieldVisitTypeDisplayName = x.FieldVisitTypeDisplayName,
            TreatmentBMPAssessmentTypeDisplayName = x.TreatmentBMPAssessmentTypeDisplayName,
            IsAssessmentComplete = x.IsAssessmentComplete,
            AssessmentScore = x.AssessmentScore,
            IsFieldVisitVerified = x.IsFieldVisitVerified,
            FailureNotes = failureNotesByAssessment.GetValueOrDefault(x.TreatmentBMPAssessmentID),
        };
    }

    public static async Task<TreatmentBMPAssessmentDetailDto> CreateForFieldVisitAsync(NeptuneDbContext dbContext, int fieldVisitID, TreatmentBMPAssessmentTypeEnum assessmentTypeEnum)
    {
        var fieldVisit = FieldVisits.GetByIDWithChangeTracking(dbContext, fieldVisitID);

        var existing = GetByFieldVisitIDAndTreatmentBMPAssessmentTypeWithChangeTracking(dbContext, fieldVisitID, assessmentTypeEnum);
        if (existing != null)
        {
            return (await GetByIDAsDetailDtoAsync(dbContext, existing.TreatmentBMPAssessmentID))!;
        }

        var assessment = new TreatmentBMPAssessment
        {
            TreatmentBMPID = fieldVisit.TreatmentBMPID,
            TreatmentBMPTypeID = fieldVisit.TreatmentBMP.TreatmentBMPTypeID,
            FieldVisitID = fieldVisitID,
            TreatmentBMPAssessmentTypeID = (int)assessmentTypeEnum,
            IsAssessmentComplete = false,
        };
        await dbContext.TreatmentBMPAssessments.AddAsync(assessment);
        await dbContext.SaveChangesAsync();
        return (await GetByIDAsDetailDtoAsync(dbContext, assessment.TreatmentBMPAssessmentID))!;
    }

    public static async Task<TreatmentBMPAssessmentDetailDto> UpsertObservationsAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID, List<TreatmentBMPObservationUpsertDto> observations, int callingPersonID)
    {
        var assessment = GetByIDWithChangeTracking(dbContext, treatmentBMPAssessmentID);
        var fieldVisit = FieldVisits.GetByIDWithChangeTracking(dbContext, assessment.FieldVisitID);
        var callingPerson = People.GetByID(dbContext, callingPersonID);
        fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(callingPerson);

        var treatmentBMPType = assessment.TreatmentBMPType;

        foreach (var observationUpsert in observations)
        {
            var observationType = TreatmentBMPAssessmentObservationTypes.GetByIDWithChangeTracking(dbContext, observationUpsert.TreatmentBMPAssessmentObservationTypeID);
            var typeAssessmentObservationType = treatmentBMPType.GetTreatmentBMPTypeObservationType(observationType);

            var observation = assessment.TreatmentBMPObservations
                .FirstOrDefault(x => x.TreatmentBMPAssessmentObservationTypeID == observationType.TreatmentBMPAssessmentObservationTypeID);

            if (observation == null)
            {
                observation = new TreatmentBMPObservation
                {
                    TreatmentBMPAssessment = assessment,
                    TreatmentBMPTypeAssessmentObservationType = typeAssessmentObservationType,
                    TreatmentBMPType = treatmentBMPType,
                    TreatmentBMPAssessmentObservationType = observationType,
                };
                await dbContext.TreatmentBMPObservations.AddAsync(observation);
            }
            observation.ObservationData = observationUpsert.ObservationData;
        }

        assessment.CalculateIsAssessmentComplete();
        assessment.CalculateAssessmentScore(treatmentBMPType, assessment.TreatmentBMP);

        await dbContext.SaveChangesAsync();
        return (await GetByIDAsDetailDtoAsync(dbContext, treatmentBMPAssessmentID))!;
    }

    public static async Task<TreatmentBMPAssessmentDetailDto> CopyObservationsFromInitialAsync(NeptuneDbContext dbContext, int postMaintenanceAssessmentID, int callingPersonID)
    {
        var postMaint = GetByIDWithChangeTracking(dbContext, postMaintenanceAssessmentID);
        Check.Require(postMaint.TreatmentBMPAssessmentTypeID == (int)TreatmentBMPAssessmentTypeEnum.PostMaintenance,
            "CopyObservationsFromInitial is only valid for post-maintenance assessments.");

        var initial = GetByFieldVisitIDAndTreatmentBMPAssessmentType(dbContext, postMaint.FieldVisitID, TreatmentBMPAssessmentTypeEnum.Initial);
        Check.RequireNotNull(initial, "No Initial Assessment exists to copy from.");

        var observations = initial.TreatmentBMPObservations.Select(o => new TreatmentBMPObservationUpsertDto
        {
            TreatmentBMPAssessmentObservationTypeID = o.TreatmentBMPAssessmentObservationTypeID,
            ObservationData = o.ObservationData,
        }).ToList();

        return await UpsertObservationsAsync(dbContext, postMaintenanceAssessmentID, observations, callingPersonID);
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        var assessment = GetByIDWithChangeTracking(dbContext, treatmentBMPAssessmentID);
        await assessment.DeleteFull(dbContext);
    }
}
