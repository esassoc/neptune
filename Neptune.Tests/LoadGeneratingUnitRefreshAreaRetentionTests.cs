using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1158: refresh areas are pruned by the order they were queued, because the table has no
    /// created date. Each test runs in a transaction that is rolled back, and uses dates far in the
    /// future so the rows already in the database are all "older than the retention period".
    /// </summary>
    [TestClass]
    public class LoadGeneratingUnitRefreshAreaRetentionTests
    {
        private static readonly DateTime FarFuture = new(2100, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

        [TestInitialize]
        public void Setup()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(TestDatabase.ConnectionString, x =>
            {
                x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                x.UseNetTopologySuite();
            });
            _dbContext = new NeptuneDbContext(optionsBuilder.Options);
            _transaction = _dbContext.Database.BeginTransaction();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private int AddRefreshArea(DateTime? processDate)
        {
            var refreshArea = new LoadGeneratingUnitRefreshArea
            {
                LoadGeneratingUnitRefreshAreaGeometry = new Polygon(new LinearRing(new[]
                {
                    new Coordinate(1850000, 660000), new Coordinate(1850010, 660000),
                    new Coordinate(1850010, 660010), new Coordinate(1850000, 660010), new Coordinate(1850000, 660000),
                })) { SRID = 2771 },
                ProcessDate = processDate,
            };
            _dbContext.LoadGeneratingUnitRefreshAreas.Add(refreshArea);
            _dbContext.SaveChanges();
            return refreshArea.LoadGeneratingUnitRefreshAreaID;
        }

        private bool Exists(int refreshAreaID) =>
            _dbContext.LoadGeneratingUnitRefreshAreas.AsNoTracking().Any(x => x.LoadGeneratingUnitRefreshAreaID == refreshAreaID);

        [TestMethod]
        public async Task DeleteOlderThanRetentionPeriod_RemovesEverythingQueuedBeforeTheOldestRowInsideThePeriod()
        {
            var completedLongAgo = AddRefreshArea(FarFuture.AddDays(-LoadGeneratingUnitRefreshAreas.RetentionDays - 30));
            var neverCompletedAndOld = AddRefreshArea(null);
            var completedInsidePeriod = AddRefreshArea(FarFuture.AddDays(-10));
            var stillQueued = AddRefreshArea(null);
            var completedJustNow = AddRefreshArea(FarFuture);

            var deletedCount = await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(_dbContext, FarFuture);

            Assert.IsTrue(deletedCount >= 2);
            Assert.IsFalse(Exists(completedLongAgo), "completed before the retention period");
            Assert.IsFalse(Exists(neverCompletedAndOld), "never completed, and queued before the oldest row inside the period");
            Assert.IsTrue(Exists(completedInsidePeriod));
            Assert.IsTrue(Exists(stillQueued), "queued after the oldest row inside the period, so it may still be waiting to run");
            Assert.IsTrue(Exists(completedJustNow));
        }

        [TestMethod]
        public async Task DeleteOlderThanRetentionPeriod_DeletesNothing_WhenNoRowCompletedInsideThePeriod()
        {
            var neverCompleted = AddRefreshArea(null);
            var countBefore = _dbContext.LoadGeneratingUnitRefreshAreas.Count();

            // every row in the table completed long before this date, or never completed
            var deletedCount = await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(_dbContext, FarFuture);

            Assert.AreEqual(0, deletedCount);
            Assert.AreEqual(countBefore, _dbContext.LoadGeneratingUnitRefreshAreas.Count());
            Assert.IsTrue(Exists(neverCompleted));
        }
    }
}
