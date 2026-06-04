using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services
{
    public static class HttpUtilities
    {
        public static async Task<FileResource> MakeFileResourceFromFormFileAsync(NeptuneDbContext dbContext, HttpContext httpContext, AzureBlobStorageService blobStorageService, IFormFile inputFile)
        {
            byte[] bytes;
            await using(var ms = new MemoryStream())
            {
                await inputFile.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var person = UserContext.GetUserAsDtoFromHttpContext(dbContext, httpContext);

            var fileResourceMimeType = FileResourceMimeType.GetFileResourceMimeTypeByContentTypeName(dbContext,
                inputFile.ContentType);

            var fileResource = new FileResource()
            {
                FileResourceMimeTypeID = fileResourceMimeType.FileResourceMimeTypeID,
                OriginalBaseFilename = Path.GetFileNameWithoutExtension(inputFile.FileName),
                OriginalFileExtension = Path.GetExtension(inputFile.FileName).TrimStart('.'),
                FileResourceGUID = Guid.NewGuid(),
                CreatePersonID = person.PersonID,
                CreateDate = DateTime.UtcNow,
                ContentLength = bytes.Length
            };
            await dbContext.FileResources.AddAsync(fileResource);

            if (await blobStorageService.UploadFileResource(fileResource.FileResourceGUID, fileResource.OriginalFileExtension, bytes))
            {
                await dbContext.SaveChangesAsync();
                await dbContext.Entry(fileResource).ReloadAsync();
                return fileResource;
            }

            throw new Exception($"New file resource was not uploaded to blob storage successfully.");
        }

        public static async Task<byte[]> GetData(this HttpRequest httpRequest)
        {
            byte[] bytes;
            using (var ms = new MemoryStream(2048))
            {
                await httpRequest.Body.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            return bytes;
        }
    }
    
}