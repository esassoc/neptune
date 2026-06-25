using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1056 — covers the Manager Dashboard provisional grid projections and the
    /// bulk-verify helpers added on FieldVisits / TreatmentBMPs / Delineations. Integration-
    /// style: hits a real DB inside a transaction that rolls back on cleanup.
    /// </summary>
    [TestClass]
    public class ManagerDashboardTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private Person _jurisdictionPerson = null!;
        private int _treatmentBMPTypeID;
        private int _otherJurisdictionID;

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

            // Pick the first two jurisdictions so we have one "in scope" (assigned to the test
            // person) and one "out of scope" (not assigned) to exercise jurisdiction scoping.
            var jurisdictionIDs = _dbContext.StormwaterJurisdictions.AsNoTracking()
                .OrderBy(x => x.StormwaterJurisdictionID)
                .Select(x => x.StormwaterJurisdictionID)
                .Take(2)
                .ToList();
            Assert.IsTrue(jurisdictionIDs.Count >= 2, "Need at least 2 seeded jurisdictions for scoping assertions");
            _jurisdictionID = jurisdictionIDs[0];
            _otherJurisdictionID = jurisdictionIDs[1];

            // AsNoTracking — without it the eager Include below pulls Person nav into the
            // change tracker, and a subsequent `_dbContext.FieldVisits.Add(fieldVisit)` trips
            // NavigationFixer.ConditionallyNullForeignKeyProperties on the Person<->FieldVisit
            // graph. Memory: [feedback_ef_change_tracker_pollution].
            _jurisdictionPerson = _dbContext.StormwaterJurisdictionPeople.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID == _jurisdictionID)
                .Join(_dbContext.People.AsNoTracking().Include(p => p.StormwaterJurisdictionPeople),
                    sjp => sjp.PersonID, p => p.PersonID, (sjp, p) => p)
                .Where(p => p.RoleID == (int)RoleEnum.JurisdictionManager || p.RoleID == (int)RoleEnum.JurisdictionEditor)
                .OrderBy(p => p.PersonID)
                .First();

            _treatmentBMPTypeID = _dbContext.TreatmentBMPTypes.AsNoTracking()
                .OrderBy(x => x.TreatmentBMPTypeID)
                .Select(x => x.TreatmentBMPTypeID)
                .First();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private TreatmentBMP CreateBMP(string name, int? stormwaterJurisdictionID = null, bool inventoryIsVerified = false)
        {
            var jurisdictionID = stormwaterJurisdictionID ?? _jurisdictionID;
            var loc4326 = new Point(-117.85 + new Random().NextDouble() * 0.01, 33.65 + new Random().NextDouble() * 0.01) { SRID = 4326 };
            var ownerOrgID = _dbContext.StormwaterJurisdictions.AsNoTracking().Single(x => x.StormwaterJurisdictionID == jurisdictionID).OrganizationID;
            var bmp = new TreatmentBMP
            {
                TreatmentBMPName = name,
                TreatmentBMPTypeID = _treatmentBMPTypeID,
                StormwaterJurisdictionID = jurisdictionID,
                OwnerOrganizationID = ownerOrgID,
                LocationPoint4326 = loc4326,
                LocationPoint = loc4326.ProjectTo2771(),
                InventoryIsVerified = inventoryIsVerified,
                TreatmentBMPLifespanTypeID = (int)TreatmentBMPLifespanTypeEnum.Unspecified,
                SizingBasisTypeID = (int)SizingBasisTypeEnum.NotProvided,
                TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.None,
            };
            _dbContext.TreatmentBMPs.Add(bmp);
            _dbContext.SaveChanges();
            return bmp;
        }

        private Delineation CreateDelineation(TreatmentBMP bmp, bool isVerified)
        {
            var poly4326 = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.85, 33.65), new Coordinate(-117.84, 33.65),
                new Coordinate(-117.84, 33.66), new Coordinate(-117.85, 33.66), new Coordinate(-117.85, 33.65),
            })) { SRID = 4326 };
            var delineation = new Delineation
            {
                TreatmentBMPID = bmp.TreatmentBMPID,
                DelineationTypeID = (int)DelineationTypeEnum.Distributed,
                DelineationGeometry4326 = poly4326,
                DelineationGeometry = poly4326.ProjectTo2771(),
                DateLastModified = DateTime.UtcNow,
                IsVerified = isVerified,
                HasDiscrepancies = false,
            };
            _dbContext.Delineations.Add(delineation);
            _dbContext.SaveChanges();
            return delineation;
        }

        private FieldVisit CreateFieldVisit(TreatmentBMP bmp, bool isVerified)
        {
            // Clear the change tracker before each insert so previously-tracked navigations
            // (TreatmentBMP -> Organization -> Person etc) don't trigger NavigationFixer
            // null-FK fixups when we add a new FieldVisit. Memory: feedback_ef_change_tracker_pollution.
            _dbContext.ChangeTracker.Clear();
            var fieldVisit = new FieldVisit
            {
                TreatmentBMPID = bmp.TreatmentBMPID,
                PerformedByPersonID = _jurisdictionPerson.PersonID,
                VisitDate = DateTime.UtcNow.AddDays(-1),
                FieldVisitStatusID = isVerified ? (int)FieldVisitStatusEnum.Complete : (int)FieldVisitStatusEnum.InProgress,
                FieldVisitTypeID = (int)FieldVisitTypeEnum.DryWeather,
                IsFieldVisitVerified = isVerified,
                InventoryUpdated = false,
            };
            _dbContext.FieldVisits.Add(fieldVisit);
            _dbContext.SaveChanges();
            return fieldVisit;
        }

        // -------- Field Visits provisional grid + bulk-verify --------

        [TestMethod]
        public async Task GetProvisionalFieldVisitsAsGridDto_OmitsVerifiedRows()
        {
            var bmp = CreateBMP($"NPT-1056-FV-{Guid.NewGuid():N}");
            var provisionalVisit = CreateFieldVisit(bmp, isVerified: false);
            var verifiedVisit = CreateFieldVisit(bmp, isVerified: true);

            var rows = await vFieldVisitDetaileds.GetProvisionalFieldVisitsAsGridDtoAsync(_dbContext, _jurisdictionPerson);

            Assert.IsTrue(rows.Any(r => r.FieldVisitID == provisionalVisit.FieldVisitID), "Provisional visit should appear");
            Assert.IsFalse(rows.Any(r => r.FieldVisitID == verifiedVisit.FieldVisitID), "Verified visit should be filtered out");
        }

        [TestMethod]
        public async Task BulkMarkFieldVisitsAsVerified_HappyPath_FlipsFlag_AndAdvancesStatus()
        {
            // Each FieldVisit needs its own BMP — the schema unique index
            // CK_AtMostOneFieldVisitMayBeInProgressAtAnyTimePerBMP rejects two In-Progress
            // visits on the same BMP.
            var bmp1 = CreateBMP($"NPT-1056-FV-BULK-1-{Guid.NewGuid():N}");
            var bmp2 = CreateBMP($"NPT-1056-FV-BULK-2-{Guid.NewGuid():N}");
            var fv1 = CreateFieldVisit(bmp1, isVerified: false);
            var fv2 = CreateFieldVisit(bmp2, isVerified: false);

            var count = await FieldVisits.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { fv1.FieldVisitID, fv2.FieldVisitID }, _jurisdictionPerson);

            Assert.AreEqual(2, count);
            var refreshed1 = _dbContext.FieldVisits.AsNoTracking().Single(x => x.FieldVisitID == fv1.FieldVisitID);
            var refreshed2 = _dbContext.FieldVisits.AsNoTracking().Single(x => x.FieldVisitID == fv2.FieldVisitID);
            Assert.IsTrue(refreshed1.IsFieldVisitVerified);
            Assert.IsTrue(refreshed2.IsFieldVisitVerified);
            // VerifyFieldVisit also bumps the status to Complete.
            Assert.AreEqual((int)FieldVisitStatusEnum.Complete, refreshed1.FieldVisitStatusID);
            Assert.AreEqual((int)FieldVisitStatusEnum.Complete, refreshed2.FieldVisitStatusID);
        }

        [TestMethod]
        public async Task BulkMarkFieldVisitsAsVerified_DropsOutOfJurisdictionIDs()
        {
            var inJurisdictionBMP = CreateBMP($"NPT-1056-FV-IN-{Guid.NewGuid():N}");
            var outOfJurisdictionBMP = CreateBMP($"NPT-1056-FV-OUT-{Guid.NewGuid():N}", _otherJurisdictionID);
            var inFV = CreateFieldVisit(inJurisdictionBMP, isVerified: false);
            var outFV = CreateFieldVisit(outOfJurisdictionBMP, isVerified: false);

            var count = await FieldVisits.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { inFV.FieldVisitID, outFV.FieldVisitID }, _jurisdictionPerson);

            Assert.AreEqual(1, count, "Only the in-jurisdiction visit should be verified");
            var refreshedIn = _dbContext.FieldVisits.AsNoTracking().Single(x => x.FieldVisitID == inFV.FieldVisitID);
            var refreshedOut = _dbContext.FieldVisits.AsNoTracking().Single(x => x.FieldVisitID == outFV.FieldVisitID);
            Assert.IsTrue(refreshedIn.IsFieldVisitVerified);
            Assert.IsFalse(refreshedOut.IsFieldVisitVerified, "Out-of-jurisdiction visit must not be touched");
        }

        // -------- BMP Records provisional grid + bulk-verify --------

        [TestMethod]
        public async Task GetProvisionalTreatmentBMPsAsGridDto_OmitsVerifiedRows()
        {
            var provisionalBMP = CreateBMP($"NPT-1056-BMP-P-{Guid.NewGuid():N}", inventoryIsVerified: false);
            var verifiedBMP = CreateBMP($"NPT-1056-BMP-V-{Guid.NewGuid():N}", inventoryIsVerified: true);

            var rows = await TreatmentBMPs.GetProvisionalTreatmentBMPsAsGridDtoAsync(_dbContext, _jurisdictionPerson);

            Assert.IsTrue(rows.Any(r => r.TreatmentBMPID == provisionalBMP.TreatmentBMPID), "Unverified BMP should appear");
            Assert.IsFalse(rows.Any(r => r.TreatmentBMPID == verifiedBMP.TreatmentBMPID), "Verified BMP should be filtered out");
        }

        [TestMethod]
        public async Task BulkMarkTreatmentBMPsAsVerified_HappyPath_SetsVerifiedFlags()
        {
            var bmp1 = CreateBMP($"NPT-1056-BMP-BULK-1-{Guid.NewGuid():N}");
            var bmp2 = CreateBMP($"NPT-1056-BMP-BULK-2-{Guid.NewGuid():N}");

            var count = await TreatmentBMPs.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { bmp1.TreatmentBMPID, bmp2.TreatmentBMPID }, _jurisdictionPerson);

            Assert.AreEqual(2, count);
            var refreshed1 = _dbContext.TreatmentBMPs.AsNoTracking().Single(x => x.TreatmentBMPID == bmp1.TreatmentBMPID);
            var refreshed2 = _dbContext.TreatmentBMPs.AsNoTracking().Single(x => x.TreatmentBMPID == bmp2.TreatmentBMPID);
            Assert.IsTrue(refreshed1.InventoryIsVerified);
            Assert.IsTrue(refreshed2.InventoryIsVerified);
            Assert.AreEqual(_jurisdictionPerson.PersonID, refreshed1.InventoryVerifiedByPersonID);
            Assert.IsNotNull(refreshed1.DateOfLastInventoryVerification);
        }

        [TestMethod]
        public async Task BulkMarkTreatmentBMPsAsVerified_DropsOutOfJurisdictionIDs()
        {
            var inBMP = CreateBMP($"NPT-1056-BMP-IN-{Guid.NewGuid():N}");
            var outBMP = CreateBMP($"NPT-1056-BMP-OUT-{Guid.NewGuid():N}", _otherJurisdictionID);

            var count = await TreatmentBMPs.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { inBMP.TreatmentBMPID, outBMP.TreatmentBMPID }, _jurisdictionPerson);

            Assert.AreEqual(1, count);
            var refreshedIn = _dbContext.TreatmentBMPs.AsNoTracking().Single(x => x.TreatmentBMPID == inBMP.TreatmentBMPID);
            var refreshedOut = _dbContext.TreatmentBMPs.AsNoTracking().Single(x => x.TreatmentBMPID == outBMP.TreatmentBMPID);
            Assert.IsTrue(refreshedIn.InventoryIsVerified);
            Assert.IsFalse(refreshedOut.InventoryIsVerified, "Out-of-jurisdiction BMP must not be touched");
        }

        // -------- Delineations provisional grid + bulk-verify --------

        [TestMethod]
        public async Task GetProvisionalBMPDelineationsAsGridDto_OmitsVerifiedRows()
        {
            var bmp1 = CreateBMP($"NPT-1056-DEL-P-{Guid.NewGuid():N}");
            var bmp2 = CreateBMP($"NPT-1056-DEL-V-{Guid.NewGuid():N}");
            var provisional = CreateDelineation(bmp1, isVerified: false);
            var verified = CreateDelineation(bmp2, isVerified: true);

            var rows = await Delineations.GetProvisionalBMPDelineationsAsGridDtoAsync(_dbContext, _jurisdictionPerson);

            Assert.IsTrue(rows.Any(r => r.DelineationID == provisional.DelineationID), "Unverified delineation should appear");
            Assert.IsFalse(rows.Any(r => r.DelineationID == verified.DelineationID), "Verified delineation should be filtered out");
        }

        [TestMethod]
        public async Task BulkMarkDelineationsAsVerified_HappyPath_SetsVerifiedFlagsAndDirtiesModel()
        {
            var bmp1 = CreateBMP($"NPT-1056-DEL-BULK-1-{Guid.NewGuid():N}");
            var bmp2 = CreateBMP($"NPT-1056-DEL-BULK-2-{Guid.NewGuid():N}");
            var del1 = CreateDelineation(bmp1, isVerified: false);
            var del2 = CreateDelineation(bmp2, isVerified: false);

            var count = await Delineations.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { del1.DelineationID, del2.DelineationID }, _jurisdictionPerson);

            Assert.AreEqual(2, count);
            var refreshed1 = _dbContext.Delineations.AsNoTracking().Single(x => x.DelineationID == del1.DelineationID);
            var refreshed2 = _dbContext.Delineations.AsNoTracking().Single(x => x.DelineationID == del2.DelineationID);
            Assert.IsTrue(refreshed1.IsVerified);
            Assert.IsTrue(refreshed2.IsVerified);
            Assert.AreEqual(_jurisdictionPerson.PersonID, refreshed1.VerifiedByPersonID);
            Assert.IsNotNull(refreshed1.DateLastVerified);
            // NereidUtilities.MarkDelineationDirty is invoked unconditionally by the helper; we
            // don't assert on DirtyModelNode rows because that side effect depends on the
            // upstream RSB topology of the synthetic test BMP, which isn't wired here.
        }

        [TestMethod]
        public async Task BulkMarkDelineationsAsVerified_DropsOutOfJurisdictionIDs()
        {
            var inBMP = CreateBMP($"NPT-1056-DEL-IN-{Guid.NewGuid():N}");
            var outBMP = CreateBMP($"NPT-1056-DEL-OUT-{Guid.NewGuid():N}", _otherJurisdictionID);
            var inDel = CreateDelineation(inBMP, isVerified: false);
            var outDel = CreateDelineation(outBMP, isVerified: false);

            var count = await Delineations.BulkMarkAsVerifiedAsync(_dbContext, new List<int> { inDel.DelineationID, outDel.DelineationID }, _jurisdictionPerson);

            Assert.AreEqual(1, count);
            var refreshedIn = _dbContext.Delineations.AsNoTracking().Single(x => x.DelineationID == inDel.DelineationID);
            var refreshedOut = _dbContext.Delineations.AsNoTracking().Single(x => x.DelineationID == outDel.DelineationID);
            Assert.IsTrue(refreshedIn.IsVerified);
            Assert.IsFalse(refreshedOut.IsVerified, "Out-of-jurisdiction delineation must not be touched");
        }

        // -------- Empty / no-op behaviors --------

        [TestMethod]
        public async Task BulkMarkFieldVisitsAsVerified_EmptyList_ReturnsZero()
        {
            var count = await FieldVisits.BulkMarkAsVerifiedAsync(_dbContext, new List<int>(), _jurisdictionPerson);
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task BulkMarkTreatmentBMPsAsVerified_EmptyList_ReturnsZero()
        {
            var count = await TreatmentBMPs.BulkMarkAsVerifiedAsync(_dbContext, new List<int>(), _jurisdictionPerson);
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task BulkMarkDelineationsAsVerified_EmptyList_ReturnsZero()
        {
            var count = await Delineations.BulkMarkAsVerifiedAsync(_dbContext, new List<int>(), _jurisdictionPerson);
            Assert.AreEqual(0, count);
        }
    }
}
