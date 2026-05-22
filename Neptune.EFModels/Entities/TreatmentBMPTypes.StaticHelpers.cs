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

        await ResolveLookupDisplayNamesAsync(dbContext, new[] { dto });
        return dto;
    }

    /// <summary>
    /// Detail DTOs for every BMP type, sorted by name. Used by the public card-list page —
    /// the front end needs the full shape (OT relationship rows + CA list) per card.
    /// One materialization pass + one lookup-resolution pass; no N+1.
    /// </summary>
    public static async Task<List<TreatmentBMPTypeDetailDto>> ListAsDetailDtoAsync(NeptuneDbContext dbContext)
    {
        var dtos = await dbContext.TreatmentBMPTypes
            .AsNoTracking()
            .OrderBy(x => x.TreatmentBMPTypeName)
            .Select(TreatmentBMPTypeProjections.AsDetailDto)
            .ToListAsync();

        await ResolveLookupDisplayNamesAsync(dbContext, dtos);
        return dtos;
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
        // DeleteFull does its own ExecuteDeleteAsync queries by TreatmentBMPTypeID, so we don't
        // need to eagerly load child collections — just need an entity instance to call the
        // (instance) method on, which does double duty as a not-found check.
        var entity = await dbContext.TreatmentBMPTypes
            .SingleAsync(x => x.TreatmentBMPTypeID == treatmentBMPTypeID);

        await entity.DeleteFull(dbContext);
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

    /// <summary>
    /// Cached per-OT spec-driven labels — populated once per OT entity to avoid re-parsing the
    /// schema JSON across every row that references the same OT.
    /// </summary>
    private sealed record ObservationTypeStaticLabels(
        bool HasBenchmarkAndThreshold,
        string BenchmarkUnitDisplayName,
        string ThresholdUnitDisplayName);

    /// <summary>
    /// Post-materialize lookup resolution for TreatmentBMPType detail DTOs. Loads the related
    /// TreatmentBMPAssessmentObservationType entities once (with their schema JSON) and reuses
    /// their existing helper methods (BenchmarkMeasurementUnitLabel / ThresholdMeasurementUnitLabel /
    /// GetFormattedBenchmarkValue / GetFormattedThresholdValue) so the SPA gets the same display
    /// strings the legacy MVC pages compute.
    /// </summary>
    public static async Task ResolveLookupDisplayNamesAsync(NeptuneDbContext dbContext, IReadOnlyCollection<TreatmentBMPTypeDetailDto> dtos)
    {
        if (dtos.Count == 0) return;

        var observationTypeIDs = dtos.SelectMany(d => d.ObservationTypes)
            .Select(o => o.TreatmentBMPAssessmentObservationTypeID)
            .Distinct()
            .ToList();
        var observationTypeEntities = await dbContext.TreatmentBMPAssessmentObservationTypes
            .AsNoTracking()
            .Where(x => observationTypeIDs.Contains(x.TreatmentBMPAssessmentObservationTypeID))
            .ToDictionaryAsync(x => x.TreatmentBMPAssessmentObservationTypeID);

        // Pre-compute the spec-driven static labels once per OT entity. The unit-label methods
        // each parse the OT's schema JSON on every call; doing it once here keeps the per-row
        // hot loop O(1) on the cached dictionary instead of O(parses) on each row.
        var staticLabelsByOTID = observationTypeEntities.ToDictionary(
            kvp => kvp.Key,
            kvp => new ObservationTypeStaticLabels(
                HasBenchmarkAndThreshold: kvp.Value.GetHasBenchmarkAndThreshold(),
                BenchmarkUnitDisplayName: kvp.Value.BenchmarkMeasurementUnitLabel(),
                ThresholdUnitDisplayName: kvp.Value.ThresholdMeasurementUnitLabel()));

        foreach (var dto in dtos)
        {
            foreach (var ot in dto.ObservationTypes)
            {
                if (ObservationTypeSpecification.AllLookupDictionary.TryGetValue(ot.ObservationTypeSpecificationID, out var spec))
                {
                    ot.ObservationTypeCollectionMethodDisplayName = spec.ObservationTypeCollectionMethod.ObservationTypeCollectionMethodDisplayName;
                    ot.ObservationTypeCollectionMethodID = spec.ObservationTypeCollectionMethodID;
                    ot.ObservationTargetTypeID = spec.ObservationTargetTypeID;
                    ot.ObservationThresholdTypeID = spec.ObservationThresholdTypeID;
                }

                if (staticLabelsByOTID.TryGetValue(ot.TreatmentBMPAssessmentObservationTypeID, out var labels))
                {
                    ot.HasBenchmarkAndThreshold = labels.HasBenchmarkAndThreshold;
                    ot.BenchmarkUnitDisplayName = labels.BenchmarkUnitDisplayName;
                    ot.ThresholdUnitDisplayName = labels.ThresholdUnitDisplayName;
                }

                // Formatted values depend on this row's Default Benchmark/Threshold values, so
                // they vary per row and can't share a cached result. The internal schema parses
                // happen only here.
                if (observationTypeEntities.TryGetValue(ot.TreatmentBMPAssessmentObservationTypeID, out var otEntity))
                {
                    ot.FormattedBenchmarkValue = otEntity.GetFormattedBenchmarkValue(ot.DefaultBenchmarkValue);
                    ot.FormattedThresholdValue = otEntity.GetFormattedThresholdValue(ot.DefaultThresholdValue, ot.DefaultBenchmarkValue);
                }
            }

            foreach (var cat in dto.CustomAttributeTypes)
            {
                if (CustomAttributeTypePurpose.AllLookupDictionary.TryGetValue(cat.CustomAttributeTypePurposeID, out var purpose))
                {
                    cat.CustomAttributeTypePurposeDisplayName = purpose.CustomAttributeTypePurposeDisplayName;
                }
                if (CustomAttributeDataType.AllLookupDictionary.TryGetValue(cat.CustomAttributeDataTypeID, out var dataType))
                {
                    cat.CustomAttributeDataTypeDisplayName = dataType.CustomAttributeDataTypeDisplayName;
                }
                if (cat.MeasurementUnitTypeID.HasValue && MeasurementUnitType.AllLookupDictionary.TryGetValue(cat.MeasurementUnitTypeID.Value, out var unit))
                {
                    cat.MeasurementUnitDisplayName = unit.MeasurementUnitTypeDisplayName;
                }
            }
        }
    }
}
