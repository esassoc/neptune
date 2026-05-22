using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-998 — covers WQMPAPNsCsvParserHelper.CSVUpload, the Data Hub WQMPs tab CSV uploader that
    /// rebuilds a WQMP's boundary from a list of parcel APNs. Tests verify header validation, WQMP
    /// lookup, APN handling (missing/duplicate), and the new-vs-existing boundary path. Read-only
    /// (controller saves).
    /// </summary>
    [TestClass]
    public class WQMPAPNsCsvParserHelperTests
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

        private static Stream CsvStream(string csv) => new MemoryStream(Encoding.UTF8.GetBytes(csv));

        [TestMethod]
        public void MissingRequiredHeader_ReturnsNullWithError()
        {
            // No "APNs" column.
            var csv = "WQMP,WQMP Boundary Notes\n";
            var result = WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), GetAnyJurisdictionID(),
                out var errors, out _, out _);
            Assert.IsNull(result);
            Assert.IsTrue(errors.Any(x => x.Contains("APNs")));
        }

        [TestMethod]
        public void DuplicateWQMPInCsv_Errors()
        {
            var csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                      "___NPT_998_TEST_DUP___,123-456-789,\n" +
                      "___NPT_998_TEST_DUP___,987-654-321,\n";
            WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), GetAnyJurisdictionID(),
                out var errors, out _, out _);
            Assert.IsTrue(errors.Any(x => x.Contains("___NPT_998_TEST_DUP___") && x.Contains("multiple times")),
                string.Join("; ", errors));
        }

        [TestMethod]
        public void NonexistentWQMP_ReportedAsNotFound()
        {
            var csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                      "___NPT_998_TEST_NOWQMP___,123-456-789,\n";
            WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), GetAnyJurisdictionID(),
                out var errors, out _, out _);
            Assert.IsTrue(errors.Any(x => x.Contains("___NPT_998_TEST_NOWQMP___") && x.Contains("not found")),
                string.Join("; ", errors));
        }

        [TestMethod]
        public void EmptyAPNs_Errors()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            var csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                      $"{existingWqmp.WaterQualityManagementPlanName},,\n";
            WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), existingWqmp.StormwaterJurisdictionID,
                out var errors, out _, out _);
            Assert.IsTrue(errors.Any(x => x.Contains("APNs") && x.Contains("null, empty")),
                string.Join("; ", errors));
        }

        [TestMethod]
        public void NoneOfTheAPNsExist_MissingAPNsReportedAndNoBoundary()
        {
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }
            // Garbage APNs that we expect not to match any parcel.
            var csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                      $"{existingWqmp.WaterQualityManagementPlanName},___NPT_998_BAD_APN_A___;___NPT_998_BAD_APN_B___,\n";
            // The parser splits on comma but tolerates other delimiters via Trim; use commas in the field
            // (escaped via quotes) for the real splitter. Build properly:
            csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                  $"{existingWqmp.WaterQualityManagementPlanName},\"___NPT_998_BAD_APN_A___,___NPT_998_BAD_APN_B___\",\n";
            var result = WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), existingWqmp.StormwaterJurisdictionID,
                out var errors, out var missingApns, out _);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            CollectionAssert.AreEquivalent(
                new[] { "___NPT_998_BAD_APN_A___", "___NPT_998_BAD_APN_B___" },
                missingApns.ToList(),
                "Both APNs should be listed as missing.");
            Assert.AreEqual(0, result.Count, "No boundary should be returned when zero parcels matched.");
        }

        [TestMethod]
        public void ValidParcels_ReturnsBoundaryAndReportsMissingPartial()
        {
            // Find any parcel with a non-empty number and a geometry. Parcels aren't directly
            // jurisdiction-scoped (parcel→jurisdiction lookup is by geometry/spatial), so any
            // real parcel + any existing WQMP will exercise the matched/missing-APN split.
            var anyParcel = _dbContext.Parcels
                .AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.ParcelNumber))
                .Join(_dbContext.ParcelGeometries.AsNoTracking(), p => p.ParcelID, g => g.ParcelID, (p, _) => p)
                .FirstOrDefault();
            if (anyParcel == null)
            {
                Assert.Inconclusive("No parcels with geometries in dev DB.");
                return;
            }
            var existingWqmp = _dbContext.WaterQualityManagementPlans.AsNoTracking().FirstOrDefault();
            if (existingWqmp == null)
            {
                Assert.Inconclusive("No existing WQMP in dev DB.");
                return;
            }

            var csv = "WQMP,APNs,WQMP Boundary Notes\n" +
                      $"{existingWqmp.WaterQualityManagementPlanName},\"{anyParcel.ParcelNumber},___NPT_998_BAD_APN___\",test\n";
            var result = WQMPAPNsCsvParserHelper.CSVUpload(_dbContext, CsvStream(csv), existingWqmp.StormwaterJurisdictionID,
                out var errors, out var missingApns, out _);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
            Assert.AreEqual(1, result.Count);
            Assert.IsNotNull(result[0].GeometryNative, "Boundary geometry should be set from the matched parcel.");
            CollectionAssert.AreEquivalent(new[] { "___NPT_998_BAD_APN___" }, missingApns.ToList(),
                "Only the bad APN should appear in missingApns.");
        }
    }
}
