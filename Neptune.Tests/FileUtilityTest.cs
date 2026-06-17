/*-----------------------------------------------------------------------
<copyright file="FileUtilityTest.cs" company="Sitka Technology Group">
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

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common;

namespace Neptune.Tests
{
    [TestClass]
    public class FileUtilityTest
    {
        private readonly string _testFileName = $"UnitTestFileName-{Guid.NewGuid()}.txt";
        private readonly string _tempDirFullName = Path.Combine(Path.GetTempPath(), $"TestTemp{Guid.NewGuid()}");
        private DirectoryInfo _tempDir;

        [TestInitialize]
        public void TheSetup()
        {
            _tempDir = Directory.CreateDirectory(_tempDirFullName);
        }

        [TestCleanup]
        public void TheTearDown()
        {
            _tempDir.Delete(true);
        }

        [TestMethod]
        public void GivenFileInCurrentDirectoryThenCanFind()
        {
            var fileToWriteTo = new FileInfo(Path.Combine(_tempDir.FullName, _testFileName));
            FileUtility.StringToFile("test", fileToWriteTo);

            var foundFile = FileUtility.FirstMatchingFileUpDirectoryTree(_tempDir, fileToWriteTo.Name);
            Assert.AreEqual(fileToWriteTo.FullName,foundFile.FullName, "Should have found file");
        }

        [TestMethod]
        public void GivenFileUpLevelsThenCanFind()
        {
            var fileToWriteTo = new FileInfo(Path.Combine(_tempDir.FullName, _testFileName));
            FileUtility.StringToFile("test", fileToWriteTo);

            var tempDirSub1 = _tempDir.CreateSubdirectory("Sub1");

            var foundFile = FileUtility.FirstMatchingFileUpDirectoryTree(tempDirSub1, fileToWriteTo.Name);
            Assert.AreEqual(fileToWriteTo.FullName, foundFile.FullName, "Should have found file");
        }

        [TestMethod]
        public void GivenFileUpAndDownLevelsThenCanFind()
        {
            var tempDirSub1 = _tempDir.CreateSubdirectory("Sub1");
            var fileToWriteTo = new FileInfo(Path.Combine(tempDirSub1.FullName, _testFileName));
            FileUtility.StringToFile("test", fileToWriteTo);

            var tempDirSub2 = tempDirSub1.CreateSubdirectory("Sub2");

            var foundFile = FileUtility.FirstMatchingFileUpDirectoryTree(tempDirSub2, Path.Combine(tempDirSub1.Name, fileToWriteTo.Name));
            Assert.AreEqual(fileToWriteTo.FullName, foundFile.FullName, "Should have found file");
        }

        [TestMethod]
        public void GivenFileNotThereThenThrowException()
        {
            var fileFullNameRelativePath = $"MyNonExistentTestFile{Guid.NewGuid()}.txt";
            var exception = Assert.Throws<FileNotFoundException>(() => FileUtility.FirstMatchingFileUpDirectoryTree(_tempDir, fileFullNameRelativePath), "Should throw if can't find file");
            Assert.IsTrue(exception.Message.Contains(_tempDir.FullName), "Exception should mention the directory");
            Assert.IsTrue(exception.Message.Contains(fileFullNameRelativePath), "Exception should mention the file name");
        }
    }
}
