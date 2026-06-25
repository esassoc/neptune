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

        public string GetOriginalCompleteFileName()
        {
            if (string.IsNullOrEmpty(OriginalFileExtension)) return OriginalBaseFilename ?? string.Empty;
            if (string.IsNullOrEmpty(OriginalBaseFilename)) return $".{OriginalFileExtension}";
            var extensionWithDot = $".{OriginalFileExtension}";
            if (OriginalBaseFilename.EndsWith(extensionWithDot, StringComparison.OrdinalIgnoreCase)) return OriginalBaseFilename;
            return $"{OriginalBaseFilename}{extensionWithDot}";
        }

        public static readonly Regex FileResourceUrlRegEx =
            new Regex(@"FileResource\/DisplayResource\/(?<fileResourceGuidCapture>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string GetFileResourceGUIDAsString()
        {
            return FileResourceGUID.ToString();
        }
    }
}
