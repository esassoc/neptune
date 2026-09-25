using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.NearbyAsset;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1123 — nearby-assets proximity lookup behind the homepage Field Actions panel.
    /// DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive when the data isn't present.
    /// </summary>
    [TestClass]
    public class NearbyAssetsTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer("Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static Person GetAdmin(NeptuneDbContext db)
        {
            return db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin && p.IsActive);
        }

        // --- Coordinate validation (DB-independent) ---

        [TestMethod]
        public void AreValidCoordinates_AcceptsInRangeAndBoundaryValues()
        {
            Assert.IsTrue(NearbyAssets.AreValidCoordinates(33.4964780, -117.6705060));
            Assert.IsTrue(NearbyAssets.AreValidCoordinates(90, 180));
            Assert.IsTrue(NearbyAssets.AreValidCoordinates(-90, -180));
        }

        [TestMethod]
        public void AreValidCoordinates_RejectsOutOfRange()
        {
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(90.0001, 0));
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(0, -180.0001));
        }

        [TestMethod]
        public void AreValidCoordinates_RejectsNonFinite()
        {
            // NaN slips past plain range comparisons (every comparison with NaN is false)
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(double.NaN, 0));
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(0, double.NaN));
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(double.PositiveInfinity, 0));
            Assert.IsFalse(NearbyAssets.AreValidCoordinates(0, double.NegativeInfinity));
        }

        [TestMethod]
        public async Task TreatmentBMP_AtItsOwnLocation_IsReturnedAtZeroDistance()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) { Assert.Inconclusive("No active Admin in the local DB."); return; }

            var bmp = db.TreatmentBMPs.AsNoTracking().FirstOrDefault(x => x.LocationPoint4326 != null && x.LocationPoint != null);
            if (bmp == null) { Assert.Inconclusive("No located BMP in the local DB."); return; }

            var result = await NearbyAssets.ListWithinRadiusAsync(db, admin, bmp.LocationPoint4326.Coordinate.Y, bmp.LocationPoint4326.Coordinate.X);

            var hit = result.Assets.SingleOrDefault(x => x.AssetType == NearbyAssetDto.TreatmentBMPAssetType && x.AssetID == bmp.TreatmentBMPID);
            Assert.IsNotNull(hit, "A BMP must be found when searching at its own location.");
            // ProjNet treats WGS84 and NAD83(HARN) as the same datum, so reprojecting the stored 4326 point lands
            // ~1-2 m from the stored native point (the OC datum offset). Irrelevant at a 100 m radius.
            Assert.IsTrue(hit.DistanceMeters < 5, $"Expected within the ~2 m datum offset, got {hit.DistanceMeters}.");
            Assert.AreEqual(NearbyAssets.RadiusMeters, result.RadiusMeters);
        }

        [TestMethod]
        public async Task Results_AreSortedNearestFirst_AndWithinRadius()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) { Assert.Inconclusive("No active Admin in the local DB."); return; }

            var bmp = db.TreatmentBMPs.AsNoTracking().FirstOrDefault(x => x.LocationPoint4326 != null && x.LocationPoint != null);
            if (bmp == null) { Assert.Inconclusive("No located BMP in the local DB."); return; }

            // offset ~30 m north so distances aren't all zero
            var result = await NearbyAssets.ListWithinRadiusAsync(db, admin, bmp.LocationPoint4326.Coordinate.Y + 0.00027, bmp.LocationPoint4326.Coordinate.X);

            Assert.IsTrue(result.Assets.Any(), "Expected at least the seed BMP within 100 m.");
            Assert.IsTrue(result.Assets.All(x => x.DistanceMeters <= NearbyAssets.RadiusMeters + 0.001), "Every result must fall within the radius.");
            for (var i = 1; i < result.Assets.Count; i++)
            {
                Assert.IsTrue(result.Assets[i - 1].DistanceMeters <= result.Assets[i].DistanceMeters, "Results must be sorted nearest first.");
            }
        }

        [TestMethod]
        public async Task WaterQualityManagementPlan_InsideItsBoundary_IsReturnedAtZeroDistance()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) { Assert.Inconclusive("No active Admin in the local DB."); return; }

            var viewable = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(db, admin).ToList();
            var boundary = db.WaterQualityManagementPlanBoundaries.AsNoTracking()
                .FirstOrDefault(x => x.Geometry4326 != null && x.GeometryNative != null && viewable.Contains(x.WaterQualityManagementPlan.StormwaterJurisdictionID));
            if (boundary == null) { Assert.Inconclusive("No WQMP boundary in an Admin-viewable jurisdiction."); return; }

            var inside = boundary.Geometry4326.InteriorPoint.Coordinate;
            var result = await NearbyAssets.ListWithinRadiusAsync(db, admin, inside.Y, inside.X);

            var hit = result.Assets.SingleOrDefault(x => x.AssetType == NearbyAssetDto.WaterQualityManagementPlanAssetType && x.AssetID == boundary.WaterQualityManagementPlanID);
            Assert.IsNotNull(hit, "A WQMP must be found when standing inside its boundary.");
            Assert.IsTrue(hit.DistanceMeters < 1, $"Expected ~0 m inside the boundary, got {hit.DistanceMeters}.");
        }

        [TestMethod]
        public async Task OnlandVisualTrashAssessmentArea_InsideItsGeometry_IsReturnedAtZeroDistance()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) { Assert.Inconclusive("No active Admin in the local DB."); return; }

            var area = db.OnlandVisualTrashAssessmentAreas.AsNoTracking().FirstOrDefault(x => x.OnlandVisualTrashAssessmentAreaGeometry4326 != null);
            if (area == null) { Assert.Inconclusive("No OVTA area with a 4326 geometry in the local DB."); return; }

            var inside = area.OnlandVisualTrashAssessmentAreaGeometry4326.InteriorPoint.Coordinate;
            var result = await NearbyAssets.ListWithinRadiusAsync(db, admin, inside.Y, inside.X);

            var hit = result.Assets.SingleOrDefault(x => x.AssetType == NearbyAssetDto.OnlandVisualTrashAssessmentAreaAssetType && x.AssetID == area.OnlandVisualTrashAssessmentAreaID);
            Assert.IsNotNull(hit, "An OVTA area must be found when standing inside it.");
            Assert.IsTrue(hit.DistanceMeters < 1, $"Expected ~0 m inside the area, got {hit.DistanceMeters}.");
        }

        [TestMethod]
        public async Task JurisdictionManager_OnlySeesAssetsInViewableJurisdictions()
        {
            using var db = GetDbContext();
            var jm = db.People.AsNoTracking()
                .FirstOrDefault(p => p.RoleID == (int)RoleEnum.JurisdictionManager && p.StormwaterJurisdictionPeople.Any());
            if (jm == null) { Assert.Inconclusive("No JurisdictionManager with an assigned jurisdiction in the local DB."); return; }

            var assigned = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, jm.PersonID);
            // search at a BMP OUTSIDE the JM's jurisdictions — it must not come back
            var outsideBmp = db.TreatmentBMPs.AsNoTracking()
                .FirstOrDefault(x => x.LocationPoint4326 != null && x.LocationPoint != null && !assigned.Contains(x.StormwaterJurisdictionID));
            if (outsideBmp == null) { Assert.Inconclusive("No located BMP outside the JM's jurisdictions."); return; }

            var result = await NearbyAssets.ListWithinRadiusAsync(db, jm, outsideBmp.LocationPoint4326.Coordinate.Y, outsideBmp.LocationPoint4326.Coordinate.X);

            Assert.IsFalse(result.Assets.Any(x => x.AssetType == NearbyAssetDto.TreatmentBMPAssetType && x.AssetID == outsideBmp.TreatmentBMPID),
                "A BMP outside the caller's jurisdictions must not be returned.");
            Assert.IsTrue(result.Assets.All(x => assigned.Contains(x.StormwaterJurisdictionID)),
                "Every result must be in a jurisdiction the caller can view.");
        }

        [TestMethod]
        public async Task PointOffshore_ReturnsNoAssets()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) { Assert.Inconclusive("No active Admin in the local DB."); return; }

            // open ocean off the Orange County coast
            var result = await NearbyAssets.ListWithinRadiusAsync(db, admin, 33.3, -118.5);

            Assert.AreEqual(0, result.Assets.Count);
        }
    }
}
