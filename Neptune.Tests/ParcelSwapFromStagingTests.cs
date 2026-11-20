using System;
using System.Linq;
using System.Threading.Tasks;
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
    /// NPT-1140 — Parcels.SwapFromStagingAsync replaces Parcel, ParcelGeometry and
    /// WaterQualityManagementPlanParcel from ParcelStaging in one transaction. A failure after the
    /// procedure has already deleted and re-inserted the live rows must leave them exactly as they
    /// were. Runs against the local NeptuneDB inside a transaction that is rolled back.
    /// </summary>
    [TestClass]
    public class ParcelSwapFromStagingTests
    {
        private NeptuneDbContext _dbContext = GetDbContext();
        private IDbContextTransaction? _transaction;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                TestDatabase.ConnectionString, x =>
                {
                    // the swap deletes and rebuilds every live parcel row
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
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private record LiveParcelSnapshot(int ParcelCount, long ParcelIDSum, DateTime? MaxLastUpdate, int GeometryCount, long ParcelGeometryIDSum, int WqmpParcelCount, long WqmpParcelIDSum);

        private async Task<LiveParcelSnapshot> TakeSnapshot()
        {
            return new LiveParcelSnapshot(
                await _dbContext.Parcels.CountAsync(),
                await _dbContext.Parcels.SumAsync(x => (long)x.ParcelID),
                await _dbContext.Parcels.MaxAsync(x => (DateTime?)x.LastUpdate),
                await _dbContext.ParcelGeometries.CountAsync(),
                await _dbContext.ParcelGeometries.SumAsync(x => (long)x.ParcelGeometryID),
                await _dbContext.WaterQualityManagementPlanParcels.CountAsync(),
                await _dbContext.WaterQualityManagementPlanParcels.SumAsync(x => (long)x.WaterQualityManagementPlanParcelID));
        }

        private static ParcelStaging NewStaging(string parcelNumber, double offset, bool with4326)
        {
            // a 100 m square in Orange County (EPSG:2771, meters)
            var x = 1850000 + offset;
            var geometry = new WKTReader().Read($"POLYGON (({x} 660000, {x + 100} 660000, {x + 100} 660100, {x} 660100, {x} 660000))");
            geometry.SRID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;
            return new ParcelStaging
            {
                ParcelNumber = parcelNumber,
                Geometry = geometry,
                Geometry4326 = with4326 ? geometry.ProjectTo4326() : null,
                ParcelAreaInSquareFeet = 10000,
                ParcelAddress = "1 TEST ST",
                ParcelCityState = "TEST",
                ParcelZipCode = "00000",
            };
        }

        private async Task StageAsync(params ParcelStaging[] parcelStagings)
        {
            await _dbContext.ParcelStagings.ExecuteDeleteAsync();
            _dbContext.ParcelStagings.AddRange(parcelStagings);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        [TestMethod]
        public async Task SwapFailingVerification_OnMissingGeometry4326_LeavesLiveParcelsUnchanged()
        {
            var before = await TakeSnapshot();
            Assert.IsTrue(before.ParcelCount > 0, "Local DB needs parcels for this test to mean anything.");
            await StageAsync(NewStaging("TEST-0001", 0, with4326: true), NewStaging("TEST-0002", 1000, with4326: false));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Parcels.SwapFromStagingAsync(_dbContext, 2));

            StringAssert.Contains(ex.Message, "Geometry4326");
            Assert.AreEqual(before, await TakeSnapshot());
        }

        [TestMethod]
        public async Task SwapFailingVerification_OnCountMismatch_LeavesLiveParcelsUnchanged()
        {
            var before = await TakeSnapshot();
            await StageAsync(NewStaging("TEST-0001", 0, with4326: true), NewStaging("TEST-0002", 1000, with4326: true));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Parcels.SwapFromStagingAsync(_dbContext, 3));

            StringAssert.Contains(ex.Message, "were staged");
            Assert.AreEqual(before, await TakeSnapshot());
        }

        [TestMethod]
        public async Task SwapSucceeding_ReplacesLiveParcelsWithStaging()
        {
            await StageAsync(NewStaging("TEST-0001", 0, with4326: true), NewStaging("TEST-0002", 1000, with4326: true));

            await Parcels.SwapFromStagingAsync(_dbContext, 2);

            var parcelNumbers = await _dbContext.Parcels.AsNoTracking().Select(x => x.ParcelNumber).OrderBy(x => x).ToListAsync();
            CollectionAssert.AreEqual(new[] { "TEST-0001", "TEST-0002" }, parcelNumbers);
            Assert.AreEqual(0, await ParcelGeometries.CountMissing4326Async(_dbContext));
        }
    }
}
