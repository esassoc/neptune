using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public class FileResources
{
    public static List<ErrorMessage> ValidateFileUpload(IFormFile inputFile, bool imagesOnly = false)
    {
        var errors = new List<ErrorMessage>();
        var acceptedExtensions = new List<string> { ".pdf", ".png", ".jpg", ".jpeg", ".docx", ".doc", ".xlsx", ".txt", ".csv" };
        if (imagesOnly)
        {
            acceptedExtensions = [".png", ".jpg", ".jpeg"];
        }

        var extension = Path.GetExtension(inputFile.FileName);

        if (string.IsNullOrEmpty(extension) || !acceptedExtensions.Contains(extension.ToLower()))
        {
            errors.Add(new ErrorMessage
            {
                Type = "File Resource",
                Message = $"{extension[1..].ToUpper()} is not an accepted file extension: {string.Join(", ", acceptedExtensions)}."
            });
        }

        const double maxFileSize = 500d * 1024d * 1024d;
        if (inputFile.Length > maxFileSize)
        {
            errors.Add(new ErrorMessage
            {
                Type = "File Resource",
                Message = "File size cannot exceed 500MB."
            });
        }

        return errors;
    }

    public static FileResource Create(NeptuneDbContext dbContext, IFormFile file, string canonicalName, int createPersonID, DateTime createDate)
    {
        var clientFilename = file.FileName;
        var extension = clientFilename.Split('.').Last();
        var fileResourceGuid = Guid.NewGuid();

        var fileResource = new FileResource
        {
            FileResourceGUID = fileResourceGuid,
            OriginalBaseFilename = clientFilename,
            OriginalFileExtension = extension,
            CreateDate = createDate,
            CreatePersonID = createPersonID,
        };

        dbContext.FileResources.Add(fileResource);
        dbContext.SaveChanges();

        return fileResource;
    }

    public static FileResource? GetByID(NeptuneDbContext dbContext, int fileResourceID)
    {
        var fileResource = dbContext.FileResources
            .AsNoTracking()
            .SingleOrDefault(x => x.FileResourceID == fileResourceID);

        return fileResource;
    }

    public static FileResource? GetByGuidString(NeptuneDbContext dbContext, string fileResourceGuidAsString)
    {
        var isValidGuid = Guid.TryParse(fileResourceGuidAsString, out var fileResourceGuid);
        if (isValidGuid)
        {
            var fileResource = dbContext.FileResources.AsNoTracking()
                .SingleOrDefault(x => x.FileResourceGUID == fileResourceGuid);

            return fileResource;
        }

        return null;
    }

    public static async Task DeleteAsync(NeptuneDbContext dbContext, int fileResourceID)
    {
        await dbContext.FileResources.Where(x => x.FileResourceID == fileResourceID).ExecuteDeleteAsync();
    }
}
