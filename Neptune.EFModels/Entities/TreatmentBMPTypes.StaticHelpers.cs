using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPTypesAdmin
{
    public static async Task<List<TreatmentBMPTypeGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.TreatmentBMPTypes
            .AsNoTracking()
            .OrderBy(x => x.TreatmentBMPTypeName)
            .Select(TreatmentBMPTypeProjections.AsGridDto)
            .ToListAsync();
    }

    public static async Task<TreatmentBMPTypeDetailDto> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPTypeID)
    {
        var dto = await dbContext.TreatmentBMPTypes
            .AsNoTracking()
            .Where(x => x.TreatmentBMPTypeID == treatmentBMPTypeID)
            .Select(TreatmentBMPTypeProjections.AsDetailDto)
            .SingleOrDefaultAsync();

        if (dto == null) return null;

        // Resolve lookup display names from static dictionaries
        foreach (var ot in dto.ObservationTypes)
        {
            if (ObservationTypeSpecification.AllLookupDictionary.TryGetValue(ot.ObservationTypeSpecificationID, out var spec))
            {
                ot.ObservationTypeCollectionMethodDisplayName = spec.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodDisplayName;
            }
        }
        foreach (var cat in dto.CustomAttributeTypes)
        {
            if (CustomAttributeTypePurpose.AllLookupDictionary.TryGetValue(cat.CustomAttributeTypePurposeID, out var purpose))
            {
                cat.CustomAttributeTypePurposeDisplayName = purpose.CustomAttributeTypePurposeDisplayName;
            }
        }
        return dto;
    }

    public static async Task<TreatmentBMPTypeDetailDto> CreateAsync(NeptuneDbContext dbContext, TreatmentBMPTypeUpsertDto dto)
    {
        var entity = new TreatmentBMPType
        {
            TreatmentBMPTypeName = dto.TreatmentBMPTypeName,
            TreatmentBMPTypeDescription = dto.TreatmentBMPTypeDescription,
            IsAnalyzedInModelingModule = false,
        };
        dbContext.TreatmentBMPTypes.Add(entity);
        await dbContext.SaveChangesAsync();

        await SaveChildRecordsAsync(dbContext, entity.TreatmentBMPTypeID, dto);
        return await GetByIDAsDtoAsync(dbContext, entity.TreatmentBMPTypeID);
    }

    public static async Task<TreatmentBMPTypeDetailDto> UpdateAsync(NeptuneDbContext dbContext, int treatmentBMPTypeID, TreatmentBMPTypeUpsertDto dto)
    {
        var entity = await dbContext.TreatmentBMPTypes
            .SingleAsync(x => x.TreatmentBMPTypeID == treatmentBMPTypeID);

        entity.TreatmentBMPTypeName = dto.TreatmentBMPTypeName;
        entity.TreatmentBMPTypeDescription = dto.TreatmentBMPTypeDescription;
        await dbContext.SaveChangesAsync();

        // Delete-and-recreate child relationship records
        dbContext.TreatmentBMPTypeAssessmentObservationTypes.RemoveRange(
            dbContext.TreatmentBMPTypeAssessmentObservationTypes.Where(x => x.TreatmentBMPTypeID == treatmentBMPTypeID));
        dbContext.TreatmentBMPTypeCustomAttributeTypes.RemoveRange(
            dbContext.TreatmentBMPTypeCustomAttributeTypes.Where(x => x.TreatmentBMPTypeID == treatmentBMPTypeID));
        await dbContext.SaveChangesAsync();

        await SaveChildRecordsAsync(dbContext, treatmentBMPTypeID, dto);
        return await GetByIDAsDtoAsync(dbContext, treatmentBMPTypeID);
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int treatmentBMPTypeID)
    {
        var entity = await dbContext.TreatmentBMPTypes
            .Include(x => x.TreatmentBMPTypeAssessmentObservationTypes)
            .Include(x => x.TreatmentBMPTypeCustomAttributeTypes)
            .SingleAsync(x => x.TreatmentBMPTypeID == treatmentBMPTypeID);

        entity.DeleteFull(dbContext);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SaveChildRecordsAsync(NeptuneDbContext dbContext, int treatmentBMPTypeID, TreatmentBMPTypeUpsertDto dto)
    {
        if (dto.ObservationTypes?.Any() == true)
        {
            dbContext.TreatmentBMPTypeAssessmentObservationTypes.AddRange(dto.ObservationTypes.Select(x => new TreatmentBMPTypeAssessmentObservationType
            {
                TreatmentBMPTypeID = treatmentBMPTypeID,
                TreatmentBMPAssessmentObservationTypeID = x.TreatmentBMPAssessmentObservationTypeID,
                AssessmentScoreWeight = x.AssessmentScoreWeight,
                DefaultThresholdValue = x.DefaultThresholdValue,
                DefaultBenchmarkValue = x.DefaultBenchmarkValue,
                OverrideAssessmentScoreIfFailing = x.OverrideAssessmentScoreIfFailing,
                SortOrder = x.SortOrder,
            }));
        }

        if (dto.CustomAttributeTypes?.Any() == true)
        {
            dbContext.TreatmentBMPTypeCustomAttributeTypes.AddRange(dto.CustomAttributeTypes.Select(x => new TreatmentBMPTypeCustomAttributeType
            {
                TreatmentBMPTypeID = treatmentBMPTypeID,
                CustomAttributeTypeID = x.CustomAttributeTypeID,
                SortOrder = x.SortOrder,
            }));
        }

        await dbContext.SaveChangesAsync();
    }
}
