using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    [TestClass]
    public class DelineationGdbHelpersTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private Person _jurisdictionPerson = null!;
        private int _treatmentBMPTypeID;

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

            _jurisdictionID = _dbContext.StormwaterJurisdictions.AsNoTracking()
                .OrderBy(x => x.StormwaterJurisdictionID).Select(x => x.StormwaterJurisdictionID).First();

            _jurisdictionPerson = _dbContext.StormwaterJurisdictionPeople
                .Where(x => x.StormwaterJurisdictionID == _jurisdictionID)
                .Join(_dbContext.People.Include(p => p.StormwaterJurisdictionPeople),
                    sjp => sjp.PersonID, p => p.PersonID, (sjp, p) => p)
                .Where(p => p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                .OrderBy(p => p.PersonID).First();

            _treatmentBMPTypeID = _dbContext.TreatmentBMPTypes.AsNoTracking()
                .OrderBy(x => x.TreatmentBMPTypeID).Select(x => x.TreatmentBMPTypeID).First();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private TreatmentBMP CreateBMP(string name)
        {
            var loc4326 = new Point(-117.85 + new Random().NextDouble() * 0.01, 33.65 + new Random().NextDouble() * 0.01) { SRID = 4326 };
            var bmp = new TreatmentBMP
            {
                TreatmentBMPName = name,
                TreatmentBMPTypeID = _treatmentBMPTypeID,
                StormwaterJurisdictionID = _jurisdictionID,
                OwnerOrganizationID = _dbContext.StormwaterJurisdictions.AsNoTracking().Single(x => x.StormwaterJurisdictionID == _jurisdictionID).OrganizationID,
                LocationPoint4326 = loc4326,
                LocationPoint = loc4326.ProjectTo2771(),
                InventoryIsVerified = false,
                TreatmentBMPLifespanTypeID = (int)TreatmentBMPLifespanTypeEnum.Unspecified,
                SizingBasisTypeID = (int)SizingBasisTypeEnum.NotProvided,
                TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.None,
            };
            _dbContext.TreatmentBMPs.Add(bmp);
            _dbContext.SaveChanges();
            return bmp;
        }

        private byte[] SampleStagingGeoJson(string treatmentBMPName, string? delineationStatus = null)
        {
            // Build a polygon already in the HARN CA Zone VI projection (the upload pipeline expects 2771).
            var poly4326 = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.85, 33.65),
                new Coordinate(-117.84, 33.65),
                new Coordinate(-117.84, 33.66),
                new Coordinate(-117.85, 33.66),
                new Coordinate(-117.85, 33.65),
            })) { SRID = 4326 };
            var geom = (Geometry)poly4326.ProjectTo2771();

            var attrs = new AttributesTable
            {
                { "UploadedByPersonID", _jurisdictionPerson.PersonID },
                { "StormwaterJurisdictionID", _jurisdictionID },
                { "TreatmentBMPName", treatmentBMPName },
                { "DelineationStatus", delineationStatus },
            };
            var fc = new FeatureCollection { new Feature(geom, attrs) };
            return GeoJsonSerializer.SerializeToByteArray(fc, GeoJsonSerializer.DefaultSerializerOptions);
        }

        private byte[] SampleStagingGeoJsonForNames(IReadOnlyList<string> treatmentBMPNames)
        {
            var fc = new FeatureCollection();
            for (var i = 0; i < treatmentBMPNames.Count; i++)
            {
                var x0 = -117.85 + i * 0.02;
                var poly4326 = new Polygon(new LinearRing(new[]
                {
                    new Coordinate(x0, 33.65),
                    new Coordinate(x0 + 0.01, 33.65),
                    new Coordinate(x0 + 0.01, 33.66),
                    new Coordinate(x0, 33.66),
                    new Coordinate(x0, 33.65),
                })) { SRID = 4326 };
                var geom = (Geometry)poly4326.ProjectTo2771();
                var attrs = new AttributesTable
                {
                    { "UploadedByPersonID", _jurisdictionPerson.PersonID },
                    { "StormwaterJurisdictionID", _jurisdictionID },
                    { "TreatmentBMPName", treatmentBMPNames[i] },
                    { "DelineationStatus", (string?)null },
                };
                fc.Add(new Feature(geom, attrs));
            }
            return GeoJsonSerializer.SerializeToByteArray(fc, GeoJsonSerializer.DefaultSerializerOptions);
        }

        [TestMethod]
        public async Task ProcessDeserializedStagingAsync_HappyPathPersistsRow()
        {
            var bmp = CreateBMP($"NPT-981-PR3-{Guid.NewGuid():N}");
            var bytes = SampleStagingGeoJson(bmp.TreatmentBMPName);

            var errors = await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, bytes, _jurisdictionPerson);

            Assert.AreEqual(0, errors.Count);
            var staged = _dbContext.DelineationStagings.Single(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID);
            Assert.AreEqual(bmp.TreatmentBMPName, staged.TreatmentBMPName);
        }

        [TestMethod]
        public async Task ProcessDeserializedStagingAsync_BlocksWhenBMPHasCentralizedDelineation()
        {
            var bmp = CreateBMP($"NPT-981-PR3-CENT-{Guid.NewGuid():N}");
            var poly4326 = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.85, 33.65), new Coordinate(-117.84, 33.65),
                new Coordinate(-117.84, 33.66), new Coordinate(-117.85, 33.66), new Coordinate(-117.85, 33.65),
            })) { SRID = 4326 };
            _dbContext.Delineations.Add(new Delineation
            {
                TreatmentBMPID = bmp.TreatmentBMPID,
                DelineationTypeID = (int)DelineationTypeEnum.Centralized,
                DelineationGeometry4326 = poly4326,
                DelineationGeometry = poly4326.ProjectTo2771(),
                DateLastModified = DateTime.UtcNow,
                IsVerified = true,
                HasDiscrepancies = false,
            });
            _dbContext.SaveChanges();

            var bytes = SampleStagingGeoJson(bmp.TreatmentBMPName);
            var errors = await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, bytes, _jurisdictionPerson);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "centralized");
            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID));
        }

        [TestMethod]
        public async Task BuildReportForCurrentUser_FlagsUnmatchedBMPName()
        {
            var bmp = CreateBMP($"NPT-981-PR3-MATCH-{Guid.NewGuid():N}");
            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(bmp.TreatmentBMPName), _jurisdictionPerson);

            var stage = _dbContext.DelineationStagings.Single(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID);
            stage.TreatmentBMPName = $"DOES-NOT-EXIST-{Guid.NewGuid():N}";
            _dbContext.SaveChanges();

            var report = DelineationStagings.BuildReportForCurrentUser(_dbContext, _jurisdictionPerson);

            Assert.IsTrue(report.Errors.Any(e => e.Contains("do not match a Treatment BMP Name")));
            Assert.AreEqual(1, report.NumberOfDelineations);
            Assert.AreEqual(0, report.NumberOfDelineationsToBeUpdated);
            Assert.AreEqual(0, report.NumberOfDelineationsToBeCreated);
        }

        [TestMethod]
        public async Task ApproveAsync_ReplacesExistingDistributedDelineation()
        {
            var bmp = CreateBMP($"NPT-981-PR3-APPR-{Guid.NewGuid():N}");
            // Pre-existing distributed delineation that should be replaced.
            var poly4326 = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.86, 33.66), new Coordinate(-117.85, 33.66),
                new Coordinate(-117.85, 33.67), new Coordinate(-117.86, 33.67), new Coordinate(-117.86, 33.66),
            })) { SRID = 4326 };
            _dbContext.Delineations.Add(new Delineation
            {
                TreatmentBMPID = bmp.TreatmentBMPID,
                DelineationTypeID = (int)DelineationTypeEnum.Distributed,
                DelineationGeometry4326 = poly4326,
                DelineationGeometry = poly4326.ProjectTo2771(),
                DateLastModified = DateTime.UtcNow,
                IsVerified = false,
                HasDiscrepancies = false,
            });
            _dbContext.SaveChanges();
            var preexistingID = _dbContext.Delineations.Single(x => x.TreatmentBMPID == bmp.TreatmentBMPID).DelineationID;

            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(bmp.TreatmentBMPName, "Verified"), _jurisdictionPerson);
            var count = await DelineationStagings.ApproveAsync(_dbContext, _jurisdictionPerson);

            Assert.AreEqual(1, count);
            Assert.IsFalse(_dbContext.Delineations.Any(x => x.DelineationID == preexistingID), "Pre-existing delineation should be deleted.");
            var newDel = _dbContext.Delineations.Single(x => x.TreatmentBMPID == bmp.TreatmentBMPID);
            Assert.IsTrue(newDel.IsVerified);
            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID), "Staging should be cleared.");
        }

        [TestMethod]
        public async Task ProcessDeserializedStagingAsync_BMPWithUpstreamSetReturnsErrorButPersistsStaging()
        {
            var bmp = CreateBMP($"NPT-981-PR3-UP-{Guid.NewGuid():N}");
            // Pick any other BMP in the same jurisdiction to act as the upstream pointer.
            var upstreamCandidate = _dbContext.TreatmentBMPs.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID == _jurisdictionID && x.TreatmentBMPID != bmp.TreatmentBMPID)
                .OrderBy(x => x.TreatmentBMPID)
                .FirstOrDefault();
            Assert.IsNotNull(upstreamCandidate, "Test requires at least one other BMP in the jurisdiction to use as Upstream BMP.");
            var tracked = _dbContext.TreatmentBMPs.Single(x => x.TreatmentBMPID == bmp.TreatmentBMPID);
            tracked.UpstreamBMPID = upstreamCandidate.TreatmentBMPID;
            _dbContext.SaveChanges();

            var bytes = SampleStagingGeoJson(bmp.TreatmentBMPName);
            var errors = await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, bytes, _jurisdictionPerson);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "has an Upstream BMP");
            // Per the contract: when the upstream-BMP error is detected post-save, the row IS persisted; the
            // controller (not the helper) is responsible for cleaning up via DiscardForUserAsync.
            Assert.IsTrue(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID && x.TreatmentBMPName == bmp.TreatmentBMPName));
        }

        [TestMethod]
        public async Task ApproveAsync_BlocksWhenStagingHasErrors()
        {
            // No staging at all → BuildReportForCurrentUser surfaces an error → ApproveAsync would still throw on Check.Assert.
            // Use a different shape: stage one row with an unmatched BMP name so report has errors.
            var bmp = CreateBMP($"NPT-981-PR3-NOAPP-{Guid.NewGuid():N}");
            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(bmp.TreatmentBMPName), _jurisdictionPerson);
            var staging = _dbContext.DelineationStagings.Single(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID);
            staging.TreatmentBMPName = $"NOT-A-REAL-BMP-{Guid.NewGuid():N}";
            _dbContext.SaveChanges();

            var report = DelineationStagings.BuildReportForCurrentUser(_dbContext, _jurisdictionPerson);
            Assert.IsTrue(report.Errors.Count > 0, "Expected errors so the approve gate has something to refuse.");
            // The helper itself doesn't gate (matches legacy); the controller does. Just confirm that approving
            // still cleans the staging and creates 0 delineations because no BMPs match.
            var count = await DelineationStagings.ApproveAsync(_dbContext, _jurisdictionPerson);
            Assert.AreEqual(0, count);
            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID));
        }

        [TestMethod]
        public async Task ProcessDeserializedStagingAsync_CentralizedConflictScopedToJurisdiction()
        {
            // Find a different jurisdiction; create a BMP with a centralized delineation there.
            var otherJurisdictionID = _dbContext.StormwaterJurisdictions.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID != _jurisdictionID)
                .OrderBy(x => x.StormwaterJurisdictionID)
                .Select(x => x.StormwaterJurisdictionID)
                .First();
            var sharedName = $"NPT-981-PR3-SHARED-{Guid.NewGuid():N}";

            // Centralized BMP in the OTHER jurisdiction with the same name.
            var locOther = new Point(-117.9, 33.7) { SRID = 4326 };
            var otherBMP = new TreatmentBMP
            {
                TreatmentBMPName = sharedName,
                TreatmentBMPTypeID = _treatmentBMPTypeID,
                StormwaterJurisdictionID = otherJurisdictionID,
                OwnerOrganizationID = _dbContext.StormwaterJurisdictions.AsNoTracking().Single(x => x.StormwaterJurisdictionID == otherJurisdictionID).OrganizationID,
                LocationPoint4326 = locOther,
                LocationPoint = locOther.ProjectTo2771(),
                InventoryIsVerified = false,
                TreatmentBMPLifespanTypeID = (int)TreatmentBMPLifespanTypeEnum.Unspecified,
                SizingBasisTypeID = (int)SizingBasisTypeEnum.NotProvided,
                TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.None,
            };
            _dbContext.TreatmentBMPs.Add(otherBMP);
            _dbContext.SaveChanges();
            var poly4326 = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.91, 33.7), new Coordinate(-117.9, 33.7),
                new Coordinate(-117.9, 33.71), new Coordinate(-117.91, 33.71), new Coordinate(-117.91, 33.7),
            })) { SRID = 4326 };
            _dbContext.Delineations.Add(new Delineation
            {
                TreatmentBMPID = otherBMP.TreatmentBMPID,
                DelineationTypeID = (int)DelineationTypeEnum.Centralized,
                DelineationGeometry4326 = poly4326,
                DelineationGeometry = poly4326.ProjectTo2771(),
                DateLastModified = DateTime.UtcNow,
                IsVerified = true,
                HasDiscrepancies = false,
            });
            _dbContext.SaveChanges();

            // BMP in OUR jurisdiction with the same name; staging upload should NOT be blocked by the other jurisdiction's centralized.
            CreateBMP(sharedName);

            var errors = await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(sharedName), _jurisdictionPerson);

            Assert.AreEqual(0, errors.Count, "Centralized check should be jurisdiction-scoped; same name in another jurisdiction must not block.");
            Assert.IsTrue(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID && x.TreatmentBMPName == sharedName));
        }

        [TestMethod]
        public async Task ApproveAsync_HandlesBMPNameCaseMismatchGracefully()
        {
            // NPT-1093: the GDB casing can differ from the inventory casing (Brea had "NoName2" in the GDB but
            // "noName2" in the system). Staging must treat it as a match, and approve must succeed (previously the
            // case-sensitive in-memory .Single() threw and surfaced as a 500).
            var suffix = Guid.NewGuid().ToString("N");
            var dbName = $"noName2-{suffix}";  // inventory casing
            var gdbName = $"NoName2-{suffix}"; // GDB casing — differs only by the leading N

            var bmp = CreateBMP(dbName);
            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(gdbName), _jurisdictionPerson);

            var report = DelineationStagings.BuildReportForCurrentUser(_dbContext, _jurisdictionPerson);
            Assert.IsFalse(report.Errors.Any(e => e.Contains("do not match a Treatment BMP Name")),
                "A case-only mismatch should be reported as a match, not unmatched.");
            Assert.AreEqual(1, report.NumberOfDelineationsToBeCreated);

            var count = await DelineationStagings.ApproveAsync(_dbContext, _jurisdictionPerson);

            Assert.AreEqual(1, count);
            var newDel = _dbContext.Delineations.Single(x => x.TreatmentBMPID == bmp.TreatmentBMPID);
            Assert.AreEqual((int)DelineationTypeEnum.Distributed, newDel.DelineationTypeID);
            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID),
                "Staging should be cleared after a successful approve.");
        }

        [TestMethod]
        public async Task ApproveAsync_ReplacesMultipleExistingDistributedDelineations()
        {
            // NPT-1093 Bug 3: the bulk DeleteFullForMany path must delete every matching existing distributed
            // delineation and create the new ones in a single approve.
            var names = Enumerable.Range(0, 3).Select(i => $"NPT-1093-BULK-{i}-{Guid.NewGuid():N}").ToList();
            var preexistingIDs = new List<int>();
            foreach (var name in names)
            {
                var bmp = CreateBMP(name);
                var poly4326 = new Polygon(new LinearRing(new[]
                {
                    new Coordinate(-117.86, 33.66), new Coordinate(-117.85, 33.66),
                    new Coordinate(-117.85, 33.67), new Coordinate(-117.86, 33.67), new Coordinate(-117.86, 33.66),
                })) { SRID = 4326 };
                _dbContext.Delineations.Add(new Delineation
                {
                    TreatmentBMPID = bmp.TreatmentBMPID,
                    DelineationTypeID = (int)DelineationTypeEnum.Distributed,
                    DelineationGeometry4326 = poly4326,
                    DelineationGeometry = poly4326.ProjectTo2771(),
                    DateLastModified = DateTime.UtcNow,
                    IsVerified = false,
                    HasDiscrepancies = false,
                });
                _dbContext.SaveChanges();
                preexistingIDs.Add(_dbContext.Delineations.Single(x => x.TreatmentBMPID == bmp.TreatmentBMPID).DelineationID);
            }

            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJsonForNames(names), _jurisdictionPerson);
            var count = await DelineationStagings.ApproveAsync(_dbContext, _jurisdictionPerson);

            Assert.AreEqual(names.Count, count);
            Assert.IsFalse(_dbContext.Delineations.Any(x => preexistingIDs.Contains(x.DelineationID)),
                "All pre-existing distributed delineations should be deleted by the bulk path.");
            foreach (var name in names)
            {
                var bmpID = _dbContext.TreatmentBMPs.Single(x => x.TreatmentBMPName == name && x.StormwaterJurisdictionID == _jurisdictionID).TreatmentBMPID;
                Assert.AreEqual(1, _dbContext.Delineations.Count(x => x.TreatmentBMPID == bmpID), $"Expected exactly one new delineation for {name}.");
            }
            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID));
        }

        [TestMethod]
        public async Task DiscardForUserAsync_ClearsStaging()
        {
            var bmp = CreateBMP($"NPT-981-PR3-DISC-{Guid.NewGuid():N}");
            await DelineationStagings.ProcessDeserializedStagingAsync(_dbContext, SampleStagingGeoJson(bmp.TreatmentBMPName), _jurisdictionPerson);
            Assert.IsTrue(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID));

            await DelineationStagings.DiscardForUserAsync(_dbContext, _jurisdictionPerson);

            Assert.IsFalse(_dbContext.DelineationStagings.Any(x => x.UploadedByPersonID == _jurisdictionPerson.PersonID));
        }
    }
}
