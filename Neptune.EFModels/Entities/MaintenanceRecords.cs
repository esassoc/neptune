using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class MaintenanceRecords
{
    private static IQueryable<MaintenanceRecord> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.MaintenanceRecords
            .Include(x => x.FieldVisit)
                .ThenInclude(x => x.PerformedByPerson)
                .ThenInclude(x => x.Organization)
            .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
            .Include(x => x.TreatmentBMPType)
                .ThenInclude(x => x.TreatmentBMPTypeCustomAttributeTypes)
                .ThenInclude(x => x.CustomAttributeType)
            .Include(x => x.MaintenanceRecordObservations)
                .ThenInclude(x => x.MaintenanceRecordObservationValues)
            .Include(x => x.MaintenanceRecordObservations)
                .ThenInclude(x => x.CustomAttributeType)
            ;
    }

    public static MaintenanceRecord GetByIDWithChangeTracking(NeptuneDbContext dbContext, int maintenanceRecordID)
    {
        var maintenanceRecord = GetImpl(dbContext)
            .SingleOrDefault(x => x.MaintenanceRecordID == maintenanceRecordID);
        Check.RequireNotNull(maintenanceRecord, $"MaintenanceRecord with ID {maintenanceRecordID} not found!");
        return maintenanceRecord;
    }

    public static MaintenanceRecord GetByIDWithChangeTracking(NeptuneDbContext dbContext, MaintenanceRecordPrimaryKey maintenanceRecordPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, maintenanceRecordPrimaryKey.PrimaryKeyValue);
    }

    public static MaintenanceRecord GetByID(NeptuneDbContext dbContext, int maintenanceRecordID)
    {
        var maintenanceRecord = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.MaintenanceRecordID == maintenanceRecordID);
        Check.RequireNotNull(maintenanceRecord, $"MaintenanceRecord with ID {maintenanceRecordID} not found!");
        return maintenanceRecord;
    }

    public static MaintenanceRecord GetByID(NeptuneDbContext dbContext, MaintenanceRecordPrimaryKey maintenanceRecordPrimaryKey)
    {
        return GetByID(dbContext, maintenanceRecordPrimaryKey.PrimaryKeyValue);
    }

    public static List<MaintenanceRecord> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList();
    }

    public static MaintenanceRecord? GetByFieldVisitIDWithChangeTracking(NeptuneDbContext dbContext, int fieldVisitID)
    {
        return GetImpl(dbContext).SingleOrDefault(x => x.FieldVisitID == fieldVisitID);
    }

    /// <summary>
    /// Resolves MaintenanceRecordType.MaintenanceRecordTypeDisplayName post-materialize
    /// (it's a static C# lookup, not an EF-mapped join).
    /// </summary>
    private static MaintenanceRecordDetailDto ResolveLookupDisplayNames(MaintenanceRecordDetailDto dto)
    {
        if (dto.MaintenanceRecordTypeID.HasValue
            && MaintenanceRecordType.AllLookupDictionary.TryGetValue(dto.MaintenanceRecordTypeID.Value, out var type))
        {
            dto.MaintenanceRecordTypeDisplayName = type.MaintenanceRecordTypeDisplayName;
        }
        return dto;
    }

    public static async Task<MaintenanceRecordDetailDto?> GetByIDAsDetailDtoAsync(NeptuneDbContext dbContext, int maintenanceRecordID)
    {
        var dto = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Where(x => x.MaintenanceRecordID == maintenanceRecordID)
            .Select(MaintenanceRecordProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        return dto == null ? null : ResolveLookupDisplayNames(dto);
    }

    public static async Task<MaintenanceRecordDetailDto?> GetByFieldVisitIDAsDetailDtoAsync(NeptuneDbContext dbContext, int fieldVisitID)
    {
        var dto = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Where(x => x.FieldVisitID == fieldVisitID)
            .Select(MaintenanceRecordProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        return dto == null ? null : ResolveLookupDisplayNames(dto);
    }

    public static List<MaintenanceRecordGridDto> ListAsGridDtoForJurisdictions(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        return dbContext.vMaintenanceRecordDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
            .OrderByDescending(x => x.VisitDate)
            .ToList()
            .Select(x => new MaintenanceRecordGridDto
            {
                MaintenanceRecordID = x.MaintenanceRecordID,
                FieldVisitID = x.FieldVisitID,
                TreatmentBMPID = x.TreatmentBMPID,
                TreatmentBMPName = x.TreatmentBMPName,
                TreatmentBMPTypeID = x.TreatmentBMPTypeID,
                VisitDate = x.VisitDate,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                StormwaterJurisdictionName = x.StormwaterJurisdictionName,
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = x.WaterQualityManagementPlanName,
                PerformedByPersonID = x.PerformedByPersonID,
                PerformedByPersonName = x.PerformedByPersonName,
                MaintenanceRecordTypeID = x.MaintenanceRecordTypeID,
                MaintenanceRecordTypeDisplayName = x.MaintenanceRecordTypeDisplayName,
                MaintenanceRecordDescription = x.MaintenanceRecordDescription,
                IsFieldVisitVerified = x.IsFieldVisitVerified,
                StructuralRepairConducted = x.Structural_Repair_Conducted,
                MechanicalRepairConducted = x.Mechanical_Repair_Conducted,
                InfiltrationSurfaceRestored = x.Infiltration_Surface_Restored,
                FiltrationSurfaceRestored = x.Filtration_Surface_Restored,
                MediaReplaced = x.Media_Replaced,
                MulchAdded = x.Mulch_Added,
                PercentTrash = x.Percent_Trash,
                PercentGreenWaste = x.Percent_Green_Waste,
                PercentSediment = x.Percent_Sediment,
                AreaReseeded = x.Area_Reseeded,
                VegetationPlanted = x.Vegetation_Planted,
                SurfaceAndBankErosionRepaired = x.Surface_and_Bank_Erosion_Repaired,
                TotalMaterialVolumeRemovedCubicFeet = x.Total_Material_Volume_Removed__cu_ft_,
                TotalMaterialVolumeRemovedGallons = x.Total_Material_Volume_Removed__gal_,
            })
            .ToList();
    }

    public static async Task<MaintenanceRecordDetailDto> CreateForFieldVisitAsync(NeptuneDbContext dbContext, int fieldVisitID, int callingPersonID)
    {
        var fieldVisit = FieldVisits.GetByIDWithChangeTracking(dbContext, fieldVisitID);

        var existing = GetByFieldVisitIDWithChangeTracking(dbContext, fieldVisitID);
        if (existing != null)
        {
            return (await GetByIDAsDetailDtoAsync(dbContext, existing.MaintenanceRecordID))!;
        }

        fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(People.GetByID(dbContext, callingPersonID));

        var record = new MaintenanceRecord
        {
            FieldVisitID = fieldVisitID,
            TreatmentBMPID = fieldVisit.TreatmentBMPID,
            TreatmentBMPTypeID = fieldVisit.TreatmentBMP.TreatmentBMPTypeID,
        };
        await dbContext.MaintenanceRecords.AddAsync(record);
        await dbContext.SaveChangesAsync();
        return (await GetByIDAsDetailDtoAsync(dbContext, record.MaintenanceRecordID))!;
    }

    public static async Task<MaintenanceRecordDetailDto> UpdateAsync(NeptuneDbContext dbContext, int maintenanceRecordID, MaintenanceRecordUpsertDto upsertDto, int callingPersonID)
    {
        var record = GetByIDWithChangeTracking(dbContext, maintenanceRecordID);
        var fieldVisit = FieldVisits.GetByIDWithChangeTracking(dbContext, record.FieldVisitID);
        fieldVisit.MarkFieldVisitAsProvisionalIfNonManager(People.GetByID(dbContext, callingPersonID));

        record.MaintenanceRecordTypeID = upsertDto.MaintenanceRecordTypeID;
        record.MaintenanceRecordDescription = upsertDto.MaintenanceRecordDescription;

        // Replace observations transactionally: delete existing values + observations, then re-create
        await dbContext.MaintenanceRecordObservationValues
            .Where(v => v.MaintenanceRecordObservation.MaintenanceRecordID == maintenanceRecordID)
            .ExecuteDeleteAsync();
        await dbContext.MaintenanceRecordObservations
            .Where(o => o.MaintenanceRecordID == maintenanceRecordID)
            .ExecuteDeleteAsync();

        // Refresh the tracked record after ExecuteDeleteAsync
        await dbContext.Entry(record).Collection(x => x.MaintenanceRecordObservations).LoadAsync();

        var typeCustomAttributeTypes = record.TreatmentBMPType.TreatmentBMPTypeCustomAttributeTypes.ToList();

        foreach (var observationUpsert in upsertDto.Observations)
        {
            var typeCustomAttributeType = typeCustomAttributeTypes
                .SingleOrDefault(x => x.CustomAttributeTypeID == observationUpsert.CustomAttributeTypeID);
            if (typeCustomAttributeType == null)
            {
                continue;
            }

            var observation = new MaintenanceRecordObservation
            {
                MaintenanceRecordID = maintenanceRecordID,
                TreatmentBMPTypeCustomAttributeTypeID = typeCustomAttributeType.TreatmentBMPTypeCustomAttributeTypeID,
                TreatmentBMPTypeID = record.TreatmentBMPTypeID,
                CustomAttributeTypeID = observationUpsert.CustomAttributeTypeID,
            };
            await dbContext.MaintenanceRecordObservations.AddAsync(observation);
            await dbContext.SaveChangesAsync();

            foreach (var value in observationUpsert.Values.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                await dbContext.MaintenanceRecordObservationValues.AddAsync(new MaintenanceRecordObservationValue
                {
                    MaintenanceRecordObservationID = observation.MaintenanceRecordObservationID,
                    ObservationValue = value,
                });
            }
        }

        await dbContext.SaveChangesAsync();
        return (await GetByIDAsDetailDtoAsync(dbContext, maintenanceRecordID))!;
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int maintenanceRecordID)
    {
        var record = GetByIDWithChangeTracking(dbContext, maintenanceRecordID);
        await record.DeleteFull(dbContext);
    }
}
