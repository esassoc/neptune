using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPImages
{
    public static async Task<TreatmentBMPImageDto> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, int fileResourceID, TreatmentBMPImageCreateDto createDto, int callingPerson)
    {
        var treatmentBMPImage = new TreatmentBMPImage()
        {
            TreatmentBMPID = treatmentBMPID,
            FileResourceID = fileResourceID,
            Caption = createDto.Caption,
            UploadDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.TreatmentBMPImages.AddAsync(treatmentBMPImage);
        await dbContext.SaveChangesAsync();
        await dbContext.Entry(treatmentBMPImage).ReloadAsync();

        var treatmentBMPImageDto = treatmentBMPImage.AsDto();
        return treatmentBMPImageDto;
    }

    public static async Task<List<TreatmentBMPImageDto>> ListAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        // Caption-asc order matches the legacy MVC BMP detail carousel
        // (TreatmentBMPImages.ListByTreatmentBMPID, used by TreatmentBMPController.Detail).
        return await dbContext.TreatmentBMPImages.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .OrderBy(x => x.Caption)
            .Select(TreatmentBMPImageProjections.AsDto)
            .ToListAsync();
    }

    /// <summary>
    /// Carousel feed: inventory <see cref="TreatmentBMPImage"/> rows UNION assessment photos from
    /// the BMP's verified field visits. Used by the BMP detail page so a user can browse all of a
    /// BMP's photos in one place once the visit's been signed off, while the edit-images page
    /// (which has delete / caption-update affordances) keeps using <see cref="ListAsync"/> so it
    /// only shows rows backed by the TreatmentBMPImage table.
    /// </summary>
    public static async Task<List<TreatmentBMPImageDto>> ListForCarouselAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var inventoryDtos = await dbContext.TreatmentBMPImages.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(TreatmentBMPImageProjections.AsDto)
            .ToListAsync();

        var assessmentDtos = await dbContext.TreatmentBMPAssessmentPhotos.AsNoTracking()
            .Where(x => x.TreatmentBMPAssessment.TreatmentBMPID == treatmentBMPID
                        && x.TreatmentBMPAssessment.FieldVisit.IsFieldVisitVerified)
            .Select(TreatmentBMPImageProjections.AssessmentPhotoAsCarouselDto(treatmentBMPID))
            .ToListAsync();

        return inventoryDtos
            .Concat(assessmentDtos)
            .OrderBy(x => x.Caption)
            .ToList();
    }

    public static async Task<TreatmentBMPImageDto?> GetAsync(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPImageID)
    {
        return await dbContext.TreatmentBMPImages.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPImageID == treatmentBMPImageID)
            .Select(TreatmentBMPImageProjections.AsDto)
            .SingleOrDefaultAsync();
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateAsync(NeptuneDbContext dbContext, int treatmentBMPID, List<TreatmentBMPImageUpdateDto> updateDtos)
    {
        var errors = new List<ErrorMessage>();

        var treatmentBMPImageIDs = updateDtos.Select(x => x.TreatmentBMPImageID).ToList();
        var existingTreatmentBMPImages = await dbContext.TreatmentBMPImages.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID && treatmentBMPImageIDs.Contains(x.TreatmentBMPImageID))
            .Select(x => x.TreatmentBMPImageID)
            .ToListAsync();

        var missingIDs = treatmentBMPImageIDs.Except(existingTreatmentBMPImages).ToList();
        if (missingIDs.Any())
        {
            errors.Add(new ErrorMessage("TreatmentBMPImageID", $"TreatmentBMPImageIDs not found or not associated with TreatmentBMP {treatmentBMPID}: {string.Join(',', missingIDs)}"));
        }

        return errors;
    }

    public static async Task<List<TreatmentBMPImageDto>> UpdateAsync(NeptuneDbContext dbContext, int treatmentBMPID, List<TreatmentBMPImageUpdateDto> updateDtos)
    {
        var idsToUpdate = updateDtos.Select(x => x.TreatmentBMPImageID).ToList();

        var treatmentBMPImages = await dbContext.TreatmentBMPImages
            .Include(x => x.FileResource)
            .Where(x => x.TreatmentBMPID == treatmentBMPID && idsToUpdate.Contains(x.TreatmentBMPImageID))
            .ToListAsync();

        // Apply updates
        foreach (var dto in updateDtos)
        {
            var entity = treatmentBMPImages.Single(x => x.TreatmentBMPImageID == dto.TreatmentBMPImageID);
            entity.Caption = dto.Caption;
        }

        await dbContext.SaveChangesAsync();

        var treatmentBMPImageDtos = await ListAsync(dbContext, treatmentBMPID);
        return treatmentBMPImageDtos;
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPImageID)
    {
        var treatmentBMPImage = await dbContext.TreatmentBMPImages.AsNoTracking()
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPImageID == treatmentBMPImageID);

        await dbContext.TreatmentBMPImages
            .Where(x => x.TreatmentBMPID == treatmentBMPID && x.TreatmentBMPImageID == treatmentBMPImageID)
            .ExecuteDeleteAsync();

        await dbContext.FileResources
            .Where(x => x.FileResourceID == treatmentBMPImage.FileResourceID)
            .ExecuteDeleteAsync();
    }

    #region Used by WebMVC
    public static List<TreatmentBMPImage> ListByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.TreatmentBMPID == treatmentBMPID).OrderBy(ht => ht.Caption).ToList();
    }

    public static List<TreatmentBMPImage> ListByTreatmentBMPIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).Where(x => x.TreatmentBMPID == treatmentBMPID).OrderBy(ht => ht.Caption).ToList();
    }

    public static TreatmentBMPImage GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPImageID)
    {
        var treatmentBMPImage = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPImageID == treatmentBMPImageID);
        Check.RequireNotNull(treatmentBMPImage, $"TreatmentBMPImage with ID {treatmentBMPImageID} not found!");
        return treatmentBMPImage;
    }

    public static TreatmentBMPImage GetByIDWithChangeTracking(NeptuneDbContext dbContext, TreatmentBMPImagePrimaryKey treatmentBMPImagePrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, treatmentBMPImagePrimaryKey.PrimaryKeyValue);
    }

    public static TreatmentBMPImage GetByID(NeptuneDbContext dbContext, int treatmentBMPImageID)
    {
        var treatmentBMPImage = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPImageID == treatmentBMPImageID);
        Check.RequireNotNull(treatmentBMPImage, $"TreatmentBMPImage with ID {treatmentBMPImageID} not found!");
        return treatmentBMPImage;
    }

    public static TreatmentBMPImage GetByID(NeptuneDbContext dbContext, TreatmentBMPImagePrimaryKey treatmentBMPImagePrimaryKey)
    {
        return GetByID(dbContext, treatmentBMPImagePrimaryKey.PrimaryKeyValue);
    }


    private static IQueryable<TreatmentBMPImage> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPImages
            .Include(x => x.FileResource);
    }

    #endregion
}