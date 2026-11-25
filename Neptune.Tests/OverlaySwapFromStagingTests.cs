using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1156: the Trash Generating Unit refresh and the total Load Generating Unit refresh stage
    /// their units and replace the live tables in one transaction. A run that fails after the live
    /// rows have already been deleted must leave them exactly as they were. Runs against the local
    /// NeptuneDB inside a transaction that is rolled back.
    /// </summary>
    [TestClass]
    public class OverlaySwapFromStagingTests
    {
        private const int NonexistentID = -1;

        private NeptuneDbContext _dbContext = GetDbContext();
        private IDbContextTransaction? _transaction;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                TestDatabase.ConnectionString, x =>
                {
                    // each swap deletes and rebuilds every live unit
                    x.CommandTimeout((int)TimeSpan.FromMinutes(15).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

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
                try
                {
                    await _transaction.RollbackAsync();
                }
                catch (Exception)
                {
                    // a SQL error inside a swap procedure has already rolled the transaction back
                }
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            await _dbContext.DisposeAsync();
        }

        // a 100 m square in Orange County (EPSG:2771, meters)
        private static Geometry Square(double offset)
        {
            var x = 1850000 + offset;
            var geometry = new WKTReader().Read($"POLYGON (({x} 660000, {x + 100} 660000, {x + 100} 660100, {x} 660100, {x} 660000))");
            geometry.SRID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;
            return geometry;
        }

        #region Trash Generating Units

        private record LiveTrashGeneratingUnitSnapshot(int Count, long IDSum, DateTime? MaxLastUpdateDate, int Count4326, long IDSum4326, long LinkedIDSum4326);

        private async Task<LiveTrashGeneratingUnitSnapshot> TakeTrashGeneratingUnitSnapshot()
        {
            return new LiveTrashGeneratingUnitSnapshot(
                await _dbContext.TrashGeneratingUnits.CountAsync(),
                await _dbContext.TrashGeneratingUnits.SumAsync(x => (long)x.TrashGeneratingUnitID),
                await _dbContext.TrashGeneratingUnits.MaxAsync(x => x.LastUpdateDate),
                await _dbContext.TrashGeneratingUnit4326s.CountAsync(),
                await _dbContext.TrashGeneratingUnit4326s.SumAsync(x => (long)x.TrashGeneratingUnit4326ID),
                await _dbContext.TrashGeneratingUnit4326s.SumAsync(x => (long)(x.TrashGeneratingUnitID ?? 0)));
        }

        private async Task<int> AnyStormwaterJurisdictionID() =>
            await _dbContext.StormwaterJurisdictions.AsNoTracking().OrderBy(x => x.StormwaterJurisdictionID).Select(x => x.StormwaterJurisdictionID).FirstAsync();

        private static TrashGeneratingUnitStaging NewTrashGeneratingUnitStaging(int stormwaterJurisdictionID, double offset, int? delineationID = null)
        {
            var geometry = Square(offset);
            return new TrashGeneratingUnitStaging
            {
                StormwaterJurisdictionID = stormwaterJurisdictionID,
                DelineationID = delineationID,
                TrashGeneratingUnitGeometry = geometry,
                TrashGeneratingUnitGeometry4326 = geometry.ProjectTo4326(),
                LastUpdateDate = new DateTime(2000, 1, 1),
            };
        }

        [TestMethod]
        public async Task TrashGeneratingUnitSwap_ReplacesTheLiveUnitsWithStaging_InBothTables()
        {
            var stormwaterJurisdictionID = await AnyStormwaterJurisdictionID();
            await TrashGeneratingUnits.ReplaceStagingAsync(_dbContext,
                [NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 0), NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 1000)]);

            await TrashGeneratingUnits.SwapFromStagingAsync(_dbContext, 2);

            var units = await _dbContext.TrashGeneratingUnits.AsNoTracking().OrderBy(x => x.TrashGeneratingUnitID).ToListAsync();
            var units4326 = await _dbContext.TrashGeneratingUnit4326s.AsNoTracking().OrderBy(x => x.TrashGeneratingUnitID).ToListAsync();
            Assert.AreEqual(2, units.Count);
            Assert.AreEqual(2, units4326.Count);
            CollectionAssert.AreEqual(units.Select(x => (int?)x.TrashGeneratingUnitID).ToList(), units4326.Select(x => x.TrashGeneratingUnitID).ToList());
            // each map (4326) row is the same square as the unit it points at
            for (var i = 0; i < units.Count; i++)
            {
                var expected4326 = units[i].TrashGeneratingUnitGeometry.ProjectTo4326();
                Assert.IsTrue(expected4326.EqualsTopologically(units4326[i].TrashGeneratingUnit4326Geometry), $"4326 row {i} is not the polygon of the unit it points at.");
            }
        }

        [TestMethod]
        public async Task TrashGeneratingUnitSwap_FailingTheCountCheck_LeavesTheLiveUnitsUnchanged()
        {
            var before = await TakeTrashGeneratingUnitSnapshot();
            Assert.IsTrue(before.Count > 0, "Local DB needs Trash Generating Units for this test to mean anything.");
            var stormwaterJurisdictionID = await AnyStormwaterJurisdictionID();
            await TrashGeneratingUnits.ReplaceStagingAsync(_dbContext,
                [NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 0), NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 1000)]);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => TrashGeneratingUnits.SwapFromStagingAsync(_dbContext, 3));

            StringAssert.Contains(ex.Message, "were staged");
            Assert.AreEqual(before, await TakeTrashGeneratingUnitSnapshot());
        }

        [TestMethod]
        public async Task TrashGeneratingUnitSwap_FailingInsideTheProcedure_LeavesTheLiveUnitsUnchanged()
        {
            // a delineation deleted while the overlay ran: the insert breaks the foreign key after
            // the live rows are already gone
            var before = await TakeTrashGeneratingUnitSnapshot();
            Assert.IsTrue(before.Count > 0, "Local DB needs Trash Generating Units for this test to mean anything.");
            var stormwaterJurisdictionID = await AnyStormwaterJurisdictionID();
            await TrashGeneratingUnits.ReplaceStagingAsync(_dbContext,
                [NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 0), NewTrashGeneratingUnitStaging(stormwaterJurisdictionID, 1000, delineationID: NonexistentID)]);

            await Assert.ThrowsAsync<SqlException>(() => TrashGeneratingUnits.SwapFromStagingAsync(_dbContext, 2));

            Assert.AreEqual(before, await TakeTrashGeneratingUnitSnapshot());
        }

        #endregion

        #region Load Generating Units

        private record LiveLoadGeneratingUnitSnapshot(int Count, long IDSum, int HRUCharacteristicCount, long HRUCharacteristicIDSum);

        private async Task<LiveLoadGeneratingUnitSnapshot> TakeLoadGeneratingUnitSnapshot()
        {
            return new LiveLoadGeneratingUnitSnapshot(
                await _dbContext.LoadGeneratingUnits.CountAsync(),
                await _dbContext.LoadGeneratingUnits.SumAsync(x => (long)x.LoadGeneratingUnitID),
                await _dbContext.HRUCharacteristics.CountAsync(),
                await _dbContext.HRUCharacteristics.SumAsync(x => (long)x.HRUCharacteristicID));
        }

        private static LoadGeneratingUnitStaging NewLoadGeneratingUnitStaging(double offset, int? delineationID = null)
        {
            var geometry = Square(offset);
            return new LoadGeneratingUnitStaging
            {
                DelineationID = delineationID,
                LoadGeneratingUnitGeometry = geometry,
                LoadGeneratingUnitGeometry4326 = geometry.ProjectTo4326(),
            };
        }

        [TestMethod]
        public async Task LoadGeneratingUnitSwap_ReplacesTheLiveUnitsWithStaging_AndEmptiesTheirHruCharacteristics()
        {
            await LoadGeneratingUnits.ReplaceStagingAsync(_dbContext, [NewLoadGeneratingUnitStaging(0), NewLoadGeneratingUnitStaging(1000)]);

            await LoadGeneratingUnits.SwapFromStagingAsync(_dbContext, 2);

            var units = await _dbContext.LoadGeneratingUnits.AsNoTracking().OrderBy(x => x.LoadGeneratingUnitID).ToListAsync();
            // IDs are not kept across a total refresh (decided on NPT-1156): they start again at 1
            CollectionAssert.AreEqual(new[] { 1, 2 }, units.Select(x => x.LoadGeneratingUnitID).ToList());
            Assert.IsTrue(units.All(x => x.LoadGeneratingUnitGeometry4326 != null));
            Assert.AreEqual(0, await _dbContext.HRUCharacteristics.CountAsync());
        }

        [TestMethod]
        public async Task LoadGeneratingUnitSwap_FailingTheCountCheck_LeavesTheLiveUnitsAndHruCharacteristicsUnchanged()
        {
            var before = await TakeLoadGeneratingUnitSnapshot();
            Assert.IsTrue(before.Count > 0, "Local DB needs Load Generating Units for this test to mean anything.");
            await LoadGeneratingUnits.ReplaceStagingAsync(_dbContext, [NewLoadGeneratingUnitStaging(0), NewLoadGeneratingUnitStaging(1000)]);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => LoadGeneratingUnits.SwapFromStagingAsync(_dbContext, 3));

            StringAssert.Contains(ex.Message, "were staged");
            Assert.AreEqual(before, await TakeLoadGeneratingUnitSnapshot());
        }

        [TestMethod]
        public async Task LoadGeneratingUnitSwap_FailingInsideTheProcedure_LeavesTheLiveUnitsAndHruCharacteristicsUnchanged()
        {
            var before = await TakeLoadGeneratingUnitSnapshot();
            Assert.IsTrue(before.Count > 0, "Local DB needs Load Generating Units for this test to mean anything.");
            await LoadGeneratingUnits.ReplaceStagingAsync(_dbContext, [NewLoadGeneratingUnitStaging(0), NewLoadGeneratingUnitStaging(1000, delineationID: NonexistentID)]);

            await Assert.ThrowsAsync<SqlException>(() => LoadGeneratingUnits.SwapFromStagingAsync(_dbContext, 2));

            Assert.AreEqual(before, await TakeLoadGeneratingUnitSnapshot());
        }

        #endregion
    }
}
