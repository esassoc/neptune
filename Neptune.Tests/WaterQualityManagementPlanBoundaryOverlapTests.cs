using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155: the WQMP Boundary Overlap Report. The view lists pairs of overlapping boundaries
    /// among the plans that are Load Generating Unit inputs (Active, not Detailed); the grid row says
    /// whether the model counts the shared ground twice. The view tests run against the local DB and
    /// are rolled back; they need the view deployed there.
    /// </summary>
    [TestClass]
    public class WaterQualityManagementPlanBoundaryOverlapTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                TestDatabase.ConnectionString,
                x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestInitialize]
        public void Setup()
        {
            _dbContext = GetDbContext();
            _transaction = _dbContext.Database.BeginTransaction();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private vWaterQualityManagementPlanBoundaryOverlap? AnyOverlap()
        {
            return _dbContext.vWaterQualityManagementPlanBoundaryOverlaps.AsNoTracking().OrderBy(x => x.WaterQualityManagementPlanID).FirstOrDefault();
        }

        private int CountOverlapsBetween(int waterQualityManagementPlanID, int overlappingWaterQualityManagementPlanID)
        {
            return _dbContext.vWaterQualityManagementPlanBoundaryOverlaps.AsNoTracking()
                .Count(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID && x.OverlappingWaterQualityManagementPlanID == overlappingWaterQualityManagementPlanID);
        }

        [TestMethod]
        public void View_ListsEachPairOnce_LowerIDFirst_AboveTheFloor()
        {
            var overlaps = _dbContext.vWaterQualityManagementPlanBoundaryOverlaps.AsNoTracking().ToList();
            if (!overlaps.Any()) Assert.Inconclusive("No overlapping WQMP boundaries in the local DB.");

            Assert.IsTrue(overlaps.All(x => x.WaterQualityManagementPlanID < x.OverlappingWaterQualityManagementPlanID));
            Assert.AreEqual(overlaps.Count, overlaps.Select(x => (x.WaterQualityManagementPlanID, x.OverlappingWaterQualityManagementPlanID)).Distinct().Count());
            Assert.IsTrue(overlaps.All(x => x.OverlapAreaInSquareMeters > 200));
        }

        [TestMethod]
        [DataRow((int)WaterQualityManagementPlanStatusEnum.Inactive)]
        [DataRow((int)WaterQualityManagementPlanStatusEnum.Draft)]
        public void View_DropsAPair_WhenOneOfThePlansLeavesActive(int newStatusID)
        {
            var overlap = AnyOverlap();
            if (overlap == null) Assert.Inconclusive("No overlapping WQMP boundaries in the local DB.");

            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.WaterQualityManagementPlan SET WaterQualityManagementPlanStatusID = {0} WHERE WaterQualityManagementPlanID = {1}",
                newStatusID, overlap.OverlappingWaterQualityManagementPlanID);

            Assert.AreEqual(0, CountOverlapsBetween(overlap.WaterQualityManagementPlanID, overlap.OverlappingWaterQualityManagementPlanID));
        }

        [TestMethod]
        public void View_DropsAPair_WhenOneOfThePlansBecomesDetailed()
        {
            var overlap = AnyOverlap();
            if (overlap == null) Assert.Inconclusive("No overlapping WQMP boundaries in the local DB.");

            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.WaterQualityManagementPlan SET WaterQualityManagementPlanModelingApproachID = {0} WHERE WaterQualityManagementPlanID = {1}",
                (int)WaterQualityManagementPlanModelingApproachEnum.Detailed, overlap.WaterQualityManagementPlanID);

            Assert.AreEqual(0, CountOverlapsBetween(overlap.WaterQualityManagementPlanID, overlap.OverlappingWaterQualityManagementPlanID));
        }

        [TestMethod]
        [DataRow(true, true, true)]
        [DataRow(false, false, true)]
        [DataRow(true, false, false)]
        [DataRow(false, true, false)]
        public void GridRow_IsCountedTwice_UnlessExactlyOnePlanTreats(bool treatsRunoff, bool overlappingTreatsRunoff, bool expectedIsCountedTwice)
        {
            var overlap = new vWaterQualityManagementPlanBoundaryOverlap
            {
                WaterQualityManagementPlanID = 1,
                OverlappingWaterQualityManagementPlanID = 2,
                OverlapAreaInSquareMeters = 4046.8564,
            };

            var dto = vWaterQualityManagementPlanBoundaryOverlaps.AsGridDto(overlap, treatsRunoff, overlappingTreatsRunoff);

            Assert.AreEqual(expectedIsCountedTwice, dto.IsCountedTwice);
            Assert.AreEqual(treatsRunoff, dto.TreatsRunoff);
            Assert.AreEqual(overlappingTreatsRunoff, dto.OverlappingTreatsRunoff);
            Assert.AreEqual(1.0, dto.AreaOfOverlapInAcres, 0.001);
        }
    }
}
