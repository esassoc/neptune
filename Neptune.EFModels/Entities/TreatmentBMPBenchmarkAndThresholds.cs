using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPBenchmarkAndThresholds
{
    private static IQueryable<TreatmentBMPBenchmarkAndThreshold> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPBenchmarkAndThresholds;
    }

    public static TreatmentBMPBenchmarkAndThreshold GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPBenchmarkAndThresholdID)
    {
        var treatmentBMPBenchmarkAndThreshold = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPBenchmarkAndThresholdID == treatmentBMPBenchmarkAndThresholdID);
        Check.RequireNotNull(treatmentBMPBenchmarkAndThreshold, $"TreatmentBMPBenchmarkAndThreshold with ID {treatmentBMPBenchmarkAndThresholdID} not found!");
        return treatmentBMPBenchmarkAndThreshold;
    }

    public static TreatmentBMPBenchmarkAndThreshold GetByIDWithChangeTracking(NeptuneDbContext dbContext, TreatmentBMPBenchmarkAndThresholdPrimaryKey treatmentBMPBenchmarkAndThresholdPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, treatmentBMPBenchmarkAndThresholdPrimaryKey.PrimaryKeyValue);
    }

    public static TreatmentBMPBenchmarkAndThreshold GetByID(NeptuneDbContext dbContext, int treatmentBMPBenchmarkAndThresholdID)
    {
        var treatmentBMPBenchmarkAndThreshold = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPBenchmarkAndThresholdID == treatmentBMPBenchmarkAndThresholdID);
        Check.RequireNotNull(treatmentBMPBenchmarkAndThreshold, $"TreatmentBMPBenchmarkAndThreshold with ID {treatmentBMPBenchmarkAndThresholdID} not found!");
        return treatmentBMPBenchmarkAndThreshold;
    }

    public static TreatmentBMPBenchmarkAndThreshold GetByID(NeptuneDbContext dbContext, TreatmentBMPBenchmarkAndThresholdPrimaryKey treatmentBMPBenchmarkAndThresholdPrimaryKey)
    {
        return GetByID(dbContext, treatmentBMPBenchmarkAndThresholdPrimaryKey.PrimaryKeyValue);
    }

    public static List<TreatmentBMPBenchmarkAndThreshold> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList();
    }

    public static List<TreatmentBMPBenchmarkAndThreshold> ListByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.TreatmentBMPID == treatmentBMPID).ToList();
    }

    public static List<TreatmentBMPBenchmarkAndThreshold> ListByTreatmentBMPIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).Where(x => x.TreatmentBMPID == treatmentBMPID).ToList();
    }

    public static async Task<List<TreatmentBMPBenchmarkAndThresholdDto>> ListByTreatmentBMPIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return await dbContext.TreatmentBMPBenchmarkAndThresholds
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(TreatmentBMPBenchmarkAndThresholdDtoProjections.AsDto)
            .ToListAsync();
    }

    /// <summary>
    /// One row per benchmark/threshold-bearing observation type for the BMP's type, set or not.
    /// NPT-1061: the SPA previously only loaded existing benchmark rows, so BMP types with
    /// observation types that hadn't been given values yet (e.g. Permeable Pavement) showed/edited
    /// nothing. Enumerate all observation types like the legacy MVC editor and left-join existing
    /// values. Entities are materialized (not pure projection) because the benchmark/threshold unit
    /// labels parse the observation type's schema JSON via instance helpers.
    /// </summary>
    public static async Task<List<TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto>> ListWithObservationTypesByTreatmentBMPIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMP = await dbContext.TreatmentBMPs
            .AsNoTracking()
            .Include(x => x.TreatmentBMPType)
                .ThenInclude(x => x.TreatmentBMPTypeAssessmentObservationTypes)
                    .ThenInclude(x => x.TreatmentBMPAssessmentObservationType)
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID);

        var existingByObservationTypeID = await dbContext.TreatmentBMPBenchmarkAndThresholds
            .AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .ToDictionaryAsync(x => x.TreatmentBMPAssessmentObservationTypeID);

        var result = new List<TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto>();
        foreach (var typeObservationType in treatmentBMP.TreatmentBMPType.GetObservationTypesForAssessment())
        {
            var observationType = typeObservationType.TreatmentBMPAssessmentObservationType;
            if (!observationType.GetHasBenchmarkAndThreshold())
            {
                continue;
            }
            existingByObservationTypeID.TryGetValue(observationType.TreatmentBMPAssessmentObservationTypeID, out var existing);
            result.Add(new TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto
            {
                TreatmentBMPBenchmarkAndThresholdID = existing?.TreatmentBMPBenchmarkAndThresholdID,
                TreatmentBMPID = treatmentBMPID,
                TreatmentBMPTypeID = treatmentBMP.TreatmentBMPTypeID,
                TreatmentBMPTypeAssessmentObservationTypeID = typeObservationType.TreatmentBMPTypeAssessmentObservationTypeID,
                TreatmentBMPAssessmentObservationTypeID = observationType.TreatmentBMPAssessmentObservationTypeID,
                ObservationTypeName = observationType.TreatmentBMPAssessmentObservationTypeName,
                BenchmarkUnitLabel = observationType.BenchmarkMeasurementUnitLabel(),
                ThresholdUnitLabel = observationType.ThresholdMeasurementUnitLabel(),
                BenchmarkValue = existing?.BenchmarkValue,
                ThresholdValue = existing?.ThresholdValue,
            });
        }
        return result;
    }

    public static async Task<TreatmentBMPBenchmarkAndThresholdDto?> GetByIDAsync(NeptuneDbContext dbContext, int treatmentBMPBenchmarkAndThresholdID)
    {
        return await dbContext.TreatmentBMPBenchmarkAndThresholds
            .AsNoTracking()
            .Where(x => x.TreatmentBMPBenchmarkAndThresholdID == treatmentBMPBenchmarkAndThresholdID)
            .Select(TreatmentBMPBenchmarkAndThresholdDtoProjections.AsDto)
            .FirstOrDefaultAsync();
    }

    public static async Task<TreatmentBMPBenchmarkAndThresholdDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPBenchmarkAndThresholdUpsertDto dto)
    {
        var entity = dto.AsEntity(treatmentBMPID);
        dbContext.TreatmentBMPBenchmarkAndThresholds.Add(entity);
        await dbContext.SaveChangesAsync();
        return await dbContext.TreatmentBMPBenchmarkAndThresholds.AsNoTracking()
            .Where(x => x.TreatmentBMPBenchmarkAndThresholdID == entity.TreatmentBMPBenchmarkAndThresholdID)
            .Select(TreatmentBMPBenchmarkAndThresholdDtoProjections.AsDto)
            .SingleAsync();
    }

    public static async Task<TreatmentBMPBenchmarkAndThresholdDto?> UpdateAsync(NeptuneDbContext dbContext, int id, TreatmentBMPBenchmarkAndThresholdUpsertDto dto)
    {
        var entity = await dbContext.TreatmentBMPBenchmarkAndThresholds.FindAsync(id);
        if (entity == null)
        {
            return null;
        }
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await dbContext.TreatmentBMPBenchmarkAndThresholds.AsNoTracking()
            .Where(x => x.TreatmentBMPBenchmarkAndThresholdID == entity.TreatmentBMPBenchmarkAndThresholdID)
            .Select(TreatmentBMPBenchmarkAndThresholdDtoProjections.AsDto)
            .SingleAsync();
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int id)
    {
        var deletedCount = await dbContext.TreatmentBMPBenchmarkAndThresholds
            .Where(x => x.TreatmentBMPBenchmarkAndThresholdID == id)
            .ExecuteDeleteAsync();
        if (deletedCount == 0)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// One row's worth of default-value data, ready to be cloned onto a freshly-created
    /// TreatmentBMP. Decoupled from the TreatmentBMP so the caller can build the templates once
    /// per bulk upload (single DB round-trip) and stamp them onto every new BMP in the batch.
    /// </summary>
    public sealed record TreatmentBMPBenchmarkAndThresholdSeed(
        int TreatmentBMPTypeID,
        int TreatmentBMPTypeAssessmentObservationTypeID,
        int TreatmentBMPAssessmentObservationTypeID,
        double BenchmarkValue,
        double ThresholdValue);

    /// <summary>
    /// Build the set of default benchmark/threshold rows the BMP type would seed on a brand-new
    /// BMP. Ports <c>NewViewModel.UpdateModel</c> (Neptune.WebMvc/Views/TreatmentBMP/NewViewModel.cs:113-129).
    /// Uses focused queries instead of an Include() graph: one query for the type's observation-type
    /// join rows that have both defaults, then a single dictionary lookup of the referenced OT
    /// entities (needed for the schema-driven <c>GetHasBenchmarkAndThreshold()</c> check). NPT-1069.
    /// </summary>
    public static async Task<List<TreatmentBMPBenchmarkAndThresholdSeed>> BuildSeedTemplatesAsync(NeptuneDbContext dbContext, int treatmentBMPTypeID)
    {
        var joinRows = await dbContext.TreatmentBMPTypeAssessmentObservationTypes
            .AsNoTracking()
            .Where(x => x.TreatmentBMPTypeID == treatmentBMPTypeID
                     && x.DefaultBenchmarkValue.HasValue
                     && x.DefaultThresholdValue.HasValue)
            .ToListAsync();
        if (joinRows.Count == 0)
        {
            return new List<TreatmentBMPBenchmarkAndThresholdSeed>();
        }

        var observationTypeIDs = joinRows
            .Select(x => x.TreatmentBMPAssessmentObservationTypeID)
            .Distinct()
            .ToList();
        var observationTypes = await dbContext.TreatmentBMPAssessmentObservationTypes
            .AsNoTracking()
            .Where(x => observationTypeIDs.Contains(x.TreatmentBMPAssessmentObservationTypeID))
            .ToDictionaryAsync(x => x.TreatmentBMPAssessmentObservationTypeID);

        return joinRows
            .Where(j => observationTypes.TryGetValue(j.TreatmentBMPAssessmentObservationTypeID, out var ot) && ot.GetHasBenchmarkAndThreshold())
            .Select(j => new TreatmentBMPBenchmarkAndThresholdSeed(
                TreatmentBMPTypeID: treatmentBMPTypeID,
                TreatmentBMPTypeAssessmentObservationTypeID: j.TreatmentBMPTypeAssessmentObservationTypeID,
                TreatmentBMPAssessmentObservationTypeID: j.TreatmentBMPAssessmentObservationTypeID,
                BenchmarkValue: j.DefaultBenchmarkValue!.Value,
                ThresholdValue: j.DefaultThresholdValue!.Value))
            .ToList();
    }

    /// <summary>
    /// Attach one TreatmentBMPBenchmarkAndThreshold row per template to the BMP's navigation
    /// collection. The BMP must be tracked (or about to be added); EF cascades the inserts via
    /// the navigation when SaveChangesAsync runs on the parent context.
    /// </summary>
    public static void AttachSeedsToBMP(TreatmentBMP treatmentBMP, IEnumerable<TreatmentBMPBenchmarkAndThresholdSeed> templates)
    {
        foreach (var template in templates)
        {
            treatmentBMP.TreatmentBMPBenchmarkAndThresholds.Add(new TreatmentBMPBenchmarkAndThreshold
            {
                TreatmentBMP = treatmentBMP,
                TreatmentBMPTypeID = template.TreatmentBMPTypeID,
                TreatmentBMPTypeAssessmentObservationTypeID = template.TreatmentBMPTypeAssessmentObservationTypeID,
                TreatmentBMPAssessmentObservationTypeID = template.TreatmentBMPAssessmentObservationTypeID,
                BenchmarkValue = template.BenchmarkValue,
                ThresholdValue = template.ThresholdValue,
            });
        }
    }
}