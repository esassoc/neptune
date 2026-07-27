using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1092 — covers TreatmentBMPs.ListByWaterQualityManagementPlanIDAsFeatureCollectionAsync, the
    /// data source for the "Treatment BMPs" reference marker layer on the WQMP boundary editors. Reads
    /// the local dev DB (read-only). Asserts the FeatureCollection contains one point feature per linked
    /// inventoried BMP (ProjectID null, LocationPoint4326 present) with Name + Type properties, and that
    /// a WQMP with no linked BMPs yields an empty collection (no error state).
    /// </summary>
    [TestClass]
    public class TreatmentBMPsByWaterQualityManagementPlanFeatureCollectionTests
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

        [TestMethod]
        public async Task ReturnsOneFeaturePerLinkedInventoriedBMP_WithNameAndType()
        {
            // A WQMP that actually has linked inventoried BMPs with a location.
            var wqmpID = await _dbContext.TreatmentBMPs.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID != null && x.ProjectID == null && x.LocationPoint4326 != null)
                .GroupBy(x => x.WaterQualityManagementPlanID!.Value)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefaultAsync();
            if (wqmpID == null) { Assert.Inconclusive("No WQMP with linked inventoried BMPs in the local DB."); return; }

            var expectedCount = await _dbContext.TreatmentBMPs.AsNoTracking()
                .CountAsync(x => x.WaterQualityManagementPlanID == wqmpID && x.ProjectID == null && x.LocationPoint4326 != null);

            var featureCollection = await TreatmentBMPs.ListByWaterQualityManagementPlanIDAsFeatureCollectionAsync(_dbContext, wqmpID.Value);

            Assert.AreEqual(expectedCount, featureCollection.Count, "One feature per linked inventoried BMP with a location.");
            Assert.IsTrue(featureCollection.All(f => f.Geometry != null && f.Geometry.OgcGeometryType == NetTopologySuite.Geometries.OgcGeometryType.Point),
                "Every feature must be a point.");
            Assert.IsTrue(featureCollection.All(f =>
                    f.Attributes.Exists("TreatmentBMPName") && f.Attributes.Exists("TreatmentBMPTypeName")),
                "Every feature must carry BMP Name and Type for the popup.");
        }

        [TestMethod]
        public async Task ReturnsEmptyCollection_WhenNoLinkedBMPs()
        {
            // A WQMP that exists but has no linked inventoried BMPs (or none at all).
            var wqmpIDWithNone = await _dbContext.WaterQualityManagementPlans.AsNoTracking()
                .Where(w => !_dbContext.TreatmentBMPs.Any(x => x.WaterQualityManagementPlanID == w.WaterQualityManagementPlanID))
                .Select(w => (int?)w.WaterQualityManagementPlanID)
                .FirstOrDefaultAsync();
            if (wqmpIDWithNone == null) { Assert.Inconclusive("Every WQMP in the local DB has linked BMPs."); return; }

            var featureCollection = await TreatmentBMPs.ListByWaterQualityManagementPlanIDAsFeatureCollectionAsync(_dbContext, wqmpIDWithNone.Value);

            Assert.AreEqual(0, featureCollection.Count, "A WQMP with no linked BMPs yields an empty collection (no markers, no error).");
        }
    }
}
