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
        public void EmptyRow_IsSkippedNoError()
        {
            // After the empty-row skip fix in the WQMP-name pre-pass, trailing blank rows in
            // the spreadsheet should be silently ignored rather than producing a spurious
            // "WQMP with name '' does not exist" error.
            var dt = BuildDataTable(AllCols, new string?[] { "", "", "", "", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, GetAnyJurisdictionID(), dt, out var errors);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
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

        // ----- NPT-1073 -----

        [TestMethod]
        public void NonexistentWQMP_ProducesSingleErrorWithRowNumber()
        {
            // NPT-1073 Bug 1 regression guard: the redundant pre-pass loop was removed, so a row
            // referencing an unknown WQMP should produce exactly one error from the per-row parser
            // (with row number), not two (one from the pre-pass, one from the per-row).
            var dt = BuildDataTable(AllCols,
                new[] { "___NPT_1073_MISSING_WQMP___", "BMP-A", GetAnyBMPTypeName(), "1", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, GetAnyJurisdictionID(), dt, out var errors);

            var wqmpErrors = errors.Where(x => x.Contains("___NPT_1073_MISSING_WQMP___")).ToList();
            Assert.AreEqual(1, wqmpErrors.Count, $"Expected one WQMP-not-found error, got {wqmpErrors.Count}: {string.Join("; ", wqmpErrors)}");
            Assert.IsTrue(wqmpErrors[0].Contains("row 2"), $"Per-row error should include row number. Got: {wqmpErrors[0]}");
            Assert.IsFalse(errors.Any(x => x.Contains("does not exist in given jurisdiction")),
                "Pre-pass error wording should no longer appear.");
        }

        [TestMethod]
        public void DuplicateBMPName_WithinUpload_ProducesParserErrorWithRow()
        {
            // NPT-1073 Bug 3 regression guard: quickBMPNamesInCsv was being re-initialized inside the
            // per-row loop, so the in-CSV duplicate-name check never fired and rows slipped through
            // to SaveChanges as a UNIQUE constraint 500. With the list hoisted out, the parser now
            // catches duplicates and reports the row number.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var bmpType = GetAnyBMPTypeName();
            var bmpName = "___NPT_1073_DUPLICATE_NAME___";
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, bmpName, bmpType, "1", "", "", "", "", "" },
                new[] { existingWqmp.WaterQualityManagementPlanName, bmpName, bmpType, "2", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.IsTrue(errors.Any(x => x.Contains(bmpName) && x.Contains("duplicate name is found at row: 3")),
                $"Expected duplicate-name error naming row 3. Got: {string.Join("; ", errors)}");
            Assert.IsNull(result, "Parser must return null when any errors are present so SaveChanges is never reached.");
        }

        [TestMethod]
        public void InvalidDryWeatherFlowOverride_ErrorMentionsCorrectField()
        {
            // NPT-1073 Bug 4 regression guard: error was mis-labelled "BMP Type ..." due to copy-paste.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "BMP-BadDWF", GetAnyBMPTypeName(), "1", "", "", "", "___NOT_A_DWF_OVERRIDE___", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            var dwfError = errors.SingleOrDefault(x => x.Contains("___NOT_A_DWF_OVERRIDE___"));
            Assert.IsNotNull(dwfError, "Expected an error referencing the bad DWF value.");
            Assert.IsTrue(dwfError.Contains("Dry Weather Flow Override"), $"Error must name the correct field. Got: {dwfError}");
            Assert.IsFalse(dwfError.StartsWith("BMP Type"), $"Error must not mis-label the field as 'BMP Type'. Got: {dwfError}");
        }

        [TestMethod]
        public void CountOfBMPsZero_Rejected()
        {
            // NPT-1073 Bug 5 regression guard: 0 was silently accepted; now must be rejected with a
            // clear "at least 1" message. The entity's NumberOfIndividualBMPs must not be set on
            // the bad row.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_ZERO_COUNT___", GetAnyBMPTypeName(), "0", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.IsTrue(errors.Any(x => x.Contains("Count of BMPs") && x.Contains("at least 1") && x.Contains("row: 2")),
                $"Expected a clear 'at least 1' validation error for count = 0. Got: {string.Join("; ", errors)}");
        }

        [TestMethod]
        public void SameBMPNameAcrossDifferentWQMPs_NotFlaggedAsDuplicate()
        {
            // NPT-1073 follow-up (Copilot review on PR #539): QuickBMP uniqueness is scoped by
            // (WaterQualityManagementPlanID, QuickBMPName), so the same BMP Name under two
            // *different* WQMPs in one upload is legitimate. The duplicate tracker must be
            // per-WQMP, not global.
            var pair = _dbContext.WaterQualityManagementPlans
                .AsNoTracking()
                .GroupBy(x => x.StormwaterJurisdictionID)
                .Where(g => g.Count() >= 2)
                .Select(g => new { JurisdictionID = g.Key, Wqmps = g.Take(2).ToList() })
                .FirstOrDefault();
            if (pair == null)
            {
                Assert.Inconclusive("No jurisdiction has at least 2 WQMPs in dev DB; cross-WQMP same-name path cannot be exercised.");
                return;
            }
            var bmpType = GetAnyBMPTypeName();
            const string sharedBmpName = "___NPT_1073_CROSS_WQMP_NAME___";
            var dt = BuildDataTable(AllCols,
                new[] { pair.Wqmps[0].WaterQualityManagementPlanName, sharedBmpName, bmpType, "1", "", "", "", "", "" },
                new[] { pair.Wqmps[1].WaterQualityManagementPlanName, sharedBmpName, bmpType, "1", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, pair.JurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, $"Same BMP Name under different WQMPs is legitimate. Errors: {string.Join("; ", errors)}");
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void DuplicateBMPName_CaseInsensitive_WithinSameWQMP_ProducesError()
        {
            // SQL Server's default collation is case-insensitive, so two BMPs that differ only in
            // case under the same WQMP would still violate the UNIQUE constraint. The parser must
            // catch these the same way it catches exact-case duplicates.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var bmpType = GetAnyBMPTypeName();
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_CASE_TEST___", bmpType, "1", "", "", "", "", "" },
                new[] { existingWqmp.WaterQualityManagementPlanName, "___npt_1073_case_test___", bmpType, "2", "", "", "", "", "" });
            SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.IsTrue(errors.Any(x => x.Contains("duplicate name is found at row: 3")),
                $"Case-insensitive duplicate within a WQMP should be flagged. Got: {string.Join("; ", errors)}");
        }

        [TestMethod]
        public void CountOfBMPsOne_AcceptedBoundary()
        {
            // Inclusive lower-bound guard — 1 is valid.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_ONE_COUNT___", GetAnyBMPTypeName(), "1", "", "", "", "", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0].NumberOfIndividualBMPs);
        }

        // ----- NPT-1073 round 2 -----

        [TestMethod]
        public void DryWeatherFlowOverride_AcceptsYesShorthand()
        {
            // NPT-1073 round 2: KE asked us to accept the shorthand "Yes"/"No" in the DWF Override
            // column so uploaders don't have to type the full display name (e.g. "Yes - DWF
            // Effectively Eliminated"). The shorthand maps to DryWeatherFlowOverrideName.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_DWF_YES___", GetAnyBMPTypeName(), "1", "", "", "", "Yes", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideID, result[0].DryWeatherFlowOverrideID);
        }

        [TestMethod]
        public void DryWeatherFlowOverride_AcceptsNoShorthand()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_DWF_NO___", GetAnyBMPTypeName(), "1", "", "", "", "No", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DryWeatherFlowOverride.No.DryWeatherFlowOverrideID, result[0].DryWeatherFlowOverrideID);
        }

        [TestMethod]
        public void DryWeatherFlowOverride_AcceptsFullDisplayName()
        {
            // Backwards-compat guard: the original behaviour (full display name) still works.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_DWF_FULL___", GetAnyBMPTypeName(), "1", "", "", "", DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideDisplayName, "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideID, result[0].DryWeatherFlowOverrideID);
        }

        [TestMethod]
        public void DryWeatherFlowOverride_CaseInsensitive()
        {
            // Lowercase "yes" / mixed case should still resolve — Excel users paste from anywhere.
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var dt = BuildDataTable(AllCols,
                new[] { existingWqmp.WaterQualityManagementPlanName, "___NPT_1073_DWF_LOWER___", GetAnyBMPTypeName(), "1", "", "", "", "yes", "" });
            var result = SimplifiedBMPsExcelParserHelper.ParseWQMPRowsFromXLSX(_dbContext, existingWqmp.StormwaterJurisdictionID, dt, out var errors);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DryWeatherFlowOverride.Yes.DryWeatherFlowOverrideID, result[0].DryWeatherFlowOverrideID);
        }
    }
}
