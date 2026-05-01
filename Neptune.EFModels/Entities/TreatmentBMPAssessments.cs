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
        return dto == null ? null : ResolveLookupDisplayNames(dto);
    }

    public static async Task<TreatmentBMPAssessmentDetailDto?> GetByFieldVisitIDAndTypeAsDetailDtoAsync(NeptuneDbContext dbContext, int fieldVisitID, TreatmentBMPAssessmentTypeEnum treatmentBMPAssessmentTypeEnum)
    {
        var dto = await dbContext.TreatmentBMPAssessments
            .AsNoTracking()
            .Where(x => x.FieldVisitID == fieldVisitID && x.TreatmentBMPAssessmentTypeID == (int)treatmentBMPAssessmentTypeEnum)
            .Select(TreatmentBMPAssessmentProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        return dto == null ? null : ResolveLookupDisplayNames(dto);
    }

    public static List<TreatmentBMPAssessmentGridDto> ListAsGridDtoForJurisdictions(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        return dbContext.vTreatmentBMPAssessmentDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
            .OrderByDescending(x => x.VisitDate)
            .ToList()
            .Select(x => new TreatmentBMPAssessmentGridDto
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
            })
            .ToList();
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
