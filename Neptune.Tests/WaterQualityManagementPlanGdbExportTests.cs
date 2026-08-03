using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-943 — covers the data assembly behind the WQMP GDB export
    /// (WaterQualityManagementPlanGdbExport). The GDAL zip step is integration-only, so these tests
    /// target the query (boundary / jurisdiction / ID filters) and the FeatureCollection projection.
    /// Reads the local dev DB (read-only).
    /// </summary>
    [TestClass]
    public class WaterQualityManagementPlanGdbExportTests
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

        // A jurisdiction that has WQMPs with a recorded boundary.
        private int? JurisdictionWithBoundaries() =>
            _dbContext.WaterQualityManagementPlans.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanBoundary != null && x.WaterQualityManagementPlanBoundary.GeometryNative != null)
                .GroupBy(x => x.StormwaterJurisdictionID)
                .OrderByDescending(g => g.Count())
                .Select(g => (int?)g.Key)
                .FirstOrDefault();

        [TestMethod]
        public void ListForGdbExport_ReturnsOnlyBoundaryWqmpsInScope()
        {
            var jurisdictionID = JurisdictionWithBoundaries();
            if (jurisdictionID == null) { Assert.Inconclusive("No WQMP with a boundary in the local DB."); return; }

            var expected = _dbContext.WaterQualityManagementPlans.AsNoTracking()
                .Count(x => x.StormwaterJurisdictionID == jurisdictionID
                            && x.WaterQualityManagementPlanBoundary != null
                            && x.WaterQualityManagementPlanBoundary.GeometryNative != null);

            var result = WaterQualityManagementPlanGdbExport.ListForGdbExport(
                _dbContext, new[] { jurisdictionID.Value }, Array.Empty<int>());

            Assert.AreEqual(expected, result.Count, "Should return every boundary-having WQMP in the jurisdiction (empty ID filter).");
            Assert.IsTrue(result.All(x => x.StormwaterJurisdictionID == jurisdictionID), "No cross-jurisdiction WQMPs.");
            Assert.IsTrue(result.All(x => x.WaterQualityManagementPlanBoundary?.GeometryNative != null), "Every WQMP must have a boundary (req 6).");
        }

        [TestMethod]
        public void ListForGdbExport_HonorsIdFilter()
        {
            var jurisdictionID = JurisdictionWithBoundaries();
            if (jurisdictionID == null) { Assert.Inconclusive("No WQMP with a boundary in the local DB."); return; }

            var all = WaterQualityManagementPlanGdbExport.ListForGdbExport(_dbContext, new[] { jurisdictionID.Value }, Array.Empty<int>());
            if (all.Count < 2) { Assert.Inconclusive("Need at least two boundary WQMPs to test the ID filter."); return; }

            var subset = all.Take(2).Select(x => x.WaterQualityManagementPlanID).ToList();
            var filtered = WaterQualityManagementPlanGdbExport.ListForGdbExport(_dbContext, new[] { jurisdictionID.Value }, subset);

            CollectionAssert.AreEquivalent(subset, filtered.Select(x => x.WaterQualityManagementPlanID).ToList(),
                "Only the requested IDs should be returned.");
        }

        [TestMethod]
        public void ToFeatureCollection_ProducesOnePolygonFeaturePerWqmp_WithExpectedAttributes()
        {
            var jurisdictionID = JurisdictionWithBoundaries();
            if (jurisdictionID == null) { Assert.Inconclusive("No WQMP with a boundary in the local DB."); return; }

            var wqmps = WaterQualityManagementPlanGdbExport.ListForGdbExport(_dbContext, new[] { jurisdictionID.Value }, Array.Empty<int>())
                .Take(25).ToList();

            var fc = WaterQualityManagementPlanGdbExport.ToFeatureCollection(wqmps);

            Assert.AreEqual(wqmps.Count, fc.Count, "One feature per WQMP.");
            Assert.IsTrue(fc.All(f => f.Geometry != null), "Every feature has boundary geometry.");
            foreach (var key in new[] { "Name", "Jurisdiction", "Priority", "Trash_Capture_Status", "Maintenance_Contact_Name", "Recorded_WQMP_Area_Acres", "Calculated_Boundary_Acreage" })
            {
                Assert.IsTrue(fc.All(f => f.Attributes.Exists(key)), $"Every feature must carry the '{key}' attribute.");
            }
        }

        // A real exportable WQMP (valid lookup FKs so BuildAttributes' lookup getters resolve); we then
        // mutate just the date / geometry in-memory to exercise the export projection deterministically.
        private WaterQualityManagementPlan FirstExportableWqmp()
        {
            var jurisdictionID = JurisdictionWithBoundaries();
            if (jurisdictionID == null) return null;
            return WaterQualityManagementPlanGdbExport
                .ListForGdbExport(_dbContext, new[] { jurisdictionID.Value }, Array.Empty<int>())
                .FirstOrDefault();
        }

        [TestMethod]
        public void ToFeatureCollection_EmitsDateOnlyStringAndCalculatedAcreage()
        {
            var wqmp = FirstExportableWqmp();
            if (wqmp == null) { Assert.Inconclusive("No exportable WQMP in the local DB."); return; }

            // Seed a value with a time component to prove it is dropped (not shifted to a UTC timestamp).
            wqmp.ApprovalDate = new DateTime(2004, 11, 10, 8, 30, 0);
            var attrs = WaterQualityManagementPlanGdbExport.ToFeatureCollection(new[] { wqmp }).Single().Attributes;

            // Date-only, timezone-naive (NPT-943 item 3).
            Assert.AreEqual("2004-11-10", attrs["Approval_Date"], "Approval_Date must be a date-only string with no time/offset.");
            // Calculated acreage matches the exported polygon area (NPT-943 item 2), same formula as DtoProjections.
            var expectedAcres = Math.Round(wqmp.WaterQualityManagementPlanBoundary.GeometryNative.Area * Constants.SquareMetersToAcres, 1);
            Assert.AreEqual(expectedAcres, attrs["Calculated_Boundary_Acreage"], "Calculated_Boundary_Acreage must equal the polygon area in acres.");
        }

        [TestMethod]
        public void ToFeatureCollection_RepairsInvalidBoundaryGeometry()
        {
            var wqmp = FirstExportableWqmp();
            if (wqmp == null) { Assert.Inconclusive("No exportable WQMP in the local DB."); return; }

            // Replace the boundary with a self-intersecting "bowtie" ring — invalid per OGC, like the
            // parcel-union pinch points KE flagged — keeping the WQMP's real lookup FKs intact.
            var srid = wqmp.WaterQualityManagementPlanBoundary.GeometryNative.SRID;
            var bowtie = new GeometryFactory(new PrecisionModel(), srid).CreatePolygon(new[]
            {
                new Coordinate(0, 0), new Coordinate(2, 2), new Coordinate(0, 2), new Coordinate(2, 0), new Coordinate(0, 0),
            });
            Assert.IsFalse(bowtie.IsValid, "Precondition: the constructed bowtie should be invalid.");
            wqmp.WaterQualityManagementPlanBoundary.GeometryNative = bowtie;

            var feature = WaterQualityManagementPlanGdbExport.ToFeatureCollection(new[] { wqmp }).Single();
            Assert.IsTrue(feature.Geometry.IsValid, "Export must repair invalid boundary geometry (NPT-943 item 5a).");
        }
    }
}
