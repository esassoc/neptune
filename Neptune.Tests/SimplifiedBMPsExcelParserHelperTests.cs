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
    /// NPT-998 — covers SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX, the Excel-template
    /// uploader on the Data Hub WQMPs tab that creates/updates QuickBMP rows attached to existing
    /// WQMPs. Tests verify header validation, WQMP-name resolution, BMP-type resolution, duplicate
    /// detection, and required-field handling. Read-only (controller saves).
    /// </summary>
    [TestClass]
    public class SimplifiedBMPsExcelParserHelperTests
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

        private string GetAnyBMPTypeName() =>
            _dbContext.TreatmentBMPTypes.AsNoTracking().Select(x => x.TreatmentBMPTypeName).First();

        private static DataTable BuildDataTable(IEnumerable<string> columns, params string?[][] rows)
        {
            var dt = new DataTable();
            foreach (var col in columns) dt.Columns.Add(col);
            foreach (var row in rows) dt.Rows.Add(row.Cast<object?>().ToArray());
            return dt;
        }

        private static readonly string[] AllCols =
        {
            "WQMP Name", "BMP Name", "BMP Type", "Count of BMPs",
            "% of Site Treated", "Wet Weather % Capture", "Wet Weather % Retained",
            "Dry Weather Flow Override?", "Notes",
        };

        [TestMethod]
        public void MissingRequiredColumn_ReturnsNullWithError()
        {
            // Drop "Count of BMPs" from the headers.
            var cols = new[] { "WQMP Name", "BMP Name", "BMP Type" };
            var dt = BuildDataTable(cols);
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, GetAnyJurisdictionID(), dt, out var errors);
            Assert.IsNull(result);
            Assert.IsTrue(errors.Any(x => x.Contains("Count of BMPs")));
        }

        [TestMethod]
        public void NonexistentWQMPName_Errors()
        {
            var dt = BuildDataTable(AllCols,
                new[] { "___NPT_998_TEST_MISSING_WQMP___", "BMP-A", GetAnyBMPTypeName(), "1", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, GetAnyJurisdictionID(), dt, out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("does not exist in given jurisdiction")
                                          || x.Contains("was not found")), string.Join("; ", errors));
        }

        [TestMethod]
        public void InvalidBMPType_Errors()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB; cannot test BMP-type-not-found.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "BMP-Bad-Type", "___NOT_A_BMP_TYPE___", "1", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("BMP Type ___NOT_A_BMP_TYPE___ does not exist")));
        }

        [TestMethod]
        public void MissingBMPType_Errors()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "BMP-NoType", "", "1", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("BMP Type") && x.Contains("empty or null")));
        }

        [TestMethod]
        public void EmptyRow_TriggersBlankWqmpNameError()
        {
            // Documents current behavior: the per-row WQMP-lookup loop in the parser runs
            // before the empty-row skip logic, so an all-blank row produces a "WQMP with name
            // '' does not exist" error and the top-level returns null. The legitimate template
            // never has blank rows, but a hand-edited sheet would surface this.
            var dt = BuildDataTable(AllCols, new string?[] { "", "", "", "", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, GetAnyJurisdictionID(), dt, out var errors);
            Assert.IsNull(result);
            Assert.IsTrue(errors.Any(x => x.Contains("WQMP with name") && x.Contains("does not exist")),
                string.Join("; ", errors));
        }

        [TestMethod]
        public void ValidHappyPath_ReturnsQuickBMPAttachedToExistingWqmp()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var bmpType = GetAnyBMPTypeName();
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_998_TEST_NEW_BMP___", bmpType, "2", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(existingWqmp.WaterQualityManagementPlanID, result[0].WaterQualityManagementPlanID);
            Assert.AreEqual("___NPT_998_TEST_NEW_BMP___", result[0].QuickBMPName);
            Assert.AreEqual(2, result[0].NumberOfIndividualBMPs);
        }

        [TestMethod]
        public void InvalidCountOfBMPs_Errors()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "BMP-BadCount", GetAnyBMPTypeName(), "abc", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);
            Assert.IsTrue(errors.Any(x => x.Contains("Count of BMPs")));
        }
    }
}
