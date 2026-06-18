using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessmentObservationTypes
{
    private static IQueryable<TreatmentBMPAssessmentObservationType> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPAssessmentObservationTypes
            .Include(x => x.TreatmentBMPTypeAssessmentObservationTypes)
            .ThenInclude(x => x.TreatmentBMPType)
            ;
    }

    public static TreatmentBMPAssessmentObservationType GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPAssessmentObservationTypeID)
    {
        var treatmentBMPAssessmentObservationType = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPAssessmentObservationTypeID == treatmentBMPAssessmentObservationTypeID);
        Check.RequireNotNull(treatmentBMPAssessmentObservationType, $"TreatmentBMPAssessmentObservationType with ID {treatmentBMPAssessmentObservationTypeID} not found!");
        return treatmentBMPAssessmentObservationType;
    }

    public static async Task<List<TreatmentBMPAssessmentObservationTypeGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext)
    {
        var dtos = await dbContext.TreatmentBMPAssessmentObservationTypes
            .AsNoTracking()
            .OrderBy(x => x.TreatmentBMPAssessmentObservationTypeName)
            .Select(TreatmentBMPAssessmentObservationTypeProjections.AsGridDto)
            .ToListAsync();

        foreach (var dto in dtos)
        {
            if (ObservationTypeSpecification.AllLookupDictionary.TryGetValue(dto.ObservationTypeSpecificationID, out var spec))
            {
                dto.ObservationTypeCollectionMethodDisplayName = spec.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodDisplayName;
                dto.ObservationTargetTypeDisplayName = spec.ObservationTargetType.ObservationTargetTypeDisplayName;
                dto.ObservationThresholdTypeDisplayName = spec.ObservationThresholdType.ObservationThresholdTypeDisplayName;
            }
        }
        return dtos;
    }

    public static async Task<TreatmentBMPAssessmentObservationTypeDetailDto> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int id)
    {
        var dto = await dbContext.TreatmentBMPAssessmentObservationTypes
            .AsNoTracking()
            .Where(x => x.TreatmentBMPAssessmentObservationTypeID == id)
            .Select(TreatmentBMPAssessmentObservationTypeProjections.AsDetailDto)
            .SingleOrDefaultAsync();

        if (dto == null) return null;

        if (ObservationTypeSpecification.AllLookupDictionary.TryGetValue(dto.ObservationTypeSpecificationID, out var spec))
        {
            dto.ObservationTypeCollectionMethodDisplayName = spec.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodDisplayName;
            dto.ObservationTargetTypeDisplayName = spec.ObservationTargetType.ObservationTargetTypeDisplayName;
            dto.ObservationThresholdTypeDisplayName = spec.ObservationThresholdType.ObservationThresholdTypeDisplayName;
        }
        return dto;
    }

    public static async Task<TreatmentBMPAssessmentObservationTypeDetailDto> CreateAsync(NeptuneDbContext dbContext, TreatmentBMPAssessmentObservationTypeUpsertDto dto)
    {
        var entity = new TreatmentBMPAssessmentObservationType
        {
            TreatmentBMPAssessmentObservationTypeName = dto.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID = dto.ObservationTypeSpecificationID,
            TreatmentBMPAssessmentObservationTypeSchema = dto.TreatmentBMPAssessmentObservationTypeSchema,
        };
        dbContext.TreatmentBMPAssessmentObservationTypes.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.TreatmentBMPAssessmentObservationTypeID);
    }

    public static async Task<TreatmentBMPAssessmentObservationTypeDetailDto> UpdateAsync(NeptuneDbContext dbContext, int id, TreatmentBMPAssessmentObservationTypeUpsertDto dto)
    {
        var entity = GetByIDWithChangeTracking(dbContext, id);
        entity.TreatmentBMPAssessmentObservationTypeName = dto.TreatmentBMPAssessmentObservationTypeName;
        entity.ObservationTypeSpecificationID = dto.ObservationTypeSpecificationID;
        entity.TreatmentBMPAssessmentObservationTypeSchema = dto.TreatmentBMPAssessmentObservationTypeSchema;
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, id);
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int id)
    {
        var entity = await dbContext.TreatmentBMPAssessmentObservationTypes
            .Include(x => x.TreatmentBMPBenchmarkAndThresholds)
            .Include(x => x.TreatmentBMPObservations)
            .Include(x => x.TreatmentBMPTypeAssessmentObservationTypes)
            .SingleAsync(x => x.TreatmentBMPAssessmentObservationTypeID == id);

        dbContext.TreatmentBMPBenchmarkAndThresholds.RemoveRange(entity.TreatmentBMPBenchmarkAndThresholds);
        dbContext.TreatmentBMPObservations.RemoveRange(entity.TreatmentBMPObservations);
        dbContext.TreatmentBMPTypeAssessmentObservationTypes.RemoveRange(entity.TreatmentBMPTypeAssessmentObservationTypes);
        dbContext.TreatmentBMPAssessmentObservationTypes.Remove(entity);
        await dbContext.SaveChangesAsync();
    }
}