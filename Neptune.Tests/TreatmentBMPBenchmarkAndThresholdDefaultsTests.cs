using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1069: helpers that seed default Benchmark & Threshold rows when a Treatment BMP is
    /// created via the API single-create or bulk-upload paths. Mirrors the legacy MVC behavior
    /// in <c>Neptune.WebMvc/Views/TreatmentBMP/NewViewModel.cs:113-129</c>.
    /// </summary>
    [TestClass]
    public class TreatmentBMPBenchmarkAndThresholdDefaultsTests
    {
        private NeptuneDbContext _dbContext = GetDbContext();

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;", x =>
                {
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestMethod]
        public async Task BuildSeedTemplatesAsync_TypeWithQualifyingObservationTypes_ReturnsExpectedTemplates()
        {
            // Find any TreatmentBMPType whose join has at least one row with both defaults set; if none
            // exists in this environment the test is inconclusive rather than failing — the seeding
            // logic is the same regardless and the negative-case test below still exercises the empty path.
            var candidateTypeID = await _dbContext.TreatmentBMPTypeAssessmentObservationTypes
                .AsNoTracking()
                .Where(x => x.DefaultBenchmarkValue.HasValue && x.DefaultThresholdValue.HasValue)
                .Select(x => x.TreatmentBMPTypeID)
                .FirstOrDefaultAsync();
            if (candidateTypeID == 0)
            {
                Assert.Inconclusive("No TreatmentBMPType in this database has any TreatmentBMPTypeAssessmentObservationType with both default values populated — cannot exercise the positive seeding path.");
                return;
            }

            // Independent baseline: enumerate the join rows for the type that have both defaults set
            // AND whose OT entity reports GetHasBenchmarkAndThreshold(). Materialize then filter so
            // GetHasBenchmarkAndThreshold (which parses the OT schema JSON) runs in-memory.
            var joinRowsWithDefaults = await _dbContext.TreatmentBMPTypeAssessmentObservationTypes
                .AsNoTracking()
                .Where(x => x.TreatmentBMPTypeID == candidateTypeID
                         && x.DefaultBenchmarkValue.HasValue
                         && x.DefaultThresholdValue.HasValue)
                .ToListAsync();
            var observationTypeIDs = joinRowsWithDefaults.Select(x => x.TreatmentBMPAssessmentObservationTypeID).Distinct().ToList();
            var observationTypes = await _dbContext.TreatmentBMPAssessmentObservationTypes
                .AsNoTracking()
                .Where(x => observationTypeIDs.Contains(x.TreatmentBMPAssessmentObservationTypeID))
                .ToDictionaryAsync(x => x.TreatmentBMPAssessmentObservationTypeID);
            var expectedQualifying = joinRowsWithDefaults
                .Where(j => observationTypes[j.TreatmentBMPAssessmentObservationTypeID].GetHasBenchmarkAndThreshold())
                .ToList();

            var seedTemplates = await TreatmentBMPBenchmarkAndThresholds.BuildSeedTemplatesAsync(_dbContext, candidateTypeID);

            Assert.AreEqual(expectedQualifying.Count, seedTemplates.Count,
                "Helper should return one template per join row with both defaults set AND a benchmark/threshold-bearing collection method.");
            foreach (var template in seedTemplates)
            {
                Assert.AreEqual(candidateTypeID, template.TreatmentBMPTypeID);
                var matchedJoin = expectedQualifying.Single(j => j.TreatmentBMPTypeAssessmentObservationTypeID == template.TreatmentBMPTypeAssessmentObservationTypeID);
                Assert.AreEqual(matchedJoin.TreatmentBMPAssessmentObservationTypeID, template.TreatmentBMPAssessmentObservationTypeID);
                Assert.AreEqual(matchedJoin.DefaultBenchmarkValue!.Value, template.BenchmarkValue);
                Assert.AreEqual(matchedJoin.DefaultThresholdValue!.Value, template.ThresholdValue);
            }
        }

        [TestMethod]
        public async Task BuildSeedTemplatesAsync_TypeWithNoQualifyingObservationTypes_ReturnsEmpty()
        {
            // -1 is guaranteed not to be a real TreatmentBMPTypeID — exercises the "no qualifying join
            // rows" early-return without depending on a particular database state.
            var seedTemplates = await TreatmentBMPBenchmarkAndThresholds.BuildSeedTemplatesAsync(_dbContext, -1);

            Assert.IsNotNull(seedTemplates);
            Assert.AreEqual(0, seedTemplates.Count, "Unknown TreatmentBMPTypeID should yield an empty (not null) list.");
        }

        [TestMethod]
        public void AttachSeedsToBMP_AppendsOneEntityPerTemplate_WithCorrectFKsAndValues()
        {
            var bmp = new TreatmentBMP { TreatmentBMPName = "Test", TreatmentBMPTypeID = 99, StormwaterJurisdictionID = 1 };
            var templates = new List<TreatmentBMPBenchmarkAndThresholds.TreatmentBMPBenchmarkAndThresholdSeed>
            {
                new(TreatmentBMPTypeID: 99, TreatmentBMPTypeAssessmentObservationTypeID: 10, TreatmentBMPAssessmentObservationTypeID: 1, BenchmarkValue: 1.5, ThresholdValue: 2.5),
                new(TreatmentBMPTypeID: 99, TreatmentBMPTypeAssessmentObservationTypeID: 11, TreatmentBMPAssessmentObservationTypeID: 2, BenchmarkValue: 3.0, ThresholdValue: 4.0),
            };

            TreatmentBMPBenchmarkAndThresholds.AttachSeedsToBMP(bmp, templates);

            Assert.AreEqual(2, bmp.TreatmentBMPBenchmarkAndThresholds.Count);
            var first = bmp.TreatmentBMPBenchmarkAndThresholds.Single(x => x.TreatmentBMPAssessmentObservationTypeID == 1);
            Assert.AreSame(bmp, first.TreatmentBMP);
            Assert.AreEqual(99, first.TreatmentBMPTypeID);
            Assert.AreEqual(10, first.TreatmentBMPTypeAssessmentObservationTypeID);
            Assert.AreEqual(1.5, first.BenchmarkValue);
            Assert.AreEqual(2.5, first.ThresholdValue);
        }

        [TestMethod]
        public void AttachSeedsToBMP_EmptyTemplates_LeavesCollectionUnchanged()
        {
            var bmp = new TreatmentBMP { TreatmentBMPName = "Test", TreatmentBMPTypeID = 99, StormwaterJurisdictionID = 1 };

            TreatmentBMPBenchmarkAndThresholds.AttachSeedsToBMP(bmp, new List<TreatmentBMPBenchmarkAndThresholds.TreatmentBMPBenchmarkAndThresholdSeed>());

            Assert.AreEqual(0, bmp.TreatmentBMPBenchmarkAndThresholds.Count);
        }
    }
}
