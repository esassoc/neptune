using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1128 rework: <see cref="LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID"/> is a LINQ spatial join
    /// (Intersects prefilter + Intersection().Area threshold) that must translate to SQL Server STIntersects /
    /// STIntersection().STArea(). These tests execute it against the local dev DB (read-only) so a translation
    /// failure surfaces here rather than on the OVTA Area grid. Follows the WaterQualityManagementPlanGdbExportTests pattern.
    /// </summary>
    [TestClass]
    public class LandUseBlocksOvtaAreaIntersectTests
    {
        private NeptuneDbContext _dbContext = null!;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestInitialize]
        public void Setup() => _dbContext = GetDbContext();

        [TestCleanup]
        public void Cleanup() => _dbContext.Dispose();

        // A jurisdiction that has both OVTA Areas and Land Use Blocks, so the join can produce rows.
        private int? JurisdictionWithAreasAndBlocks() =>
            _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(a => _dbContext.LandUseBlocks.Any(l => l.StormwaterJurisdictionID == a.StormwaterJurisdictionID))
                .GroupBy(a => a.StormwaterJurisdictionID)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefault();

        [TestMethod]
        public void ListByOnlandVisualTrashAssessmentAreaID_TranslatesAndReturnsOnlySameJurisdictionBlocks()
        {
            var jurisdictionID = JurisdictionWithAreasAndBlocks();
            if (jurisdictionID == null)
            {
                Assert.Inconclusive("Local DB has no jurisdiction with both OVTA Areas and Land Use Blocks.");
            }

            var result = LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID(_dbContext, new[] { jurisdictionID.Value });

            Assert.IsNotNull(result);
            var areaIDsInJurisdiction = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(a => a.StormwaterJurisdictionID == jurisdictionID.Value)
                .Select(a => a.OnlandVisualTrashAssessmentAreaID).ToHashSet();
            Assert.IsTrue(result.Keys.All(areaIDsInJurisdiction.Contains), "Every keyed area belongs to the requested jurisdiction.");

            var blockIDs = result.Values.SelectMany(x => x).Select(x => x.LandUseBlockID).Distinct().ToList();
            if (blockIDs.Count > 0)
            {
                var foreignBlocks = _dbContext.LandUseBlocks.AsNoTracking()
                    .Count(l => blockIDs.Contains(l.LandUseBlockID) && l.StormwaterJurisdictionID != jurisdictionID.Value);
                Assert.AreEqual(0, foreignBlocks, "No block from another jurisdiction is attached to an area.");
            }
        }

        [TestMethod]
        public void ListByOnlandVisualTrashAssessmentAreaID_EmptyJurisdictionList_ReturnsEmpty()
        {
            var result = LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID(_dbContext, Array.Empty<int>());
            Assert.AreEqual(0, result.Count);
        }
    }
}
