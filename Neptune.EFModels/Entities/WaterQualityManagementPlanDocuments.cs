using System;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanDocuments
{
    public static IQueryable<WaterQualityManagementPlanDocument> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanDocuments.Include(x => x.WaterQualityManagementPlan)
            .Include(x => x.FileResource).ThenInclude(x => x.CreatePerson).ThenInclude(x => x.Organization);
    }

    public static List<WaterQualityManagementPlanDocument> ListByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).OrderBy(ht => ht.DisplayName).ToList();
    }

    public static async Task<List<WaterQualityManagementPlanDocumentDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await GetImpl(dbContext).AsNoTracking().ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }

    public static async Task<List<WaterQualityManagementPlanDocumentDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var entities = await GetImpl(dbContext).AsNoTracking().Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).OrderBy(ht => ht.DisplayName).ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }

    public static WaterQualityManagementPlanDocument GetByIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID)
    {
        var waterQualityManagementPlanDocument = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        Check.RequireNotNull(waterQualityManagementPlanDocument, $"WaterQualityManagementPlanDocument with ID {waterQualityManagementPlanDocumentID} not found!");
        return waterQualityManagementPlanDocument;
    }

    public static WaterQualityManagementPlanDocument GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID)
    {
        var waterQualityManagementPlanDocument = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        Check.RequireNotNull(waterQualityManagementPlanDocument, $"WaterQualityManagementPlanDocument with ID {waterQualityManagementPlanDocumentID} not found!");
        return waterQualityManagementPlanDocument;
    }

    public static async Task<WaterQualityManagementPlanDocumentDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID)
    {
        var entity = await GetImpl(dbContext).AsNoTracking().FirstOrDefaultAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        return entity?.AsDto();
    }

    public static async Task<WaterQualityManagementPlanDocumentDto> CreateAsync(NeptuneDbContext dbContext, WaterQualityManagementPlanDocumentUpsertDto dto)
    {
        var entity = dto.AsEntity();
        dbContext.WaterQualityManagementPlanDocuments.Add(entity);
        await dbContext.SaveChangesAsync();
        return (await GetByIDAsDtoAsync(dbContext, entity.WaterQualityManagementPlanDocumentID))!;
    }

    public static async Task<WaterQualityManagementPlanDocumentDto?> UpdateAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID, WaterQualityManagementPlanDocumentUpsertDto dto)
    {
        var entity = await dbContext.WaterQualityManagementPlanDocuments.FirstAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.WaterQualityManagementPlanDocumentID);
    }

    public static async Task<WaterQualityManagementPlanDocument> CreateFromFileResourceAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID, int fileResourceID, string displayName, int documentTypeID, string? description = null)
    {
        var document = new WaterQualityManagementPlanDocument
        {
            WaterQualityManagementPlanID = waterQualityManagementPlanID,
            FileResourceID = fileResourceID,
            DisplayName = displayName,
            UploadDate = DateTime.UtcNow,
            WaterQualityManagementPlanDocumentTypeID = documentTypeID,
            Description = description,
        };
        dbContext.WaterQualityManagementPlanDocuments.Add(document);
        await dbContext.SaveChangesAsync();
        return document;
    }

    public static async Task<WaterQualityManagementPlanDocumentDto?> UpdateMetadataAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID, int? newFileResourceID,
        string displayName, int documentTypeID, string? description)
    {
        var entity = await dbContext.WaterQualityManagementPlanDocuments
            .FirstOrDefaultAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        if (entity == null) return null;

        entity.DisplayName = displayName;
        entity.WaterQualityManagementPlanDocumentTypeID = documentTypeID;
        entity.Description = description;
        if (newFileResourceID.HasValue)
        {
            entity.FileResourceID = newFileResourceID.Value;
            entity.UploadDate = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.WaterQualityManagementPlanDocumentID);
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanDocumentID)
    {
        var entity = await dbContext.WaterQualityManagementPlanDocuments.FirstOrDefaultAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID);
        if (entity == null) return false;
        await entity.DeleteFull(dbContext);
        return true;
    }
}