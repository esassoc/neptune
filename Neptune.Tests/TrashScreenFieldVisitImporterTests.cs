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
    /// NPT-998 — covers TrashScreenFieldVisitImporter.BulkUploadAsync, the Data Hub Trash Module
    /// tab uploader that creates trash-screen field visits + initial/post-maintenance assessments
    /// + maintenance records. The importer reads XLSX directly, so these tests build in-memory
    /// .xlsx files with ClosedXML matching the "Field Visits" worksheet shape. Tests focus on
    /// error paths (where the importer returns early without saving). Happy-path coverage left
    /// for a future tx-harnessed test.
    /// </summary>
    [TestClass]
    public class TrashScreenFieldVisitImporterTests
    {
        private const int InletAndTrashScreenTreatmentBMPTypeID = 35;

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

        // Column ordering must match the production template; the importer uses both column
        // names and indices (e.g. for the all-or-nothing assessment block checks).
        private static readonly string[] Columns =
        {
            "BMP Name", "Jurisdiction", "Field Visit Type", "Field Visit Date",

            // Initial assessment block (ends with "Notes")
            "Inlet Condition", "Inlet Condition Notes",
            "Outlet Condition", "Outlet Condition Notes",
            "Device Operability", "Device Operability Notes",
            "Significant Nuisance Conditions", "Significant Nuisance Conditions Notes",
            "Material Accumulation as Percent of Total System Volume",
            "Material Accumulation as Percent of Total System Volume Notes",

            // Maintenance record block
            "Maintenance Type", "Description",
            "Structural Repair Conducted", "Mechanical Repair Conducted",
            "Total Material Volume Removed (cu-ft)", "Total Material Volume Removed (gal)",
            "Percent Trash", "Percent Green Waste", "Percent Sediment",

            // Post-maintenance block
            "Inlet Condition (Post-Maintenance)", "Inlet Condition Notes (Post-Maintenance)",
            "Outlet Condition (Post-Maintenance)", "Outlet Condition Notes (Post-Maintenance)",
            "Device Operability (Post-Maintenance)", "Device Operability Notes (Post-Maintenance)",
            "Significant Nuisance Conditions (Post-Maintenance)", "Significant Nuisance Conditions Notes (Post-Maintenance)",
            "Material Accumulation as Percent of Total System Volume (Post-Maintenance)",
            "Material Accumulation as Percent of Total System Volume Notes (Post-Maintenance)",
        };

        private static string[] BlankRow()
        {
            var row = new string[Columns.Length];
            for (var i = 0; i < row.Length; i++) row[i] = "";
            return row;
        }

        private static void Set(string[] row, string col, string value)
        {
            row[Array.IndexOf(Columns, col)] = value;
        }

        private static Stream BuildXlsx(IEnumerable<string[]> rows)
        {
            var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Field Visits");
            for (var c = 0; c < Columns.Length; c++) ws.Cell(1, c + 1).Value = Columns[c];
            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < row.Length; c++) ws.Cell(r, c + 1).Value = row[c] ?? "";
                r++;
            }
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            return ms;
        }

        private TreatmentBMP GetAnyTrashScreenBMP() =>
            _dbContext.TreatmentBMPs.Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization).AsNoTracking()
                .FirstOrDefault(x => x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID);

        [TestMethod]
        public async Task MissingWorksheet_ReportsParseError()
        {
            var wb = new XLWorkbook();
            wb.AddWorksheet("Some Other Sheet");
            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;

            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, ms, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(),
                "Expected an error; importer should fail when the 'Field Visits' worksheet is missing.");
        }

        [TestMethod]
        public async Task UnknownBMPName_Errors()
        {
            var jurisdiction = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking().First();
            var row = BlankRow();
            Set(row, "BMP Name", "___NPT_998_TEST_NO_BMP___");
            Set(row, "Jurisdiction", jurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", "06/15/2024");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Invalid BMP Name or Jurisdiction")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task InvalidFieldVisitType_Errors()
        {
            var bmp = GetAnyTrashScreenBMP();
            if (bmp == null)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }
            var row = BlankRow();
            Set(row, "BMP Name", bmp.TreatmentBMPName);
            Set(row, "Jurisdiction", bmp.StormwaterJurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", "Telepathic");
            Set(row, "Field Visit Date", "06/15/2024");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Invalid Field Visit Type")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task InvalidFieldVisitDate_Errors()
        {
            var bmp = GetAnyTrashScreenBMP();
            if (bmp == null)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }
            var row = BlankRow();
            Set(row, "BMP Name", bmp.TreatmentBMPName);
            Set(row, "Jurisdiction", bmp.StormwaterJurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", "not-a-date");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Invalid Field Visit Date")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task InitialAssessmentPartialFill_ErrorsAllOrNothing()
        {
            var bmp = GetAnyTrashScreenBMP();
            if (bmp == null)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }
            var row = BlankRow();
            Set(row, "BMP Name", bmp.TreatmentBMPName);
            Set(row, "Jurisdiction", bmp.StormwaterJurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", "06/15/2024");
            // Fill in Inlet but skip Outlet / Operability / Nuisance / Accumulation — should trip
            // the "all or nothing" check.
            Set(row, "Inlet Condition", "Pass");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Initial Assessment")
                                                  && x.Contains("completely filled out or left completely blank")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task PostMaintenancePartialFill_ErrorsAllOrNothing()
        {
            var bmp = GetAnyTrashScreenBMP();
            if (bmp == null)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }
            var row = BlankRow();
            Set(row, "BMP Name", bmp.TreatmentBMPName);
            Set(row, "Jurisdiction", bmp.StormwaterJurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", "06/15/2024");
            // Trip the post-maintenance all-or-nothing check by setting only one column in the block.
            Set(row, "Inlet Condition (Post-Maintenance)", "Pass");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.IsTrue(result.Errors.Any(x => x.Contains("Post-Maintenance Assessment")
                                                  && x.Contains("completely filled out or left completely blank")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task NonAdminUploadingOutOfJurisdiction_BlockedEarly()
        {
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
            var disallowed = _dbContext.StormwaterJurisdictions.Include(x => x.Organization).AsNoTracking()
                .FirstOrDefault(x => !assignedIDs.Contains(x.StormwaterJurisdictionID));
            if (disallowed == null)
            {
                Assert.Inconclusive("Could not find a jurisdiction this editor is NOT assigned to.");
                return;
            }

            var row = BlankRow();
            Set(row, "BMP Name", "anything");
            Set(row, "Jurisdiction", disallowed.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", "06/15/2024");

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, editor);
            Assert.IsTrue(result.Errors.Any(x => x.Contains("do not have permission to manage")),
                string.Join("; ", result.Errors));
        }

        [TestMethod]
        public async Task EmptyRow_IsSkippedNoError()
        {
            using var xlsx = BuildXlsx(new[] { BlankRow() });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());
            Assert.AreEqual(0, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.AreEqual(0, result.RowsProcessed);
        }

        [TestMethod]
        public async Task ValidRow_NoAssessmentBlocks_PersistsFieldVisitShell()
        {
            // Minimal happy path: valid BMP + visit type + date, with blank initial-assessment
            // and post-maintenance blocks. The importer should create the FieldVisit row and
            // skip the assessment/maintenance sub-paths.
            var bmp = GetAnyTrashScreenBMP();
            if (bmp == null)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }
            // Use a date well in the future to avoid colliding with a pre-existing FieldVisit
            // for this BMP on the same day (which would update rather than insert).
            var visitDate = "01/01/2099";
            var row = BlankRow();
            Set(row, "BMP Name", bmp.TreatmentBMPName);
            Set(row, "Jurisdiction", bmp.StormwaterJurisdiction.Organization.OrganizationName);
            Set(row, "Field Visit Type", FieldVisitType.All.First().FieldVisitTypeDisplayName);
            Set(row, "Field Visit Date", visitDate);

            var beforeCount = await _dbContext.FieldVisits.CountAsync(x => x.TreatmentBMPID == bmp.TreatmentBMPID);

            using var xlsx = BuildXlsx(new[] { row });
            var result = await TrashScreenFieldVisitImporter.BulkUploadAsync(_dbContext, xlsx, GetAdminPerson());

            Assert.AreEqual(0, result.Errors.Count, string.Join("; ", result.Errors));
            Assert.AreEqual(1, result.RowsProcessed);

            var afterCount = await _dbContext.FieldVisits.CountAsync(x => x.TreatmentBMPID == bmp.TreatmentBMPID);
            Assert.AreEqual(beforeCount + 1, afterCount,
                "Exactly one new FieldVisit should have been added for this BMP.");
        }
    }
}
