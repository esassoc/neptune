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

    public static async Task<List<WaterQualityManagementPlanVerifyGridDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var rawData = await dbContext.WaterQualityManagementPlanVerifies
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(x => new
            {
                x.WaterQualityManagementPlanVerifyID,
                x.VerificationDate,
                x.LastEditedDate,
                LastEditedByPersonFullName = x.LastEditedByPerson.FirstName + " " + x.LastEditedByPerson.LastName,
                x.WaterQualityManagementPlanVerifyTypeID,
                x.WaterQualityManagementPlanVisitStatusID,
                x.WaterQualityManagementPlanVerifyStatusID,
                x.IsDraft,
            })
            .OrderByDescending(x => x.VerificationDate)
            .ToListAsync();

        return rawData.Select(x => new WaterQualityManagementPlanVerifyGridDto
        {
            WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
            VerificationDate = x.VerificationDate,
            LastEditedDate = x.LastEditedDate,
            LastEditedByPersonFullName = x.LastEditedByPersonFullName,
            WaterQualityManagementPlanVerifyTypeDisplayName = WaterQualityManagementPlanVerifyType.AllLookupDictionary.TryGetValue(x.WaterQualityManagementPlanVerifyTypeID, out var verifyType) ? verifyType.WaterQualityManagementPlanVerifyTypeDisplayName : null,
            WaterQualityManagementPlanVisitStatusDisplayName = WaterQualityManagementPlanVisitStatus.AllLookupDictionary.TryGetValue(x.WaterQualityManagementPlanVisitStatusID, out var visitStatus) ? visitStatus.WaterQualityManagementPlanVisitStatusDisplayName : null,
            WaterQualityManagementPlanVerifyStatusDisplayName = x.WaterQualityManagementPlanVerifyStatusID.HasValue && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(x.WaterQualityManagementPlanVerifyStatusID.Value, out var verifyStatus) ? verifyStatus.WaterQualityManagementPlanVerifyStatusDisplayName : null,
            IsDraft = x.IsDraft,
        }).ToList();
    }

    public static async Task<WaterQualityManagementPlanVerifyDetailDto> GetByIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyID)
    {
        var verify = await dbContext.WaterQualityManagementPlanVerifies
            .AsNoTracking()
            .Include(x => x.LastEditedByPerson)
            .Include(x => x.FileResource)
            .Include(x => x.WaterQualityManagementPlanVerifyTreatmentBMPs).ThenInclude(x => x.TreatmentBMP)
            .Include(x => x.WaterQualityManagementPlanVerifyQuickBMPs).ThenInclude(x => x.QuickBMP).ThenInclude(x => x.TreatmentBMPType)
            .Include(x => x.WaterQualityManagementPlanVerifySourceControlBMPs).ThenInclude(x => x.SourceControlBMP).ThenInclude(x => x.SourceControlBMPAttribute).ThenInclude(x => x.SourceControlBMPAttributeCategory)
            .SingleOrDefaultAsync(x => x.WaterQualityManagementPlanVerifyID == waterQualityManagementPlanVerifyID);

        if (verify == null) return null;

        return new WaterQualityManagementPlanVerifyDetailDto
        {
            WaterQualityManagementPlanVerifyID = verify.WaterQualityManagementPlanVerifyID,
            WaterQualityManagementPlanID = verify.WaterQualityManagementPlanID,
            WaterQualityManagementPlanVerifyTypeID = verify.WaterQualityManagementPlanVerifyTypeID,
            WaterQualityManagementPlanVerifyTypeDisplayName = WaterQualityManagementPlanVerifyType.AllLookupDictionary.TryGetValue(verify.WaterQualityManagementPlanVerifyTypeID, out var vt) ? vt.WaterQualityManagementPlanVerifyTypeDisplayName : null,
            WaterQualityManagementPlanVisitStatusID = verify.WaterQualityManagementPlanVisitStatusID,
            WaterQualityManagementPlanVisitStatusDisplayName = WaterQualityManagementPlanVisitStatus.AllLookupDictionary.TryGetValue(verify.WaterQualityManagementPlanVisitStatusID, out var vs) ? vs.WaterQualityManagementPlanVisitStatusDisplayName : null,
            WaterQualityManagementPlanVerifyStatusID = verify.WaterQualityManagementPlanVerifyStatusID,
            WaterQualityManagementPlanVerifyStatusDisplayName = verify.WaterQualityManagementPlanVerifyStatusID.HasValue && WaterQualityManagementPlanVerifyStatus.AllLookupDictionary.TryGetValue(verify.WaterQualityManagementPlanVerifyStatusID.Value, out var vrs) ? vrs.WaterQualityManagementPlanVerifyStatusDisplayName : null,
            VerificationDate = verify.VerificationDate,
            LastEditedDate = verify.LastEditedDate,
            LastEditedByPersonFullName = verify.LastEditedByPerson != null ? $"{verify.LastEditedByPerson.FirstName} {verify.LastEditedByPerson.LastName}" : null,
            SourceControlCondition = verify.SourceControlCondition,
            EnforcementOrFollowupActions = verify.EnforcementOrFollowupActions,
            IsDraft = verify.IsDraft,
            FileResourceGUID = verify.FileResource?.FileResourceGUID.ToString(),
            TreatmentBMPs = verify.WaterQualityManagementPlanVerifyTreatmentBMPs.Select(x => new WaterQualityManagementPlanVerifyTreatmentBMPSimpleDto
            {
                WaterQualityManagementPlanVerifyTreatmentBMPID = x.WaterQualityManagementPlanVerifyTreatmentBMPID,
                WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
                TreatmentBMPID = x.TreatmentBMPID,
                IsAdequate = x.IsAdequate,
                WaterQualityManagementPlanVerifyTreatmentBMPNote = x.WaterQualityManagementPlanVerifyTreatmentBMPNote,
                TreatmentBMPName = x.TreatmentBMP?.TreatmentBMPName,
                TreatmentBMPType = x.TreatmentBMP?.TreatmentBMPType?.TreatmentBMPTypeName,
            }).ToList(),
            QuickBMPs = verify.WaterQualityManagementPlanVerifyQuickBMPs.Select(x => new WaterQualityManagementPlanVerifyQuickBMPDto
            {
                WaterQualityManagementPlanVerifyQuickBMPID = x.WaterQualityManagementPlanVerifyQuickBMPID,
                WaterQualityManagementPlanVerifyID = x.WaterQualityManagementPlanVerifyID,
                QuickBMPID = x.QuickBMPID,
                IsAdequate = x.IsAdequate,
                WaterQualityManagementPlanVerifyQuickBMPNote = x.WaterQualityManagementPlanVerifyQuickBMPNote,
                QuickBMPName = x.QuickBMP?.QuickBMPName,
                TreatmentBMPType = x.QuickBMP?.TreatmentBMPType?.TreatmentBMPTypeName,
            }).ToList(),
            SourceControlBMPs = verify.WaterQualityManagementPlanVerifySourceControlBMPs.Select(x => new VerifySourceControlBMPDetailDto
            {
                WaterQualityManagementPlanVerifySourceControlBMPID = x.WaterQualityManagementPlanVerifySourceControlBMPID,
                SourceControlBMPID = x.SourceControlBMPID,
                SourceControlBMPAttributeName = x.SourceControlBMP?.SourceControlBMPAttribute?.SourceControlBMPAttributeName,
                SourceControlBMPAttributeCategoryName = x.SourceControlBMP?.SourceControlBMPAttribute?.SourceControlBMPAttributeCategory?.SourceControlBMPAttributeCategoryName,
                WaterQualityManagementPlanSourceControlCondition = x.WaterQualityManagementPlanSourceControlCondition,
            }).ToList(),
        };
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

        verify.DeleteFull(dbContext);
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