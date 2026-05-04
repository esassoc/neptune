using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanVerifies
{
    public static IQueryable<WaterQualityManagementPlanVerify> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanVerifies
            .Include(x => x.WaterQualityManagementPlan)
            .ThenInclude(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.LastEditedByPerson)
            .Include(x => x.FileResource)
            ;
    }

    public static WaterQualityManagementPlanVerify GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanVerifyID)
    {
        var waterQualityManagementPlanVerify = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID);
        Check.RequireNotNull(waterQualityManagementPlanVerify,
            $"WaterQualityManagementPlanVerify with ID {waterQualityManagementPlanVerifyID} not found!");
        return waterQualityManagementPlanVerify;
    }

    public static WaterQualityManagementPlanVerify GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, waterQualityManagementPlanVerifyPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlanVerify GetByID(NeptuneDbContext dbContext,
        int waterQualityManagementPlanVerifyID)
    {
        var waterQualityManagementPlanVerify = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID);
        Check.RequireNotNull(waterQualityManagementPlanVerify,
            $"WaterQualityManagementPlanVerify with ID {waterQualityManagementPlanVerifyID} not found!");
        return waterQualityManagementPlanVerify;
    }

    public static WaterQualityManagementPlanVerify GetByID(NeptuneDbContext dbContext,
        WaterQualityManagementPlanVerifyPrimaryKey waterQualityManagementPlanVerifyPrimaryKey)
    {
        return GetByID(dbContext, waterQualityManagementPlanVerifyPrimaryKey.PrimaryKeyValue);
    }

    public static List<WaterQualityManagementPlanVerify> ListViewable(NeptuneDbContext dbContext,
        IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.WaterQualityManagementPlan
                .StormwaterJurisdictionID))
            .OrderBy(x => x.WaterQualityManagementPlan.StormwaterJurisdiction.Organization.OrganizationName)
            .ThenBy(x => x.WaterQualityManagementPlan.WaterQualityManagementPlanName)
            .ThenByDescending(x => x.LastEditedDate).ToList();
    }

    public static List<WaterQualityManagementPlanVerify> ListByWaterQualityManagementPlanID(NeptuneDbContext dbContext,
        int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .OrderByDescending(x => x.VerificationDate).ToList();
    }

    public static async Task<List<WaterQualityManagementPlanVerifyIndexGridDto>> ListAllAsIndexGridDtoAsync(
        NeptuneDbContext dbContext, PersonDto callingPerson)
    {
        var viewableJurisdictionIDs = await StormwaterJurisdictionPeople
            .ListViewableStormwaterJurisdictionIDsByPersonDtoForBMPsAsync(dbContext, callingPerson);

        var dtos = await dbContext.WaterQualityManagementPlanVerifies
            .AsNoTracking()
            .Where(x => viewableJurisdictionIDs.Contains(x.WaterQualityManagementPlan.StormwaterJurisdictionID))
            .OrderByDescending(x => x.VerificationDate)
            .Select(WaterQualityManagementPlanVerifyProjections.AsIndexGridDto)
            .ToListAsync();

        // Resolve lookup display names from static binding dictionaries (not available in LINQ-to-SQL)
        foreach (var dto in dtos)
        {
            if (WaterQualityManagementPlanVerifyType.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyTypeID, out var verifyType))
            {
                dto.WaterQualityManagementPlanVerifyTypeDisplayName = verifyType.WaterQualityManagementPlanVerifyTypeDisplayName;
            }
            if (WaterQualityManagementPlanVisitStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVisitStatusID, out var visitStatus))
            {
                dto.WaterQualityManagementPlanVisitStatusDisplayName = visitStatus.WaterQualityManagementPlanVisitStatusDisplayName;
            }
            if (dto.WaterQualityManagementPlanVerifyStatusID.HasValue
                && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyStatusID.Value, out var verifyStatus))
            {
                dto.WaterQualityManagementPlanVerifyStatusDisplayName = verifyStatus.WaterQualityManagementPlanVerifyStatusDisplayName;
            }
        }

        return dtos;
    }

    public static async Task<List<WaterQualityManagementPlanVerifyGridDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var dtos = await dbContext.WaterQualityManagementPlanVerifies
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .OrderByDescending(x => x.VerificationDate)
            .Select(WaterQualityManagementPlanVerifyProjections.AsGridDto)
            .ToListAsync();

        // Resolve lookup display names from static binding dictionaries (not available in LINQ-to-SQL)
        foreach (var dto in dtos)
        {
            if (WaterQualityManagementPlanVerifyType.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyTypeID, out var verifyType))
            {
                dto.WaterQualityManagementPlanVerifyTypeDisplayName = verifyType.WaterQualityManagementPlanVerifyTypeDisplayName;
            }
            if (WaterQualityManagementPlanVisitStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVisitStatusID, out var visitStatus))
            {
                dto.WaterQualityManagementPlanVisitStatusDisplayName = visitStatus.WaterQualityManagementPlanVisitStatusDisplayName;
            }
            if (dto.WaterQualityManagementPlanVerifyStatusID.HasValue
                && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyStatusID.Value, out var verifyStatus))
            {
                dto.WaterQualityManagementPlanVerifyStatusDisplayName = verifyStatus.WaterQualityManagementPlanVerifyStatusDisplayName;
            }
        }

        return dtos;
    }

    public static async Task<WaterQualityManagementPlanVerifyDetailDto> GetByIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyID)
    {
        var dto = await dbContext.WaterQualityManagementPlanVerifies
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID)
            .Select(WaterQualityManagementPlanVerifyProjections.AsDetailDto)
            .SingleOrDefaultAsync();

        if (dto == null) return null;

        ResolveLookupDisplayNames(dto);
        return dto;
    }

    // Resolves lookup display names from static binding dictionaries (not available in LINQ-to-SQL projections)
    private static void ResolveLookupDisplayNames(WaterQualityManagementPlanVerifyDetailDto dto)
    {
        if (WaterQualityManagementPlanVerifyType.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyTypeID, out var verifyType))
        {
            dto.WaterQualityManagementPlanVerifyTypeDisplayName = verifyType.WaterQualityManagementPlanVerifyTypeDisplayName;
        }
        if (WaterQualityManagementPlanVisitStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVisitStatusID, out var visitStatus))
        {
            dto.WaterQualityManagementPlanVisitStatusDisplayName = visitStatus.WaterQualityManagementPlanVisitStatusDisplayName;
        }
        if (dto.WaterQualityManagementPlanVerifyStatusID.HasValue
            && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(dto.WaterQualityManagementPlanVerifyStatusID.Value, out var verifyStatus))
        {
            dto.WaterQualityManagementPlanVerifyStatusDisplayName = verifyStatus.WaterQualityManagementPlanVerifyStatusDisplayName;
        }
        foreach (var sc in dto.SourceControlBMPs)
        {
            if (SourceControlBMPAttributeCategory.AllLookupDictionary.TryGetValue(sc.SourceControlBMPAttributeCategoryID, out var category))
            {
                sc.SourceControlBMPAttributeCategoryName = category.SourceControlBMPAttributeCategoryName;
            }
        }
    }

    public static async Task<WaterQualityManagementPlanVerifyDetailDto> CreateAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID,
        WaterQualityManagementPlanVerifyUpsertDto dto, int callingUserPersonID)
    {
        var verify = new WaterQualityManagementPlanVerify
        {
            WaterQualityManagementPlanID = waterQualityManagementPlanID,
            WaterQualityManagementPlanVerifyTypeID = dto.WaterQualityManagementPlanVerifyTypeID,
            WaterQualityManagementPlanVisitStatusID = dto.WaterQualityManagementPlanVisitStatusID,
            WaterQualityManagementPlanVerifyStatusID = dto.WaterQualityManagementPlanVerifyStatusID,
            VerificationDate = dto.VerificationDate,
            SourceControlCondition = dto.SourceControlCondition,
            EnforcementOrFollowupActions = dto.EnforcementOrFollowupActions,
            IsDraft = dto.IsDraft,
            LastEditedByPersonID = callingUserPersonID,
            LastEditedDate = DateTime.UtcNow,
        };
        dbContext.WaterQualityManagementPlanVerifies.Add(verify);
        await dbContext.SaveChangesAsync();

        await SaveChildRecordsAsync(dbContext, verify.WaterQualityManagementPlanVerifyID, dto);

        return await GetByIDAsDtoAsync(dbContext, verify.WaterQualityManagementPlanVerifyID);
    }

    public static async Task<WaterQualityManagementPlanVerifyDetailDto> UpdateAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyID,
        WaterQualityManagementPlanVerifyUpsertDto dto, int callingUserPersonID)
    {
        var verify = await dbContext.WaterQualityManagementPlanVerifies
            .SingleAsync(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID);

        verify.WaterQualityManagementPlanVerifyTypeID = dto.WaterQualityManagementPlanVerifyTypeID;
        verify.WaterQualityManagementPlanVisitStatusID = dto.WaterQualityManagementPlanVisitStatusID;
        verify.WaterQualityManagementPlanVerifyStatusID = dto.WaterQualityManagementPlanVerifyStatusID;
        verify.VerificationDate = dto.VerificationDate;
        verify.SourceControlCondition = dto.SourceControlCondition;
        verify.EnforcementOrFollowupActions = dto.EnforcementOrFollowupActions;
        verify.IsDraft = dto.IsDraft;
        verify.LastEditedByPersonID = callingUserPersonID;
        verify.LastEditedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        // Delete-and-recreate child records
        dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs.RemoveRange(
            dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs.Where(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID));
        dbContext.WaterQualityManagementPlanVerifyQuickBMPs.RemoveRange(
            dbContext.WaterQualityManagementPlanVerifyQuickBMPs.Where(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID));
        dbContext.WaterQualityManagementPlanVerifySourceControlBMPs.RemoveRange(
            dbContext.WaterQualityManagementPlanVerifySourceControlBMPs.Where(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID));
        await dbContext.SaveChangesAsync();

        await SaveChildRecordsAsync(dbContext, waterQualityManagementPlanVerifyID, dto);

        return await GetByIDAsDtoAsync(dbContext, waterQualityManagementPlanVerifyID);
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyID)
    {
        var verify = await dbContext.WaterQualityManagementPlanVerifies
            .Include(x => x.WaterQualityManagementPlanVerifyTreatmentBMPs)
            .Include(x => x.WaterQualityManagementPlanVerifyQuickBMPs)
            .Include(x => x.WaterQualityManagementPlanVerifySourceControlBMPs)
            .Include(x => x.WaterQualityManagementPlanVerifyPhotos)
            .SingleAsync(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID);

        await verify.DeleteFull(dbContext);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SaveChildRecordsAsync(NeptuneDbContext dbContext, int verifyID, WaterQualityManagementPlanVerifyUpsertDto dto)
    {
        if (dto.TreatmentBMPs?.Any() == true)
        {
            dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs.AddRange(dto.TreatmentBMPs.Select(x => new WaterQualityManagementPlanVerifyTreatmentBMP
            {
                WaterQualityManagementPlanVerifyID = verifyID,
                TreatmentBMPID = x.TreatmentBMPID,
                IsAdequate = x.IsAdequate,
                WaterQualityManagementPlanVerifyTreatmentBMPNote = x.WaterQualityManagementPlanVerifyTreatmentBMPNote,
            }));
        }

        if (dto.QuickBMPs?.Any() == true)
        {
            dbContext.WaterQualityManagementPlanVerifyQuickBMPs.AddRange(dto.QuickBMPs.Select(x => new WaterQualityManagementPlanVerifyQuickBMP
            {
                WaterQualityManagementPlanVerifyID = verifyID,
                QuickBMPID = x.QuickBMPID,
                IsAdequate = x.IsAdequate,
                WaterQualityManagementPlanVerifyQuickBMPNote = x.WaterQualityManagementPlanVerifyQuickBMPNote,
            }));
        }

        if (dto.SourceControlBMPs?.Any() == true)
        {
            dbContext.WaterQualityManagementPlanVerifySourceControlBMPs.AddRange(dto.SourceControlBMPs.Select(x => new WaterQualityManagementPlanVerifySourceControlBMP
            {
                WaterQualityManagementPlanVerifyID = verifyID,
                SourceControlBMPID = x.SourceControlBMPID,
                WaterQualityManagementPlanSourceControlCondition = x.WaterQualityManagementPlanSourceControlCondition,
            }));
        }

        await dbContext.SaveChangesAsync();
    }
}