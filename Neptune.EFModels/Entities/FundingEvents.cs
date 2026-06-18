using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class FundingEvents
{
    private static IQueryable<FundingEvent> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.FundingEvents
                .Include(x => x.FundingEventFundingSources)
                .ThenInclude(x => x.FundingSource)
                .ThenInclude(x => x.Organization)
                .ThenInclude(x => x.OrganizationType)
            ;
    }

    public static async Task<List<FundingEventDto>> ListByTreatmentBMPIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var entities = await dbContext.FundingEvents
            .Include(x => x.FundingEventFundingSources)
            .ThenInclude(x => x.FundingSource)
            .ThenInclude(x => x.Organization)
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .ToListAsync();
        return entities.Select(x => x.AsDto()).OrderBy(x => x.DisplayName).ToList();
    }

    public static FundingEvent GetByIDWithChangeTracking(NeptuneDbContext dbContext, int fundingEventID)
    {
        var fundingEvent = GetImpl(dbContext)
            .SingleOrDefault(x => x.FundingEventID == fundingEventID);
        Check.RequireNotNull(fundingEvent, $"FundingEvent with ID {fundingEventID} not found!");
        return fundingEvent;
    }

    public static FundingEvent GetByID(NeptuneDbContext dbContext, int fundingEventID)
    {
        var fundingEvent = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.FundingEventID == fundingEventID);
        Check.RequireNotNull(fundingEvent, $"FundingEvent with ID {fundingEventID} not found!");
        return fundingEvent;
    }

    public static async Task<FundingEventDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int fundingEventID)
    {
        var entity = await dbContext.FundingEvents
            .Include(x => x.FundingEventFundingSources)
            .ThenInclude(x => x.FundingSource)
            .ThenInclude(x => x.Organization)
            .FirstOrDefaultAsync(x => x.FundingEventID == fundingEventID);
        return entity?.AsDto();
    }

    public static async Task<FundingEventDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, FundingEventUpsertDto dto)
    {
        var entity = dto.AsEntity(treatmentBMPID);
        dbContext.FundingEvents.Add(entity);
        await dbContext.SaveChangesAsync();
        foreach (var fsDto in dto.FundingEventFundingSources)
        {
            var fsEntity = new FundingEventFundingSource
            {
                FundingEventID = entity.FundingEventID,
                FundingSourceID = fsDto.FundingSourceID,
                Amount = fsDto.Amount
            };
            dbContext.FundingEventFundingSources.Add(fsEntity);
        }
        await dbContext.SaveChangesAsync();
        // Return fully loaded DTO using helper
        return await GetByIDAsDtoAsync(dbContext, entity.FundingEventID);
    }

    public static async Task<FundingEventDto?> UpdateAsync(NeptuneDbContext dbContext, int fundingEventID,
        FundingEventUpsertDto dto)
    {
        var entity = await dbContext.FundingEvents
            .Include(x => x.FundingEventFundingSources)
            .FirstAsync(x => x.FundingEventID == fundingEventID);
        entity.UpdateFromUpsertDto(dto);
        var updatedSources = dto.FundingEventFundingSources
            .Select(fsDto => new FundingEventFundingSource
            {
                FundingEventID = entity.FundingEventID,
                FundingSourceID = fsDto.FundingSourceID,
                Amount = fsDto.Amount
            })
            .ToList();
        entity.FundingEventFundingSources.Merge(updatedSources, dbContext.FundingEventFundingSources,
            (a, b) => a.FundingSourceID == b.FundingSourceID && a.FundingEventID == b.FundingEventID,
            (existing, updated) =>
            {
                existing.Amount = updated.Amount;
                existing.FundingSourceID = updated.FundingSourceID;
            });
        await dbContext.SaveChangesAsync();
        // Return fully loaded DTO using helper
        return await GetByIDAsDtoAsync(dbContext, entity.FundingEventID);
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int fundingEventID)
    {
        // Remove FundingEventFundingSources first using ExecuteDeleteAsync
        await dbContext.FundingEventFundingSources
            .Where(x => x.FundingEventID == fundingEventID)
            .ExecuteDeleteAsync();
        // Remove FundingEvent
        var deletedCount = await dbContext.FundingEvents
            .Where(x => x.FundingEventID == fundingEventID)
            .ExecuteDeleteAsync();
        return deletedCount > 0;
    }

    /// <summary>
    /// Validates a FundingEventUpsertDto for duplicate FundingSourceID values.
    /// Returns an error message if duplicates are found, otherwise null.
    /// </summary>
    public static string? ValidateFundingEventUpsertDto(FundingEventUpsertDto dto)
    {
        if (dto.FundingEventFundingSources
            .GroupBy(x => x.FundingSourceID)
            .Any(g => g.Count() > 1))
        {
            return "Duplicate FundingSourceID values are not allowed in FundingEventFundingSources.";
        }
        return null;
    }
}