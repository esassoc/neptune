using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1077: covers the refactored Land Use Block GDB upload pipeline:
    /// - <see cref="LandUseBlocks.FromStaging"/> — staging→production mapping (regression guard
    ///   for the income-zeroing bug fix).
    /// - <see cref="LandUseBlockStagings.ValidateStagings"/> — aggregated PriorityLandUseType +
    ///   PermitType validation that the new staging-report endpoint surfaces synchronously.
    /// - <see cref="LandUseBlockStagings.BuildReportForCurrentUserAsync"/> — end-to-end report
    ///   builder (DB-backed; falls back to Inconclusive if the dev DB lacks the prerequisites).
    /// </summary>
    [TestClass]
    public class LandUseBlockUploadTests
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

        // A square in NAD 83 HARN CA Zone VI (SRID 2771) — values are well within Orange County range.
        // Geometry must be set with the right SRID so ProjectTo4326() inside FromStaging() works.
        private static Geometry BuildSquare(double originX = 6_100_000, double originY = 2_200_000, double side = 100)
        {
            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(2771);
            return factory.CreatePolygon(new[]
            {
                new Coordinate(originX, originY),
                new Coordinate(originX + side, originY),
                new Coordinate(originX + side, originY + side),
                new Coordinate(originX, originY + side),
                new Coordinate(originX, originY),
            });
        }

        // ----- FromStaging mapper -----

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_ForResidential()
        {
            var staging = NewStaging(landUseForTGR: "RESIDENTIAL", residential: 75000m, retail: 100000m);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.AreEqual(75000m, lub.MedianHouseholdIncomeResidential);
            Assert.AreEqual(100000m, lub.MedianHouseholdIncomeRetail);
        }

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_ForRetail()
        {
            var staging = NewStaging(landUseForTGR: "RETAIL", residential: 75000m, retail: 100000m);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.AreEqual(75000m, lub.MedianHouseholdIncomeResidential);
            Assert.AreEqual(100000m, lub.MedianHouseholdIncomeRetail);
        }

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_ForCommercial()
        {
            // The legacy bug: any LandUseForTGR not exactly "RESIDENTIAL"/"RETAIL" zeroed both income
            // fields. Confirm the regression: both are preserved.
            var staging = NewStaging(landUseForTGR: "COMMERCIAL", residential: 65000m, retail: 88000m);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.AreEqual(65000m, lub.MedianHouseholdIncomeResidential);
            Assert.AreEqual(88000m, lub.MedianHouseholdIncomeRetail);
        }

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_WhenLandUseForTGR_IsNull()
        {
            var staging = NewStaging(landUseForTGR: null, residential: 50000m, retail: 60000m);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.AreEqual(50000m, lub.MedianHouseholdIncomeResidential);
            Assert.AreEqual(60000m, lub.MedianHouseholdIncomeRetail);
        }

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_WhenLandUseForTGR_IsEmpty()
        {
            var staging = NewStaging(landUseForTGR: "", residential: 50000m, retail: 60000m);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.AreEqual(50000m, lub.MedianHouseholdIncomeResidential);
            Assert.AreEqual(60000m, lub.MedianHouseholdIncomeRetail);
        }

        [TestMethod]
        public void FromStaging_PreservesIncomeValues_WhenBothAreNull()
        {
            var staging = NewStaging(landUseForTGR: "RESIDENTIAL", residential: null, retail: null);
            var lub = LandUseBlocks.FromStaging(staging);
            Assert.IsNull(lub.MedianHouseholdIncomeResidential);
            Assert.IsNull(lub.MedianHouseholdIncomeRetail);
        }

        // ----- ValidateStagings — pure unit -----

        [TestMethod]
        public void ValidateStagings_EmptyList_ReturnsNoErrors()
        {
            // Empty staging is NOT flagged here — ValidateStagings only inspects per-row field
            // validity. The "no rows to import" guard is owned by the controller's ApproveStaging
            // endpoint and the background job (Copilot review on PR #541). Documenting the
            // contract here so a future regression that flips it doesn't go unnoticed.
            var errors = LandUseBlockStagings.ValidateStagings(new List<LandUseBlockStaging>());
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void ValidateStagings_AllValid_ReturnsNoErrors()
        {
            var stagings = new List<LandUseBlockStaging>
            {
                NewStaging(),
                NewStaging(),
            };
            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }

        [TestMethod]
        public void ValidateStagings_AggregatesIdenticalBadPriorityLandUseType_IntoOneLine()
        {
            // Three rows share the same bad value; should produce ONE error line referencing that value
            // with a count of 3, not three separate lines.
            var stagings = new List<LandUseBlockStaging>
            {
                NewStaging(priorityLandUseType: "Comercial"), // typo
                NewStaging(priorityLandUseType: "Comercial"),
                NewStaging(priorityLandUseType: "Comercial"),
            };
            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            var priorityErrors = errors.Where(x => x.Contains("PriorityLandUseType")).ToList();
            Assert.AreEqual(1, priorityErrors.Count, $"Expected one aggregated error. Got: {string.Join("; ", errors)}");
            Assert.IsTrue(priorityErrors[0].Contains("'Comercial'"));
            Assert.IsTrue(priorityErrors[0].Contains("3 row(s)"));
        }

        [TestMethod]
        public void ValidateStagings_UsesOneBasedRowNumbers()
        {
            // Single bad row, second position → should reference "row(s) 2".
            var stagings = new List<LandUseBlockStaging>
            {
                NewStaging(),
                NewStaging(priorityLandUseType: "BadValue"),
                NewStaging(),
            };
            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            var priorityError = errors.Single(x => x.Contains("PriorityLandUseType"));
            Assert.IsTrue(priorityError.Contains("e.g. row(s) 2"), $"Expected 1-indexed row number. Got: {priorityError}");
        }

        [TestMethod]
        public void ValidateStagings_FlagsBlankPermitType_Separately()
        {
            // Build the empty/null permit rows inline so the NewStaging helper's null-default
            // fallback doesn't accidentally re-fill them with a valid value.
            var blankRow = NewStaging();
            blankRow.PermitType = "";
            var nullRow = NewStaging();
            nullRow.PermitType = null;
            var stagings = new List<LandUseBlockStaging> { blankRow, nullRow };

            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            var blankError = errors.SingleOrDefault(x => x.Contains("PermitType") && x.Contains("blank"));
            Assert.IsNotNull(blankError, $"Expected a 'PermitType is blank' aggregated error. Got: {string.Join("; ", errors)}");
            Assert.IsTrue(blankError.Contains("2 row(s)"), $"Expected the aggregated count to be 2. Got: {blankError}");
        }

        [TestMethod]
        public void ValidateStagings_BeyondSampleRows_TruncatesWithEllipsis()
        {
            // 10 bad rows; the aggregated message should show the first 5 + a "… and 5 more" tail.
            var stagings = Enumerable.Range(0, 10)
                .Select(_ => NewStaging(priorityLandUseType: "BadValue"))
                .ToList();
            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            var priorityError = errors.Single(x => x.Contains("PriorityLandUseType"));
            Assert.IsTrue(priorityError.Contains("10 row(s)"));
            Assert.IsTrue(priorityError.Contains("and 5 more"), $"Expected truncation tail. Got: {priorityError}");
        }

        [TestMethod]
        public void ValidateStagings_ListsAllowedValues_ToHelpTheUser()
        {
            var stagings = new List<LandUseBlockStaging> { NewStaging(priorityLandUseType: "BadValue") };
            var errors = LandUseBlockStagings.ValidateStagings(stagings);
            var priorityError = errors.Single(x => x.Contains("PriorityLandUseType"));
            // We don't hardcode every allowed value name (seed data could shift); just confirm at
            // least one real value is mentioned.
            var anyAllowed = PriorityLandUseType.All.Select(x => x.PriorityLandUseTypeDisplayName);
            Assert.IsTrue(anyAllowed.Any(name => priorityError.Contains(name)), $"Error must list allowed values. Got: {priorityError}");
        }

        // ----- BuildReportForCurrentUserAsync — DB-backed integration -----

        [TestMethod]
        public async Task BuildReportForCurrentUserAsync_NoStaging_ReturnsZeros()
        {
            // -1 isn't a real person — guaranteed no staging rows. Should produce a clean empty report.
            var report = await LandUseBlockStagings.BuildReportForCurrentUserAsync(_dbContext, -1);
            Assert.IsNotNull(report);
            Assert.AreEqual(0, report.TotalStagedRowCount);
            Assert.AreEqual(0, report.ExistingRowsToReplace);
            Assert.AreEqual(0, report.Errors.Count);
        }

        // ----- helpers -----

        private static LandUseBlockStaging NewStaging(
            string priorityLandUseType = null,
            string permitType = null,
            string landUseForTGR = "RESIDENTIAL",
            decimal? residential = 50000m,
            decimal? retail = 50000m)
        {
            return new LandUseBlockStaging
            {
                StormwaterJurisdictionID = 1,
                UploadedByPersonID = 1,
                PriorityLandUseType = priorityLandUseType ?? PriorityLandUseType.All.First().PriorityLandUseTypeDisplayName,
                PermitType = permitType ?? PermitType.All.First().PermitTypeDisplayName,
                LandUseDescription = "Test description",
                TrashGenerationRate = 2.5m,
                LandUseForTGR = landUseForTGR,
                MedianHouseholdIncomeResidential = residential,
                MedianHouseholdIncomeRetail = retail,
                Geometry = BuildSquare(),
            };
        }
    }
}
