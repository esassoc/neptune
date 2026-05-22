using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-998 — covers WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX, which transforms the WQMP Excel
    /// template (Data Hub WQMPs tab) into WaterQualityManagementPlan entities. Tests verify header
    /// validation, lookup-name resolution for the 5 enum-style FKs (Land Use, Priority, Status,
    /// Development Type, Trash Capture Status), duplicate detection within an upload, the
    /// new-vs-existing WQMP path, and optional-field handling.
    ///
    /// Pattern mirrors TestTreatmentCSVParser.cs: connects to the local dev SQL Server. The parser
    /// returns entities without persisting (the controller saves), so tests are read-only.
    /// </summary>
    [TestClass]
    public class WQMPXSLXParserHelperTests
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

        private int GetAnyJurisdictionID() =>
            _dbContext.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID).First();

        private static DataTable BuildDataTable(IEnumerable<string> columns, params string?[][] rows)
        {
            var dt = new DataTable();
            foreach (var col in columns) dt.Columns.Add(col);
            foreach (var row in rows) dt.Rows.Add(row.Cast<object?>().ToArray());
            return dt;
        }

        private static readonly string[] RequiredCols =
        {
            "WQMP Name", "Land Use", "Priority", "Status", "Development Type", "Trash Capture Status",
        };

        private static readonly string[] AllCols =
        {
            "WQMP Name", "Land Use", "Priority", "Status", "Development Type", "Trash Capture Status",
            "Maintenance Contact Name", "Maintenance Contact Organization", "Maintenance Contact Phone",
            "Maintenance Contact Address 1", "Maintenance Contact Address 2", "Maintenance Contact City",
            "Maintenance Contact State", "Maintenance Contact Zip",
            "Permit Term", "Hydromodification Controls Apply", "Approval Date", "Date of Construction",
            "Hydrologic Subarea", "Record Number", "Recorded WQMP Area (Acres)",
            "Trash Capture Effectiveness", "Modeling Approach",
        };

        private static string ValidLandUse() => WaterQualityManagementPlanLandUse.All.First().WaterQualityManagementPlanLandUseDisplayName;
        private static string ValidPriority() => WaterQualityManagementPlanPriority.All.First().WaterQualityManagementPlanPriorityDisplayName;
        private static string ValidStatus() => WaterQualityManagementPlanStatus.All.First().WaterQualityManagementPlanStatusDisplayName;
        private static string ValidDevelopmentType() => WaterQualityManagementPlanDevelopmentType.All.First().WaterQualityManagementPlanDevelopmentTypeDisplayName;
        private static string ValidTrashCaptureStatus() => TrashCaptureStatusType.All.First().TrashCaptureStatusTypeDisplayName;

        // Build a row of length AllCols.Length so optional-column reads inside the parser don't
        // throw "column does not belong to table". The production XLSX template always includes
        // every column, so this mirrors real-world uploads.
        private static string[] ValidRequiredRow(string wqmpName)
        {
            var row = new string[AllCols.Length];
            for (var i = 0; i < row.Length; i++) row[i] = "";
            row[Array.IndexOf(AllCols, "WQMP Name")] = wqmpName;
            row[Array.IndexOf(AllCols, "Land Use")] = ValidLandUse();
            row[Array.IndexOf(AllCols, "Priority")] = ValidPriority();
            row[Array.IndexOf(AllCols, "Status")] = ValidStatus();
            row[Array.IndexOf(AllCols, "Development Type")] = ValidDevelopmentType();
            row[Array.IndexOf(AllCols, "Trash Capture Status")] = ValidTrashCaptureStatus();
            return row;
        }

        private static string[] RowWithOverride(string wqmpName, string columnName, string value)
        {
            var row = ValidRequiredRow(wqmpName);
            row[Array.IndexOf(AllCols, columnName)] = value;
            return row;
        }

        private static string[] BlankRow()
        {
            var row = new string[AllCols.Length];
            for (var i = 0; i < row.Length; i++) row[i] = "";
            return row;
        }

        [TestMethod]
        public void MissingRequiredColumn_ReturnsErrors()
        {
            // Drop "Trash Capture Status" from the headers.
            var cols = RequiredCols.Take(5);
            var dt = BuildDataTable(cols);
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsNull(result);
            Assert.IsTrue(errors.Any(x => x.Contains("Trash Capture Status")), "Should call out the missing required column.");
        }

        [TestMethod]
        public void EmptyDataTable_ReturnsEmptyListNoErrors()
        {
            var dt = BuildDataTable(AllCols);
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void EmptyRowIsSkipped()
        {
            var dt = BuildDataTable(AllCols, BlankRow());
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void MissingWQMPName_ReturnsRow2Error()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("__placeholder__", "WQMP Name", ""));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("WQMP Name") && x.Contains("row: 2")),
                "Expected required-field error on row 2 for WQMP Name");
        }

        [TestMethod]
        public void InvalidLandUse_Errors()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("___NPT_998_TEST_BAD_LANDUSE___", "Land Use", "Not A Real Land Use"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Land Use") && x.Contains("does not exist")));
        }

        [TestMethod]
        public void InvalidPriority_Errors()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("___NPT_998_TEST_BAD_PRIORITY___", "Priority", "Stratospheric"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Priority Stratospheric")));
        }

        [TestMethod]
        public void InvalidStatus_Errors()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("___NPT_998_TEST_BAD_STATUS___", "Status", "Astral"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Status Astral")));
        }

        [TestMethod]
        public void InvalidDevelopmentType_Errors()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("___NPT_998_TEST_BAD_DEVTYPE___", "Development Type", "Hypothetical"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Development Type Hypothetical")));
        }

        [TestMethod]
        public void InvalidTrashCaptureStatus_Errors()
        {
            var dt = BuildDataTable(AllCols, RowWithOverride("___NPT_998_TEST_BAD_TCS___", "Trash Capture Status", "Quantum"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Trash Capture Status Type Quantum")));
        }

        [TestMethod]
        public void DuplicateWQMPNameInUpload_Errors()
        {
            var dt = BuildDataTable(AllCols,
                ValidRequiredRow("___NPT_998_TEST_DUP___"),
                ValidRequiredRow("___NPT_998_TEST_DUP___"));
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("duplicate name") && x.Contains("___NPT_998_TEST_DUP___")));
        }

        [TestMethod]
        public void NewWqmp_DefaultsModelingApproachToSimplified()
        {
            var dt = BuildDataTable(AllCols, ValidRequiredRow("___NPT_998_TEST_NEW_DEFAULTS___"));
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(0, result[0].WaterQualityManagementPlanID, "Should be a new entity (not yet saved)");
            Assert.AreEqual((int)WaterQualityManagementPlanModelingApproachEnum.Simplified, result[0].WaterQualityManagementPlanModelingApproachID);
        }

        [TestMethod]
        public void ExistingWqmp_ReturnsTrackedEntityWithPositiveID()
        {
            var existing = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existing == null)
            {
                Assert.Inconclusive("No existing WQMPs in dev DB; cannot test existing-entity path.");
                return;
            }
            var dt = BuildDataTable(AllCols, ValidRequiredRow(existing.WaterQualityManagementPlanName));
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, existing.StormwaterJurisdictionID, out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(existing.WaterQualityManagementPlanID, result[0].WaterQualityManagementPlanID,
                "Should return the existing entity to be updated, not a new one.");
        }

        [TestMethod]
        public void OptionalContactFields_AppliedWhenPresent()
        {
            var row = new List<string?>(ValidRequiredRow("___NPT_998_TEST_CONTACT___"));
            // Pad to AllCols length, then set contact fields by name.
            while (row.Count < AllCols.Length) row.Add("");
            void SetCol(string name, string value) => row[Array.IndexOf(AllCols, name)] = value;
            SetCol("Maintenance Contact Name", "Jane Doe");
            SetCol("Maintenance Contact Organization", "Acme");
            SetCol("Maintenance Contact Phone", "555-0100");
            SetCol("Maintenance Contact Address 1", "123 Main St");
            SetCol("Maintenance Contact City", "Orange");
            SetCol("Maintenance Contact State", "CA");
            SetCol("Maintenance Contact Zip", "92867");

            var dt = BuildDataTable(AllCols, row.ToArray());
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            var wqmp = result.Single();
            Assert.AreEqual("Jane Doe", wqmp.MaintenanceContactName);
            Assert.AreEqual("Acme", wqmp.MaintenanceContactOrganization);
            Assert.AreEqual("555-0100", wqmp.MaintenanceContactPhone);
            Assert.AreEqual("123 Main St", wqmp.MaintenanceContactAddress1);
            Assert.AreEqual("Orange", wqmp.MaintenanceContactCity);
            Assert.AreEqual("CA", wqmp.MaintenanceContactState);
            Assert.AreEqual("92867", wqmp.MaintenanceContactZip);
        }

        [TestMethod]
        public void OptionalApprovalDate_ParsedAndConvertedToUTC()
        {
            var row = new List<string?>(ValidRequiredRow("___NPT_998_TEST_DATE___"));
            while (row.Count < AllCols.Length) row.Add("");
            row[Array.IndexOf(AllCols, "Approval Date")] = "06/15/2024";
            var dt = BuildDataTable(AllCols, row.ToArray());
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            var wqmp = result.Single();
            Assert.IsTrue(wqmp.ApprovalDate.HasValue);
            Assert.AreEqual(2024, wqmp.ApprovalDate.Value.Year);
            Assert.AreEqual(DateTimeKind.Utc, wqmp.ApprovalDate.Value.Kind);
        }

        [TestMethod]
        public void OptionalApprovalDate_InvalidString_NoEntityProduced()
        {
            // Behavior of the helper: an invalid Approval Date pushes an error AND still returns
            // the parsed entity in-memory; the top-level method returns [] when any errors exist.
            var row = new List<string?>(ValidRequiredRow("___NPT_998_TEST_BADDATE___"));
            while (row.Count < AllCols.Length) row.Add("");
            row[Array.IndexOf(AllCols, "Approval Date")] = "not a date";
            var dt = BuildDataTable(AllCols, row.ToArray());
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("can not be converted to Date Time format")));
            Assert.AreEqual(0, result.Count, "Top-level returns empty list when any row had errors.");
        }

        [TestMethod]
        public void InvalidHydrologicSubarea_Errors()
        {
            var row = new List<string?>(ValidRequiredRow("___NPT_998_TEST_HSA___"));
            while (row.Count < AllCols.Length) row.Add("");
            row[Array.IndexOf(AllCols, "Hydrologic Subarea")] = "Nonexistent Subarea X";
            var dt = BuildDataTable(AllCols, row.ToArray());
            WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Hydrologic Subarea Nonexistent Subarea X")));
        }

        [TestMethod]
        public void MissingOptionalColumns_ParseSucceedsWithoutException()
        {
            // After the optional-column backfill fix: a hand-edited template that drops optional
            // columns should still parse cleanly — the missing columns are treated as blank.
            // Previously this threw ArgumentException on `row["Maintenance Contact Name"]`.
            var requiredOnly = new[] { "WQMP Name", "Land Use", "Priority", "Status", "Development Type", "Trash Capture Status" };
            var dt = BuildDataTable(requiredOnly,
                new[] { "___NPT_998_TEST_MIN_COLS___", ValidLandUse(), ValidPriority(), ValidStatus(), ValidDevelopmentType(), ValidTrashCaptureStatus() });
            var result = WQMPXSLXParserHelper.ParseWQMPRowsFromXLSX(_dbContext, dt, GetAnyJurisdictionID(), out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("___NPT_998_TEST_MIN_COLS___", result[0].WaterQualityManagementPlanName);
            Assert.IsNull(result[0].MaintenanceContactName);
            Assert.IsNull(result[0].ApprovalDate);
        }
    }
}
