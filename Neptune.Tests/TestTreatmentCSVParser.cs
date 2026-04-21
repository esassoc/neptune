using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;

namespace Neptune.Tests
{
    [TestClass]
    public class TreatmentBMPCsvParserHelperTest
    {
        private NeptuneDbContext _dbContext = GetDbContext();

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;", x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        private List<int> GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum treatmentBMPModelingTypeEnum)
        {
            return _dbContext.TreatmentBMPTypes.AsNoTracking().Where(x => x.TreatmentBMPModelingTypeID == (int)treatmentBMPModelingTypeEnum).Select(x => x.TreatmentBMPTypeID).ToList();
        }

        private int GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum treatmentBMPModelingTypeEnum)
        {
            return _dbContext.TreatmentBMPTypes.AsNoTracking().Single(x => x.TreatmentBMPModelingTypeID == (int)treatmentBMPModelingTypeEnum).TreatmentBMPTypeID;
        }


        [TestMethod]
        public void TestInvalidColumns()
        {
            const string csv = @"BMP Name,Jurisdiction,,Latitude,Longitude,BMP Type,Trash Capture Status,Sizing Basis,Yo";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("One or more required headers have not been provided. Required Fields are: ")), "Expected error about missing required fields in header");
            Assert.IsTrue(errorList.Any(x => x.Contains("did not match a property or custom attribute of the BMP type '")), "Expected error about misspelled headers");
        }

        [TestMethod]
        public void TestValidColumns()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(!errorList.Any(), "Should be a valid upload so no error messages expected");
        }

        [TestMethod]
        public void TestBMPNameNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("BMP Name is null, empty, or just whitespaces for row: ")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPNameExistsAndMatchingJurisdictionAndType()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Test Cook Park,30,10,City of San Juan Capistrano,City of San Juan Capistrano,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,Not Provided,";
            const int treatmentBMPTypeID = 14;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(!errorList.Any(), "Expected an error message");
            Assert.AreEqual(1, treatmentBMPs.Count, "Expected only one treatment BMP");
            Assert.IsTrue(treatmentBMPs[0].TreatmentBMPID > 0, "Treatment BMP ID should be > 0 since it is an existing one");
        }

        [TestMethod]
        public void TestBMPNameExistsAndNonMatchingType()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Test Cook Park,30,10,City of San Juan Capistrano,City of San Juan Capistrano,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,Not Provided,";
            const int treatmentBMPTypeID = 17; // this is the type that shouldn't match
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains(", which does not match the uploaded Type ")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLatitudeNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Latitude ")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLatitudeValueNotDecimalParseable()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,AB120,120,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Latitude can not be converted to Decimal format at row: ")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLatitudeValueNotAcceptableDecimalValue()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,95,120,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Latitude 95")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLongitudeNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Latitude")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLongitudeValueNotDecimalParseable()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,AB120,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Longitude can not be converted to Decimal format at row: ")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLongitudeValueNotAcceptableDecimalValue()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,85,181,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment BMP Longitude 181")), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPLocationGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Treatment BMP Latitude")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("Treatment BMP Latitude can not be converted to Decimal format at row: ")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("Treatment BMP Longitude can not be converted to Decimal format at row: ")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("Treatment BMP Longitude")), "Expected an error message");
            Assert.AreEqual($"SRID={Proj4NetHelper.WEB_MERCATOR};POINT (10 30)",
                $"SRID={treatmentBMPs[0].LocationPoint4326.SRID};{treatmentBMPs[0].LocationPoint4326}");
        }

        [TestMethod]
        public void TestJurisdictionNameNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Jurisdiction is null, empty, or just whitespaces for row: ")), "Expected error about blank Jurisdiction");
        }

        [TestMethod]
        public void TestJurisdictionNameExistsBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,Sitka Technology Grou,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("No Jurisdiction with the name '")), "Expected error about unknown Jurisdiction");
        }

        [TestMethod]
        public void TestJurisdictionNameExistsGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Dana Point,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Jurisdiction with the name '")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("Jurisdiction is null, empty, or just whitespaces for row: ")), "Expected an error message");
            Assert.AreEqual(2, treatmentBMPs[0].StormwaterJurisdictionID);
        }

        [TestMethod]
        public void TestOrganizationOwnerNameNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Owner is null, empty, or just whitespaces for row:")), "Expected an error message");
        }

        [TestMethod]
        public void TestOrganizationOwnerNameExistsBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Grou,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("No Owner with the name '")), "Expected an error message");
        }

        [TestMethod]
        public void TestOrganizationOwnerExistsGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,City of Orange,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Owner is null, empty, or just whitespaces for row:")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Owner with the name '")), "Expected an error message");
            Assert.AreEqual(25, treatmentBMPs[0].OwnerOrganizationID); // 25 is the City of Orange organizationID as of 2022-10-19
        }

        //Begin Optional Field Tests

        [TestMethod]
        public void TestYearBuiltIntParseBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,AB34,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Year Built or Installed can not be converted to Int at row")), "Expected an error message");
        }
        [TestMethod]
        public void TestYearBuiltIntParseGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Year Built or Installed can not be converted to Int at row")), "Expected an error message");
            Assert.AreEqual(2008, treatmentBMPs[0].YearBuilt);
        }

        [TestMethod]
        public void TestYearBuiltIntAcceptsNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Year Built or Installed can not be converted to Int at row")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].YearBuilt);
        }

        [TestMethod]
        public void TestAssetIdInSystemOfRecordsBad()
        {
            var sysID = "";
            for (var i = 0; i < TreatmentBMP.FieldLengths.SystemOfRecordID + 2; i++)
            {
                sysID += "a";
            }

            var csv = $@"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,{sysID},Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Asset ID in System of Record is too long at row")), "Expected an error message");
        }

        [TestMethod]
        public void TestAssetIdInSystemOfRecordsGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Asset ID in System of Record is too long at row")));
            Assert.AreEqual("ABCD", treatmentBMPs[0].SystemOfRecordID);
        }

        [TestMethod]
        public void TestAssetIdInSystemOfRecordsNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Asset ID in System of Record is too long at row")), "Expected an error message");
            Assert.AreEqual(null, treatmentBMPs[0].SystemOfRecordID);
        }

        [TestMethod]
        public void TestRequiredLifespanOfInstallationBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpet,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("No Required Lifespan of Installation with the name '")), "Expected an error message");
        }

        [TestMethod]
        public void TestRequiredLifespanOfInstallationGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Required Lifespan of Installation with the name '")), "Expected an error message");
            Assert.AreEqual(2, treatmentBMPs[0].TreatmentBMPLifespanTypeID);
        }

        [TestMethod]
        public void TestRequiredLifespanOfInstallationNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Required Lifespan Of Installation with the name '")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].TreatmentBMPLifespanTypeID);
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Allowable End Date of Installation (if applicable) can not be converted to Date Time format at row")), "Expected an error message");
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsFixedEndDateButDateNullBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Fixed End Date,,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("An end date must be provided if the 'Required Lifespan of Installation' field is set to fixed end date")), "Expected an error message");
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsFixedEndDateButDateNotNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Fixed End Date,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("An end date must be provided if the 'Required Lifespan of Installation' field is set to fixed end date")), "Expected an error message");
            Assert.AreEqual(DateTime.Parse("11/12/2022"), treatmentBMPs[0].TreatmentBMPLifespanEndDate);
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsNotFixedEndDateButDateNotNullBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("An end date was provided when 'Required Lifespan of Installation' field was set to Perpetuity/Life of Project")), "Expected an error message");
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsNotFixedEndDateButDateNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("An end date was provided when 'Required Lifespan of Installation' field was set to Perpetuity/Life of Project")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].TreatmentBMPLifespanEndDate);
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsNullButDateNotNullBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("An end date was provided when 'Required Lifespan of Installation' field was set to null")), "Expected an error message");
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationTypeIsNullButDateNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,,,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("An end date was provided when 'Required Lifespan of Installation' field was set to null")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].TreatmentBMPLifespanEndDate);
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Fixed End Date,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Allowable End Date of Installation can not be converted to Date Time format at row")), "Expected an error message");
            Assert.AreEqual(DateTime.Parse("11/12/2022"), treatmentBMPs[0].TreatmentBMPLifespanEndDate);
        }

        [TestMethod]
        public void TestAllowableEndDateOfInstallationNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Allowable End Date of Installation can not be converted to Date Time format at row")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].TreatmentBMPLifespanEndDate);
        }

        [TestMethod]
        public void TestRequiredFieldVisitsPerYearBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,Ab5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Required Field Visits Per Year can not be converted to Int at row")), "Expected an error message");
        }

        [TestMethod]
        public void TestRequiredFieldVisitsPerYearGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Required Field Visits per Year can not be converted to Int at row")), "Expected an error message");
            Assert.AreEqual(5, treatmentBMPs[0].RequiredFieldVisitsPerYear);
        }

        [TestMethod]
        public void TestRequiredFieldVisitsPerYearNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Required Field Visits per Year can not be converted to Int at row")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].RequiredFieldVisitsPerYear);
        }

        [TestMethod]
        public void TestRequiredPostStormFieldVisitsPerYearBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,AB6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Required Post-Storm Field Visits Per Year can not be converted to Int at row")), "Expected an error message");
        }

        [TestMethod]
        public void TestRequiredPostStormFieldVisitsPerYearGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Required Post-Storm Field Visits Per Year can not be converted to Int at row")), "Expected an error message");
            Assert.AreEqual(6, treatmentBMPs[0].RequiredPostStormFieldVisitsPerYear);
        }

        [TestMethod]
        public void TestRequiredPostStormFieldVisitsPerYearNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Required Post-Storm Field Visits Per Year can not be converted to Int at row")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].RequiredPostStormFieldVisitsPerYear);
        }

        [TestMethod]
        public void TestNotesBad()
        {
            var note = "";
            for (var i = 0; i < TreatmentBMP.FieldLengths.Notes + 2; i++)
            {
                note += "a";
            }

            var csv = $@"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,AB6,{note},Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Notes is too long at row")), "Expected an error message");
        }

        [TestMethod]
        public void TestNotesGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Notes is too long at row")), "Expected an error message");
            Assert.AreEqual("Happy", treatmentBMPs[0].Notes);
        }

        [TestMethod]
        public void TestNotesNullGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Notes length is too long at row")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].Notes);
        }

        //End Optional Field Tests
        [TestMethod]
        public void TestTrashCaptureStatusExistsNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,null,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Required Lifespan Of Installation by the name")), "Expected an error message");
            Assert.IsNull(treatmentBMPs[0].TreatmentBMPLifespanTypeID);
        }

        [TestMethod]
        public void TestTrashCaptureStatusExistsBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,4,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("No Trash Capture Status with the name '")), "Expected an error message");
        }

        [TestMethod]
        public void TestTrashCaptureStatusExistsGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Trash Capture Status is null, empty, or just whitespaces for row: ")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Trash Capture Status with the name '")), "Expected an error message");
            Assert.AreEqual(1, treatmentBMPs[0].TrashCaptureStatusTypeID);
        }


        [TestMethod]
        public void TestSizingBasisExistsNull()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Sizing Basis is null, empty, or just whitespaces for row: ")), "Expected an error message");
        }

        [TestMethod]
        public void TestSizingBasisExistsBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,4";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("No Sizing Basis with the name ")), "Expected an error message");
        }

        [TestMethod]
        public void TestSizingBasisExistsGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("No Sizing Basis with the name '")), "Expected an error message");
            Assert.IsTrue(errorList.Any(x => !x.Contains("Sizing Basis is null, empty, or just whitespaces for row: ")), "Expected an error message");
            Assert.AreEqual(4, treatmentBMPs[0].SizingBasisTypeID);
        }


        [TestMethod]
        public void UploadListPopulatesGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Brea,City of Brea,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,Not Provided
John,30,10,City of Brea,City of Brea,2008,ABCD,Perpetuity/Life of Project,,5,6,Happy,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(!errorList.Any(), "Expected no error messages");
        }

        [TestMethod]
        public void UploadListPopulatesBad()
        {

            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,,Full,Not Provided";
            const int treatmentBMPTypeID = 17;
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(), "Expected an error message");
        }

        [TestMethod]
        public void TestBMPModelingAttributeAverageDivertedFlowRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Average Diverted Flowrate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.LowFlowDiversions);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Average Diverted Flowrate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeAverageDivertedFlowRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Average Diverted Flowrate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.LowFlowDiversions);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Average Diverted Flowrate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeAverageTreatmentFlowRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Average Treatment Flowrate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Average Treatment Flowrate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeAverageTreatmentFlowRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Average Treatment Flowrate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Average Treatment Flowrate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignDryWeatherTreatmentCapacityGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Dry Weather Treatment Capacity,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Design Dry Weather Treatment Capacity")));
            var customAttributeTypeID = _dbContext.CustomAttributeTypes
                .Where(x => x.CustomAttributeTypeName == "Design Dry Weather Treatment Capacity").Select(x => x.CustomAttributeTypeID).First();
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
            Assert.AreEqual(customAttributeTypeID, customAttributes[0].CustomAttribute.CustomAttributeTypeID);
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignDryWeatherTreatmentCapacityBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Dry Weather Treatment Capacity,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Design Dry Weather Treatment Capacity")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignLowFlowDiversionCapacityGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Low Flow Diversion Capacity,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.LowFlowDiversions);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Design Low Flow Diversion Capacity")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignLowFlowDiversionCapacityBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Low Flow Diversion Capacity,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.LowFlowDiversions);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Design Low Flow Diversion Capacity")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignMediaFiltrationRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Media Filtration Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioinfiltrationBioretentionWithRaisedUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Design Media Filtration Rate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeDesignMediaFiltrationRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Design Media Filtration Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioinfiltrationBioretentionWithRaisedUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Design Media Filtration Rate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeDrawdownTimeforWQDetentionVolumeGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Drawdown Time For WQ Detention Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryExtendedDetentionBasin);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
                Assert.IsTrue(errorList.Any(x => !x.Contains("Drawdown Time For WQ Detention Volume")));
                Assert.AreEqual("1", customAttributes[0].AttributeValue);
            }
        }

        [TestMethod]
        public void TestBMPModelingAttributeDrawdownTimeforWQDetentionVolumeBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Drawdown Time For WQ Detention Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";

            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryExtendedDetentionBasin);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
                Assert.IsTrue(errorList.Any(x => x.Contains("Drawdown Time For WQ Detention Volume")));
            }
        }

        [TestMethod]
        public void TestBMPModelingAttributeEffectiveFootprintGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Effective Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryExtendedDetentionBasin);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Effective Footprint")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeEffectiveFootprintBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Effective Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryExtendedDetentionBasin);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Effective Footprint")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeEffectiveRetentionDepthGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Effective Retention Depth,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Effective Retention Depth")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeEffectiveRetentionDepthBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Effective Retention Depth,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Effective Retention Depth")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeInfiltrationDischargeRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Infiltration Discharge Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.Drywell);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Infiltration Discharge Rate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeInfiltrationDischargeRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Infiltration Discharge Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.Drywell);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Infiltration Discharge Rate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeInfiltrationSurfaceAreaGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Infiltration Surface Area,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioretentionWithNoUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Infiltration Surface Area")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeInfiltrationSurfaceAreaBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Infiltration Surface Area,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioretentionWithNoUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Infiltration Surface Area")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeMediaBedFootprintGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Media Bed Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioinfiltrationBioretentionWithRaisedUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Media Bed Footprint")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeMediaBedFootprintBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Media Bed Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.BioinfiltrationBioretentionWithRaisedUnderdrain);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Media Bed Footprint")));
        }

        [TestMethod]
        public void TestBMPModelingAttributePermanentPoolOrWetlandVolumeGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Permanent Pool Or Wetland Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";

            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.ConstructedWetland);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
                Assert.IsTrue(errorList.Any(x => !x.Contains("Permanent Pool Or Wetland Volume")));
                Assert.AreEqual("1", customAttributes[0].AttributeValue);
            }
        }

        [TestMethod]
        public void TestBMPModelingAttributePermanentPoolOrWetlandVolumeBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Permanent Pool Or Wetland Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";

            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.ConstructedWetland);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
                Assert.IsTrue(errorList.Any(x => x.Contains("Permanent Pool Or Wetland Volume")));
            }
        }

        [TestMethod]
        public void TestBMPcustomAttributestorageVolumeBelowLowestOutletElevationGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Storage Volume Below Lowest Outlet Elevation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Storage Volume Below Lowest Outlet Elevation")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPcustomAttributestorageVolumeBelowLowestOutletElevationBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Storage Volume Below Lowest Outlet Elevation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Storage Volume Below Lowest Outlet Elevation")));
        }

        [TestMethod]
        public void TestBMPcustomAttributesummerHarvestedWaterDemandGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Summer Harvested Water Demand,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";

            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.CisternsForHarvestAndUse);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
                Assert.IsTrue(errorList.Any(x => !x.Contains("Summer Harvested Water Demand")));
                Assert.AreEqual("1", customAttributes[0].AttributeValue);
            }
        }

        [TestMethod]
        public void TestBMPcustomAttributesummerHarvestedWaterDemandBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Summer Harvested Water Demand,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";

            var treatmentBMPTypeIDs = GetTreatmentBMPTypesByModelingTypeEnum(TreatmentBMPModelingTypeEnum.ConstructedWetland);
            foreach (var treatmentBMPTypeID in treatmentBMPTypeIDs)
            {
                TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
                Assert.IsTrue(errorList.Any(x => x.Contains("Summer Harvested Water Demand")));
            }
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID5Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,5";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("5", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID10Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,10";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("10", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID15Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,15";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("15", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID20Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,20";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("20", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID30Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,30";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("30", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID45Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,45";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("45", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationID60Good()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,60";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Time Of Concentration")));
            Assert.AreEqual("60", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTimeOfConcentrationIDBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Time Of Concentration,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,25";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Time Of Concentration")));
        }

        /*[TestMethod]
        public void TestBMPModelingAttributeDrawdownTimeForDetentionVolumeGood()
        {
AssertCustom.IgnoreOnBuildServer();
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Drawdown Time For Detention Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var bmpType = dbContext.TreatmentBMPTypes.Single(x => x.TreatmentBMPModelingTypeID == TreatmentBMPModelingType.DryWeatherTreatmentSystems.TreatmentBMPModelingTypeID).TreatmentBMPTypeID;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _,  out var customAttributes, out _);
            Assert.That(errorList.Any(x => !x.Contains("Drawdown Time For Detention Volume")), Is.True);
            Assert.That(customAttributes[0].DrawdownTimeForDetentionVolume, Is.EqualTo(1.0));
        }

        [TestMethod]
        public void TestBMPModelingAttributeDrawdownTimeForDetentionVolumeBad()
        {
AssertCustom.IgnoreOnBuildServer();
            const string csv =
                @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Drawdown Time For Detention Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var bmpType = dbContext.TreatmentBMPTypes.Single(x => x.TreatmentBMPModelingTypeID == TreatmentBMPModelingType.DryWeatherTreatmentSystems.TreatmentBMPModelingTypeID).TreatmentBMPTypeID;
            var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _, out _);
            Assert.That(errorList.Any(x => x.Contains("Drawdown Time For Detention Volume")), Is.True);
        }*/

        [TestMethod]
        public void TestBMPModelingAttributeTotalEffectiveBMPVolumeGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Total Effective BMP Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Total Effective BMP Volume")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTotalEffectiveBMPVolumeBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Total Effective BMP Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Total Effective BMP Volume")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeTotalEffectiveDrywellBMPVolumeGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Total Effective Drywell BMP Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.Drywell);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Total Effective Drywell BMP Volume")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTotalEffectiveDrywellBMPVolumeBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Total Effective Drywell BMP Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.Drywell);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Total Effective Drywell BMP Volume")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeTreatmentRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Treatment Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.HydrodynamicSeparator);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Treatment Rate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeTreatmentRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Treatment Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.HydrodynamicSeparator);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Treatment Rate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDAGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,A";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Hydrologic Soil Group")));
            Assert.AreEqual("A", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDBGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,B";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Hydrologic Soil Group")));
            Assert.AreEqual("B", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDCGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,C";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Hydrologic Soil Group")));
            Assert.AreEqual("C", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDDGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,D";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Hydrologic Soil Group")));
            Assert.AreEqual("D", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDLinerGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,Liner";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Hydrologic Soil Group")));
            Assert.AreEqual("Liner", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingHydrologicSoilGroupIDBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Hydrologic Soil Group,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.FlowDurationControlTank);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Underlying Hydrologic Soil Group")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingInfiltrationRateGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Infiltration Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.UndergroundInfiltration);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Underlying Infiltration Rate")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeUnderlyingInfiltrationRateBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Underlying Infiltration Rate,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.UndergroundInfiltration);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Underlying Infiltration Rate")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeExtendedDetentionSurchargeVolumeGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Extended Detention Surcharge Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.WetDetentionBasin);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Extended Detention Surcharge Volume")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeExtendedDetentionSurchargeVolumeBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Extended Detention Surcharge Volume,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.WetDetentionBasin);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Extended Detention Surcharge Volume")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeWettedFootprintGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Wetted Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Wetted Footprint")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeWettedFootprintBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Wetted Footprint,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.VegetatedSwale);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Wetted Footprint")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeWinterHarvestedWaterDemandGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Winter Harvested Water Demand,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,1";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.CisternsForHarvestAndUse);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out var customAttributes);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Winter Harvested Water Demand")));
            Assert.AreEqual("1", customAttributes[0].AttributeValue);
        }

        [TestMethod]
        public void TestBMPModelingAttributeWinterHarvestedWaterDemandBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Winter Harvested Water Demand,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.CisternsForHarvestAndUse);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Winter Harvested Water Demand")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeMonthsOfOperationWinterGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Months of Operation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,Winter";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Months of Operation")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeMonthsOfOperationSummerGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Months of Operation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,Summer";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Months of Operation")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeMonthsOfOperationBothGood()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Months of Operation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,Both";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => !x.Contains("Months of Operation")));
        }

        [TestMethod]
        public void TestBMPModelingAttributeMonthsOfOperationBad()
        {
            const string csv = @"BMP Name,Latitude,Longitude,Jurisdiction, Owner,Year Built or Installed,Asset ID in System of Record, Required Lifespan of Installation,Allowable End Date of Installation (if applicable), Required Field Visits Per Year, Required Post-Storm Field Visits Per Year,Notes,Trash Capture Status,Sizing Basis,Months of Operation,  
Frank,30,10,City of Orange,Sitka Technology Group,2008,ABCD,Perpetuity/Life of Project,11/12/2022,5,6,Happy,Full,Not Provided,blah";
            var treatmentBMPTypeID = GetTreatmentBMPTypeIDByModelingTypeEnum(TreatmentBMPModelingTypeEnum.DryWeatherTreatmentSystems);
            TreatmentBMPCsvParserHelper.CSVUpload(_dbContext, csv, treatmentBMPTypeID, out var errorList, out _, out _);
            Assert.IsTrue(errorList.Any(x => x.Contains("Months of Operation")));
        }
    }
}
