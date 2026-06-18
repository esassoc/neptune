using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;
using System.Linq;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPDocuments
{
    public static async Task<TreatmentBMPDocumentDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, int fileResourceID, TreatmentBMPDocumentCreateDto createDto, int callingPerson)
    {
        var treatmentBMPDocument = new TreatmentBMPDocument()
        {
            TreatmentBMPID = treatmentBMPID,
            FileResourceID = fileResourceID,
            DocumentDescription = createDto.Description,
            UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.TreatmentBMPDocuments.AddAsync(treatmentBMPDocument);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(treatmentBMPDocument).ReloadAsync();

        var treatmentBMPDocumentDto = treatmentBMPDocument.AsDto();
        return treatmentBMPDocumentDto;
    }

    public static async Task<List<TreatmentBMPDocumentDto>> ListAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMPDocuments = await dbContext.TreatmentBMPDocuments.AsNoTracking()
            .Include(x => x.FileResource)
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .ToListAsync();

        var treatmentBMPDocumentDtos = treatmentBMPDocuments.Select(x => x.AsDto()).ToList();
        return treatmentBMPDocumentDtos;
    }

    public static async Task<TreatmentBMPDocumentDto?> GetAsync(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPDocumentID)
    {
        var treatmentBMPDocument = await dbContext.TreatmentBMPDocuments.AsNoTracking()
            .Include(x => x.FileResource)
            .SingleOrDefaultAsync(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPDocumentID == treatmentBMPDocumentID);

        var treatmentBMPDocumentDto = treatmentBMPDocument?.AsDto();
        return treatmentBMPDocumentDto;
    }

    public static async Task<TreatmentBMPDocumentDto> UpdateAsync(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPDocumentID, TreatmentBMPDocumentUpdateDto updateDto)
    {
        var treatmentBMPDocument = await dbContext.TreatmentBMPDocuments
            .Include(x => x.FileResource)
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPDocumentID == treatmentBMPDocumentID);

        treatmentBMPDocument.DocumentDescription = updateDto.Description;

        await dbContext.SaveChangesAsync();

        var treatmentBMPDocumentDto = await GetAsync(dbContext, treatmentBMPID, treatmentBMPDocumentID);
        return treatmentBMPDocumentDto!;
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPDocumentID)
    {
        var treatmentBMPDocument = await dbContext.TreatmentBMPDocuments.AsNoTracking()
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPDocumentID == treatmentBMPDocumentID);

        await dbContext.TreatmentBMPDocuments
            .Where(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPDocumentID == treatmentBMPDocumentID)
            .ExecuteDeleteAsync();

        await dbContext.FileResources
            .Where(x => x.FileResourceID == treatmentBMPDocument.FileResourceID)
            .ExecuteDeleteAsync();
    }

    public static TreatmentBMPDocument GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPDocumentID)
    {
        var treatmentBMPDocument = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPDocumentID == treatmentBMPDocumentID);
        Check.RequireNotNull(treatmentBMPDocument, $"TreatmentBMPDocument with ID {treatmentBMPDocumentID} not found!");
        return treatmentBMPDocument;
    }

    public static TreatmentBMPDocument GetByID(NeptuneDbContext dbContext, int treatmentBMPDocumentID)
    {
        var treatmentBMPDocument = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPDocumentID == treatmentBMPDocumentID);
        Check.RequireNotNull(treatmentBMPDocument, $"TreatmentBMPDocument with ID {treatmentBMPDocumentID} not found!");
        return treatmentBMPDocument;
    }


    private static IQueryable<TreatmentBMPDocument> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPDocuments
            .Include(x => x.FileResource);
    }
}