using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessmentPhotos
{
    public static async Task<List<TreatmentBMPAssessmentPhotoDto>> ListByTreatmentBMPAssessmentIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID)
    {
        return await dbContext.TreatmentBMPAssessmentPhotos
            .AsNoTracking()
            .Where(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID)
            .OrderBy(x => x.Caption)
            .Select(TreatmentBMPAssessmentPhotoProjections.AsDto)
            .ToListAsync();
    }

    public static async Task<TreatmentBMPAssessmentPhotoDto?> GetAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID, int treatmentBMPAssessmentPhotoID)
    {
        return await dbContext.TreatmentBMPAssessmentPhotos
            .AsNoTracking()
            .Where(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID && x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID)
            .Select(TreatmentBMPAssessmentPhotoProjections.AsDto)
            .SingleOrDefaultAsync();
    }

    public static async Task<TreatmentBMPAssessmentPhotoDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID, int fileResourceID, TreatmentBMPAssessmentPhotoCreateDto createDto)
    {
        var photo = new TreatmentBMPAssessmentPhoto
        {
            TreatmentBMPAssessmentID = treatmentBMPAssessmentID,
            FileResourceID = fileResourceID,
            Caption = createDto.Caption,
        };
        await dbContext.TreatmentBMPAssessmentPhotos.AddAsync(photo);
        await dbContext.SaveChangesAsync();

        return (await GetAsDtoAsync(dbContext, treatmentBMPAssessmentID, photo.TreatmentBMPAssessmentPhotoID))!;
    }

    public static async Task<TreatmentBMPAssessmentPhotoDto> UpdateCaptionAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID, int treatmentBMPAssessmentPhotoID, TreatmentBMPAssessmentPhotoUpdateDto updateDto)
    {
        var photo = await dbContext.TreatmentBMPAssessmentPhotos
            .SingleAsync(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID && x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID);
        photo.Caption = updateDto.Caption;
        await dbContext.SaveChangesAsync();

        return (await GetAsDtoAsync(dbContext, treatmentBMPAssessmentID, treatmentBMPAssessmentPhotoID))!;
    }

    public static async Task<int> DeleteAsync(NeptuneDbContext dbContext, int treatmentBMPAssessmentID, int treatmentBMPAssessmentPhotoID)
    {
        var photo = await dbContext.TreatmentBMPAssessmentPhotos
            .AsNoTracking()
            .SingleAsync(x => x.TreatmentBMPAssessmentID == treatmentBMPAssessmentID && x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID);

        await dbContext.TreatmentBMPAssessmentPhotos
            .Where(x => x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID)
            .ExecuteDeleteAsync();

        await dbContext.FileResources
            .Where(x => x.FileResourceID == photo.FileResourceID)
            .ExecuteDeleteAsync();

        return photo.FileResourceID;
    }

    public static TreatmentBMPAssessmentPhoto GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPAssessmentPhotoID)
    {
        var treatmentBMPAssessmentPhoto = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID);
        Check.RequireNotNull(treatmentBMPAssessmentPhoto, $"TreatmentBMPAssessmentPhoto with ID {treatmentBMPAssessmentPhotoID} not found!");
        return treatmentBMPAssessmentPhoto;
    }

    public static TreatmentBMPAssessmentPhoto GetByID(NeptuneDbContext dbContext, int treatmentBMPAssessmentPhotoID)
    {
        var treatmentBMPAssessmentPhoto = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPAssessmentPhotoID == treatmentBMPAssessmentPhotoID);
        Check.RequireNotNull(treatmentBMPAssessmentPhoto, $"TreatmentBMPAssessmentPhoto with ID {treatmentBMPAssessmentPhotoID} not found!");
        return treatmentBMPAssessmentPhoto;
    }


    private static IQueryable<TreatmentBMPAssessmentPhoto> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPAssessmentPhotos
            .Include(x => x.FileResource);
    }
}