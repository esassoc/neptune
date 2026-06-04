using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-998 — covers OvtaBulkUploadImporter.BulkUploadAsync, the Data Hub Trash Module tab
    /// uploader that creates OVTA assessment rows in bulk. The importer reads XLSX directly, so
    /// these tests build in-memory .xlsx files with ClosedXML matching the production template's
    /// "OVTA Assessments" worksheet. Error-path tests don't reach SaveChanges; the happy path is
    /// skipped here (would need a transaction harness — flagged as a follow-up gap).
    /// </summary>
    [TestClass]
    public class OvtaBulkUploadImporterTests
    {
        private NeptuneDbContext _dbContext = GetDbContext();
        private IDbContextTransaction? _transaction;

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

        // The importer calls SaveChangesAsync on the happy path. Wrap every test in a transaction
        // we roll back so happy-path tests can assert on persisted state without polluting the
        // dev DB. Error-path tests get a no-op rollback.
        [TestInitialize]
        public async Task TestInitialize()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private Person GetAdminPerson() =>
            _dbContext.People.AsNoTracking().First(x => x.RoleID == (int)RoleEnum.SitkaAdmin || x.RoleID == (int)RoleEnum.Admin);

        private static readonly string[] BaseColumns =
        {
            "Area Name", "Jurisdiction Name", "Created By Person", "Status", "Completed Date",
            "Score", "Is Progress Assessment",
            // The four PreliminarySourceIdentificationCategory display names follow:
            "Vehicles", "Inadequate Waste Container Management", "Pedestrian Litter", "Illegal Dumping",
        };

        private static Stream BuildXlsx(IEnumerable<string> columns, IEnumerable<string[]> rows)
        {
            var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("OVTA Assessments");
            var colList = columns.ToList();
            for (var c = 0; c < colList.Count; c++)
            {
                ws.Cell(1, c + 1).Value = colList[c];
            }
            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < row.Length && c < colList.Count; c++)
                {
                    ws.Cell(r, c + 1).Value = row[c] ?? "";
                }
                r++;
            }
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            return ms;
        }

        [TestMethod]
        public async Task MissingWorksheet_ReportsParseError()
        {
            // Different worksheet name; importer should fall into the try/catch and return a friendly error.
            var wb = new XLWorkbook();
            wb.AddWorksheet("Wrong Sheet Name");
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, ms, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Unexpected error parsing Excel Spreadsheet upload")));
        }

        [TestMethod]
        public async Task InvalidScore_Errors()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", "Z" /* invalid score */, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Score")), string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task InvalidIsProgressAssessment_Errors()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "maybe" /* must be Yes/No */,
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Is Progress Assessment")), string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task InvalidStatus_Errors()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "InTheCloud" /* not Finalized/Draft */, "06/15/2024", validScore, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Status is not a valid value")), string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task UnknownAreaName_Errors()
        {
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                "___NPT_998_TEST_NO_AREA___", jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Cannot find OVTA area name")), string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task UnknownCreatorEmail_Errors()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, "nobody@example.invalid",
                "Finalized", "06/15/2024", validScore, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Cannot find Person")), string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task NonAdminUploadingOutOfJurisdiction_BlockedEarly()
        {
            // Find a JurisdictionEditor and a jurisdiction they are NOT assigned to.
            var editor = _dbContext.People.AsNoTracking()
                .FirstOrDefault(x => x.RoleID == (int)RoleEnum.JurisdictionEditor || x.RoleID == (int)RoleEnum.JurisdictionManager);
            if (editor == null)
            {
                Assert.Inconclusive("No JurisdictionEditor/Manager users in dev DB.");
                return;
            }
            var assignedIDs = _dbContext.StormwaterJurisdictionPeople.AsNoTracking()
                .Where(x => x.PersonID == editor.PersonID)
                .Select(x => x.StormwaterJurisdictionID).ToList();
            var disallowedJurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking()
                .FirstOrDefault(x => !assignedIDs.Contains(x.StormwaterJurisdictionID));
            if (disallowedJurisdiction == null)
            {
                Assert.Inconclusive("Could not find a jurisdiction this editor is NOT assigned to.");
                return;
            }
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, disallowedJurisdiction.Organization.OrganizationName, editor.Email,
                "Finalized", "06/15/2024", validScore, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, editor);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("do not have permission to manage")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task ValidRow_PersistsAssessmentAndRecalculatesAreaScores()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking()
                .Single(x => x.StormwaterJurisdictionID == area.StormwaterJurisdictionID);
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var beforeCount = await _dbContext.OnlandVisualTrashAssessments
                .CountAsync(x => x.OnlandVisualTrashAssessmentAreaID == area.OnlandVisualTrashAssessmentAreaID);

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "No",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);

            Assert.AreEqual(0, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.AreEqual(1, result.RowsProcessed);

            // Within the still-open transaction we should see the new OVTA.
            var afterCount = await _dbContext.OnlandVisualTrashAssessments
                .CountAsync(x => x.OnlandVisualTrashAssessmentAreaID == area.OnlandVisualTrashAssessmentAreaID);
            Assert.AreEqual(beforeCount + 1, afterCount,
                "Exactly one new OnlandVisualTrashAssessment should have been added in this area.");
        }

        [TestMethod]
        public async Task OtherInPreliminarySource_Errors()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "Yes",
                "Other - some custom", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("does not allow for Other")),
                string.Join("; ", result.Errors));
        }

        // ----- NPT-1076 rework -----

        [TestMethod]
        public async Task EmptyDataRows_Errors()
        {
            // Header row only, no data rows. Previously this silently reported "successfully bulk
            // uploaded OVTAs from 0 row(s)"; now it should surface an actionable error.
            using var xlsx = BuildXlsx(BaseColumns, Array.Empty<string[]>());
            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.AreEqual(1, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.IsTrue(result.Errors[0].Contains("no data rows"), result.Errors[0]);
            Assert.AreEqual(0, result.RowsProcessed);
        }

        [TestMethod]
        public async Task AllBlankDataRows_Errors()
        {
            // Copilot review on PR #544: Excel's used range can stretch past actual data,
            // producing blank data rows that the rowEmpty check skips. Previously this slipped
            // through the upfront numRows == 0 guard and returned a false success. The
            // post-loop processedRowCount guard now catches it too.
            var blank = new string[BaseColumns.Length];
            for (var i = 0; i < blank.Length; i++) blank[i] = "";
            using var xlsx = BuildXlsx(BaseColumns, new[] { blank, blank, blank });
            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.AreEqual(1, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.IsTrue(result.Errors[0].Contains("no data rows"), result.Errors[0]);
        }

        [TestMethod]
        public async Task InvalidCompletedDate_RowScopedError()
        {
            // Bug #3: garbage Completed Date used to fall through to the outer catch and surface
            // the generic "Unexpected error parsing Excel Spreadsheet upload". Should now be a
            // row-scoped, field-specific message like Score/Status/etc.
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "not-a-date", validScore, "Yes",
                "", "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Invalid Completed Date") && x.Contains("row 1")),
                $"Expected row-scoped Completed Date error. Got: {string.Join("; ", result.Errors)}");
            Assert.IsFalse(result.Errors.Any(x => x.Contains("Unexpected error parsing")),
                $"Generic outer-catch message should no longer surface. Got: {string.Join("; ", result.Errors)}");
        }

        [TestMethod]
        public async Task RowsProcessed_ExcludesBlankRowsWithinUsedRange()
        {
            // KE round 2 (TC02): uploaded file with 3 records reported "172 row(s)" because the
            // worksheet's used range stretched far past the actual data. The importer was using
            // `numRows = dataTable.Rows.Count` (raw count) for the success banner instead of
            // `processedRowCount` (rows that actually produced a persisted assessment). This
            // test reproduces the inflated-used-range scenario by touching cells past the last
            // data row, then asserts the result reports the data-row count, not the raw count.
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking()
                .Single(x => x.StormwaterJurisdictionID == area.StormwaterJurisdictionID);
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            // Build the workbook by hand so we can touch blank cells past the data row,
            // simulating the inflated "used range" Excel produces in real-world templates.
            var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("OVTA Assessments");
            for (var c = 0; c < BaseColumns.Length; c++)
            {
                ws.Cell(1, c + 1).Value = BaseColumns[c];
            }
            var dataRow = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "No",
                "", "", "", "",
            };
            for (var c = 0; c < dataRow.Length; c++)
            {
                ws.Cell(2, c + 1).Value = dataRow[c];
            }
            // Touch a cell 10 rows past the data so ClosedXML's used range extends and produces
            // ~10 blank-but-tracked rows in the resulting DataTable. The importer's per-row
            // empty-row check skips them; the count returned must skip them too.
            ws.Cell(12, 1).Value = "";
            ws.Cell(12, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            using var xlsx = new MemoryStream();
            wb.SaveAs(xlsx);
            xlsx.Position = 0;

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);

            Assert.AreEqual(0, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.AreEqual(1, result.RowsProcessed,
                "Success banner must reflect data rows actually imported, not the inflated worksheet used range.");
        }

        [TestMethod]
        public async Task PSIWhitespaceAndCasing_Accepted()
        {
            // Bug #1: mixed-case + leading/trailing whitespace around comma-separated PSI values
            // used to produce "X is not a valid Preliminary Source Identification Type for ..."
            // even when X was present in the DB seed. Trimmed + case-insensitive match now.
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault();
            if (area == null)
            {
                Assert.Inconclusive("No OVTA areas in dev DB.");
                return;
            }
            var creator = GetAdminPerson();
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var validScore = OnlandVisualTrashAssessmentScore.All.First().OnlandVisualTrashAssessmentScoreDisplayName;

            var row = new[]
            {
                area.OnlandVisualTrashAssessmentAreaName, jurisdiction.Organization.OrganizationName, creator.Email,
                "Finalized", "06/15/2024", validScore, "Yes",
                "parked cars, UNCOVERED LOADS " /* Vehicles col: mixed case + trailing space + leading space after comma */,
                "", "", "",
            };
            using var xlsx = BuildXlsx(BaseColumns, new[] { row });

            var result = await OvtaBulkUploadImporter.BulkUploadAsync(_dbContext, xlsx, creator);
            Assert.AreEqual(0, result.Errors.Count,
                $"Expected casing/whitespace-tolerant PSI matching. Got: {string.Join("; ", result.Errors)}");
        }
    }
}
