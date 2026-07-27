using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1115 — an invalid Delineation geometry (self-intersecting freehand draw) makes GeoServer's
    /// WMS GetMap throw SQL error 24144, blanking the Delineations layer. These integration tests
    /// (real local DB, rolled back) cover the two SQL-side guards: dbo.pDelineationMakeValid repairs
    /// invalid rows (whole-table and scoped by @DelineationID), and dbo.vGeoServerDelineation returns
    /// valid geometry even when the underlying row is invalid. Requires the updated view + sproc to be
    /// deployed to the local DB.
    /// </summary>
    [TestClass]
    public class DelineationMakeValidTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

        // Self-intersecting "bowtie" polygons — SQL Server STIsValid() = 0. State-plane (2771) and WGS84 (4326).
        private const string InvalidNativeWkt = "POLYGON((1840000 660000, 1840100 660100, 1840100 660000, 1840000 660100, 1840000 660000))";
        private const string Invalid4326Wkt = "POLYGON((-118 33.5, -117.9 33.6, -117.9 33.5, -118 33.6, -118 33.5))";

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

        private int ScalarInt(string sql) => _dbContext.Database.SqlQueryRaw<int>(sql).AsEnumerable().First();

        // Inserts an invalid-geometry Delineation for a BMP that has none (the FK is UNIQUE), returns its ID.
        // Returns null (→ Inconclusive) if the local DB has no delineation-free BMP or no DelineationType.
        private int? InsertInvalidDelineation()
        {
            var bmpId = _dbContext.Database.SqlQueryRaw<int>(
                "SELECT TOP 1 TreatmentBMPID AS Value FROM dbo.TreatmentBMP WHERE TreatmentBMPID NOT IN (SELECT TreatmentBMPID FROM dbo.Delineation) ORDER BY TreatmentBMPID")
                .AsEnumerable().Cast<int?>().FirstOrDefault();
            var typeId = _dbContext.Database.SqlQueryRaw<int>(
                "SELECT TOP 1 DelineationTypeID AS Value FROM dbo.DelineationType ORDER BY DelineationTypeID")
                .AsEnumerable().Cast<int?>().FirstOrDefault();
            if (bmpId == null || typeId == null) return null;

            _dbContext.Database.ExecuteSqlRaw($@"
                INSERT INTO dbo.Delineation (DelineationGeometry, DelineationGeometry4326, DelineationTypeID, IsVerified, TreatmentBMPID, DateLastModified, HasDiscrepancies)
                VALUES (geometry::STGeomFromText('{InvalidNativeWkt}', 2771), geometry::STGeomFromText('{Invalid4326Wkt}', 4326), {typeId}, 0, {bmpId}, GETUTCDATE(), 0)");

            return ScalarInt($"SELECT DelineationID AS Value FROM dbo.Delineation WHERE TreatmentBMPID = {bmpId}");
        }

        private int NativeValid(int delineationID) => ScalarInt($"SELECT CAST(DelineationGeometry.STIsValid() AS int) AS Value FROM dbo.Delineation WHERE DelineationID = {delineationID}");
        private int Valid4326(int delineationID) => ScalarInt($"SELECT CAST(DelineationGeometry4326.STIsValid() AS int) AS Value FROM dbo.Delineation WHERE DelineationID = {delineationID}");

        [TestMethod]
        public void PDelineationMakeValid_RepairsInvalidGeometry()
        {
            var id = InsertInvalidDelineation();
            if (id == null) { Assert.Inconclusive("No delineation-free TreatmentBMP available in the local DB."); return; }

            Assert.AreEqual(0, NativeValid(id.Value), "Native geometry should start invalid.");
            Assert.AreEqual(0, Valid4326(id.Value), "4326 geometry should start invalid.");

            _dbContext.Database.ExecuteSqlRaw("EXEC dbo.pDelineationMakeValid");

            Assert.AreEqual(1, NativeValid(id.Value), "Native geometry should be valid after MakeValid.");
            Assert.AreEqual(1, Valid4326(id.Value), "4326 geometry should be valid after MakeValid.");
        }

        [TestMethod]
        public void PDelineationMakeValid_Scoped_OnlyRepairsTargetDelineation()
        {
            var first = InsertInvalidDelineation();
            var second = InsertInvalidDelineation();
            if (first == null || second == null) { Assert.Inconclusive("Need two delineation-free TreatmentBMPs in the local DB."); return; }

            _dbContext.Database.ExecuteSqlRaw("EXEC dbo.pDelineationMakeValid @DelineationID = {0}", first.Value);

            Assert.AreEqual(1, NativeValid(first.Value), "Scoped repair should fix the targeted delineation.");
            Assert.AreEqual(1, Valid4326(first.Value), "Scoped repair should fix the targeted delineation (4326).");
            Assert.AreEqual(0, NativeValid(second.Value), "Scoped repair must not touch other delineations.");
        }

        [TestMethod]
        public void VGeoServerDelineation_ReturnsValidGeometry_EvenWhenSourceRowInvalid()
        {
            var id = InsertInvalidDelineation();
            if (id == null) { Assert.Inconclusive("No delineation-free TreatmentBMP available in the local DB."); return; }

            // Underlying row is still invalid (no sproc run) — the view's MakeValid guard must repair on read.
            Assert.AreEqual(0, Valid4326(id.Value), "Precondition: the stored 4326 geometry is invalid.");

            var viewValid = ScalarInt($"SELECT CAST(DelineationGeometry.STIsValid() AS int) AS Value FROM dbo.vGeoServerDelineation WHERE DelineationID = {id.Value}");
            Assert.AreEqual(1, viewValid, "vGeoServerDelineation must never expose invalid geometry to GeoServer.");
        }
    }
}
