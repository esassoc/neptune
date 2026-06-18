/*-----------------------------------------------------------------------
<copyright file="FileUtility.cs" company="Sitka Technology Group">
Copyright (c) Sitka Technology Group. All rights reserved.
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

namespace Neptune.Common
{
    public class FileUtility
    {

        public static void StringToFile(string stuffToWrite, FileInfo fileToWriteTo)
        {
            File.WriteAllText(fileToWriteTo.FullName, stuffToWrite);
        }

        /// <summary>
        /// Looks for a file in <paramref name="startingDir"/>and upwards until the <paramref name="fileFullNameRelativePath"/> is found
        /// </summary>
        /// <param name="startingDir">Directory to start search in</param>
        /// <param name="fileFullNameRelativePath">File name and (optionally) relative path to look for</param>
        /// <returns>Matching file or throws</returns>
        /// <exception cref="FileNotFoundException">Throws this if it can't find the file</exception>
        public static FileInfo FirstMatchingFileUpDirectoryTree(DirectoryInfo startingDir, string fileFullNameRelativePath)
        {
            var currentDirectory = startingDir;

            while (true)
            {
                var potentialFile = new FileInfo(Path.Combine(currentDirectory.FullName, fileFullNameRelativePath));
                if (potentialFile.Exists)
                {
                    return potentialFile;
                }
                currentDirectory = currentDirectory.Parent;
                if (currentDirectory == null)
                {
                    throw new FileNotFoundException(
                        $"Searched directory \"{startingDir.FullName}\" and upwards and could not find file \"{fileFullNameRelativePath}\".");
                }
            }
        }

        /// <summary>
        /// returns a human-readable representation of file-size
        /// </summary>
        /// <returns></returns>
        private static readonly string[] Orders = { "EB", "PB", "TB", "GB", "MB", "KB", "Bytes" };

        private static List<string> CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            var result = new List<string>();

            // Check if the target directory exists, if not, create it.
            if (Directory.Exists(target.FullName) == false)
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into its new directory.
            foreach (var fi in source.GetFiles())
            {
                result.Add(fi.FullName);
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each sub directory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                result.AddRange(CopyAll(diSourceSubDir, nextTargetSubDir));
            }

            return result;
        }
    }
}
