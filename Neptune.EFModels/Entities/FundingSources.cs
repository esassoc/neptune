using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class FundingSources
{

    public static async Task<List<FundingSourceDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await dbContext.FundingSources
            .Include(x => x.Organization)
            .ToListAsync();
        return entities.Select(x => x.AsDto()).OrderBy(x => x.FundingSourceName).ToList();
    }

    public static async Task<FundingSourceDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int fundingSourceID)
    {
        var entity = await dbContext.FundingSources
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.FundingSourceID == fundingSourceID);
        return entity?.AsDto();
    }

    /// <summary>
    /// NPT-999: focused query for the SPA FundingSource detail page's Treatment BMP Funding
    /// panel. Mirrors the legacy MVC FundingSource/Detail view's table — joins
    /// FundingEventFundingSource → FundingEvent → TreatmentBMP and sums Amount per BMP so
    /// multiple events against the same BMP collapse into one row.
    /// </summary>
    public static async Task<List<FundingSourceTreatmentBMPFundingDto>> ListTreatmentBMPFundingByIDAsync(NeptuneDbContext dbContext, int fundingSourceID)
    {
        return await dbContext.FundingEventFundingSources.AsNoTracking()
            .Where(fefs => fefs.FundingSourceID == fundingSourceID)
            .GroupBy(fefs => new
            {
                fefs.FundingEvent.TreatmentBMPID,
                fefs.FundingEvent.TreatmentBMP.TreatmentBMPName,
                fefs.FundingEvent.TreatmentBMP.InventoryIsVerified,
            })
            .Select(g => new FundingSourceTreatmentBMPFundingDto
            {
                TreatmentBMPID = g.Key.TreatmentBMPID,
                TreatmentBMPName = g.Key.TreatmentBMPName,
                InventoryIsVerified = g.Key.InventoryIsVerified,
                Amount = g.Sum(x => x.Amount ?? 0m),
            })
            .OrderBy(x => x.TreatmentBMPName)
            .ToListAsync();
    }

    public static async Task<FundingSourceDto> CreateAsync(NeptuneDbContext dbContext, FundingSourceUpsertDto dto)
    {
        var entity = dto.AsEntity();
        dbContext.FundingSources.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.FundingSourceID);
    }

    public static async Task<FundingSourceDto?> UpdateAsync(NeptuneDbContext dbContext, int fundingSourceID, FundingSourceUpsertDto dto)
    {
        var entity = await dbContext.FundingSources
            .Include(x => x.Organization)
            .FirstAsync(x => x.FundingSourceID == fundingSourceID);
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.FundingSourceID);
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int fundingSourceID)
    {
        var deletedCount = await dbContext.FundingSources
            .Where(x => x.FundingSourceID == fundingSourceID)
            .ExecuteDeleteAsync();
        return deletedCount > 0;
    }
}