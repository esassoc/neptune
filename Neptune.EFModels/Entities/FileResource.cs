/*-----------------------------------------------------------------------
<copyright file="FileResource.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Neptune.EFModels.Entities
{
    public partial class FileResource
    {
        public string GetFileResourceUrl()
        {
            return $"/FileResource/DisplayResource/{GetFileResourceGUIDAsString()}";
        }

        public string FileResourceUrlScaledThumbnail(int maxHeight)
        {
            return $"/FileResource/GetFileResourceResized/{GetFileResourceGUIDAsString()}/{maxHeight}/{maxHeight}";
        }

        public string GetFileResourceDataLengthString()
        {
            return $"(~{(ContentLength / 1000):##,###} KB)";
        }

        public string GetOriginalCompleteFileName()
        {
            return $"{OriginalBaseFilename}.{OriginalFileExtension}";
        }

        /// <summary>
        /// Prepare the file bytes for going into blob storage
        /// </summary>
        /// <param name="formFile"></param>
        /// <returns></returns>
        public static byte[] ConvertHttpPostedFileToByteArray(IFormFile formFile)
        {
            using var binaryReader = new BinaryReader(formFile.OpenReadStream());
            var fileResourceData = binaryReader.ReadBytes((int) formFile.Length);
            binaryReader.Close();
            return fileResourceData;
        }

        public static readonly Regex FileResourceUrlRegEx =
            new Regex(@"FileResource\/DisplayResource\/(?<fileResourceGuidCapture>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Based on a string that has embedded file resource URLs in it, parse out the URLs and look up the corresponding FileResource stuff
        /// Made public for testing purposes.
        /// </summary>
        public static List<Guid> FindAllFileResourceGuidsFromStringContainingFileResourceUrls(string textWithReferences)
        {
            if (String.IsNullOrWhiteSpace(textWithReferences))
            {
                return new List<Guid>();
            }
            var guidCaptures = FileResourceUrlRegEx.Matches(textWithReferences).Cast<Match>().Select(x => x.Groups["fileResourceGuidCapture"].Value).ToList();
            var theseGuids = guidCaptures.Select(x => new Guid(x)).Distinct().ToList();
            return theseGuids;
        }

        public string GetFileResourceGUIDAsString()
        {
            return FileResourceGUID.ToString();
        }
    }
}
