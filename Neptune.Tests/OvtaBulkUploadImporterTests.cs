using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
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
    }
}
