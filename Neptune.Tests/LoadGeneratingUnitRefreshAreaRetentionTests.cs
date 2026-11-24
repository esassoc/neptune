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
        public async Task DeleteOlderThanRetentionPeriod_RemovesTheNewestRowKnownToBeOldAndEverythingQueuedBeforeIt()
        {
            var longAgo = FarFuture.AddDays(-LoadGeneratingUnitRefreshAreas.RetentionDays - 30);
            var completedLongAgo = AddRefreshArea(longAgo);
            var neverCompletedAndOld = AddRefreshArea(null);
            var newestCompletedLongAgo = AddRefreshArea(longAgo.AddDays(1));
            var completedInsidePeriod = AddRefreshArea(FarFuture.AddDays(-10));
            var stillQueued = AddRefreshArea(null);
            var completedJustNow = AddRefreshArea(FarFuture);

            var deletedCount = await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(_dbContext, FarFuture);

            Assert.IsTrue(deletedCount >= 3);
            Assert.IsFalse(Exists(completedLongAgo), "completed before the retention period");
            Assert.IsFalse(Exists(neverCompletedAndOld), "never completed, but queued before a row that is itself older than the period");
            Assert.IsFalse(Exists(newestCompletedLongAgo), "completed before the retention period");
            Assert.IsTrue(Exists(completedInsidePeriod));
            Assert.IsTrue(Exists(stillQueued), "nothing queued after it is known to be old, so it may still be waiting to run");
            Assert.IsTrue(Exists(completedJustNow));
        }

        [TestMethod]
        public async Task DeleteOlderThanRetentionPeriod_KeepsARecentFailure_AfterALongQuietSpell()
        {
            // Nothing has completed for longer than the retention period (every row already in the
            // database is far older than FarFuture). A refresh then fails, and the next one succeeds.
            // The failed row is recent and holds the geometry needed to retry it, so it must stay,
            // even though it was queued before the only row that completed inside the period.
            var failedToday = AddRefreshArea(null);
            var succeededToday = AddRefreshArea(FarFuture);

            await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(_dbContext, FarFuture);

            Assert.IsTrue(Exists(failedToday), "a recent failure is kept");
            Assert.IsTrue(Exists(succeededToday));
        }

        [TestMethod]
        public async Task DeleteOlderThanRetentionPeriod_DeletesNothing_WhenNoRowIsKnownToBeOld()
        {
            var neverCompleted = AddRefreshArea(null);
            var countBefore = _dbContext.LoadGeneratingUnitRefreshAreas.Count();

            // a date before anything in the table completed, so no row is older than the period
            var deletedCount = await LoadGeneratingUnitRefreshAreas.DeleteOlderThanRetentionPeriodAsync(
                _dbContext, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.AreEqual(0, deletedCount);
            Assert.AreEqual(countBefore, _dbContext.LoadGeneratingUnitRefreshAreas.Count());
            Assert.IsTrue(Exists(neverCompleted));
        }
    }
}
