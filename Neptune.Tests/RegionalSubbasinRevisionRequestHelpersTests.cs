using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    [TestClass]
    public class RegionalSubbasinRevisionRequestHelpersTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _otherJurisdictionID;
        private Person _jurisdictionPerson = null!;
        private Person _adminPerson = null!;
        private Person _otherJurisdictionPerson = null!;
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

            var jurisdictions = _dbContext.StormwaterJurisdictions.AsNoTracking().OrderBy(x => x.StormwaterJurisdictionID).Take(2).ToList();
            Assert.IsTrue(jurisdictions.Count >= 2, "Tests require at least 2 StormwaterJurisdiction rows in the local DB.");
            _jurisdictionID = jurisdictions[0].StormwaterJurisdictionID;
            _otherJurisdictionID = jurisdictions[1].StormwaterJurisdictionID;

            _adminPerson = _dbContext.People
                .Include(x => x.StormwaterJurisdictionPeople)
                .Where(x => x.RoleID == (int)RoleEnum.Admin || x.RoleID == (int)RoleEnum.SitkaAdmin)
                .OrderBy(x => x.PersonID).First();

            _jurisdictionPerson = _dbContext.StormwaterJurisdictionPeople
                .Where(x => x.StormwaterJurisdictionID == _jurisdictionID)
                .Join(_dbContext.People.Include(p => p.StormwaterJurisdictionPeople),
                    sjp => sjp.PersonID,
                    p => p.PersonID,
                    (sjp, p) => p)
                .Where(p => p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                .OrderBy(p => p.PersonID).First();

            _otherJurisdictionPerson = _dbContext.StormwaterJurisdictionPeople
                .Where(x => x.StormwaterJurisdictionID == _otherJurisdictionID)
                .Join(_dbContext.People.Include(p => p.StormwaterJurisdictionPeople),
                    sjp => sjp.PersonID,
                    p => p.PersonID,
                    (sjp, p) => p)
                .Where(p => p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                .OrderBy(p => p.PersonID).First();

            _treatmentBMPTypeID = _dbContext.TreatmentBMPTypes.AsNoTracking().OrderBy(x => x.TreatmentBMPTypeID).Select(x => x.TreatmentBMPTypeID).First();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private TreatmentBMP CreateBMP(int jurisdictionID, string name)
        {
            var locationPoint4326 = new Point(-117.85 + new Random().NextDouble() * 0.01, 33.65 + new Random().NextDouble() * 0.01) { SRID = 4326 };
            var locationPoint = locationPoint4326.ProjectTo2771();
            var bmp = new TreatmentBMP
            {
                TreatmentBMPName = name,
                TreatmentBMPTypeID = _treatmentBMPTypeID,
                StormwaterJurisdictionID = jurisdictionID,
                OwnerOrganizationID = _dbContext.StormwaterJurisdictions.AsNoTracking().Single(x => x.StormwaterJurisdictionID == jurisdictionID).OrganizationID,
                LocationPoint4326 = locationPoint4326,
                LocationPoint = locationPoint,
                InventoryIsVerified = false,
                TreatmentBMPLifespanTypeID = (int)TreatmentBMPLifespanTypeEnum.Unspecified,
                SizingBasisTypeID = (int)SizingBasisTypeEnum.NotProvided,
                TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.None,
            };
            _dbContext.TreatmentBMPs.Add(bmp);
            _dbContext.SaveChanges();
            return bmp;
        }

        private static string SamplePolygonGeoJson()
        {
            var poly = new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.85, 33.65),
                new Coordinate(-117.84, 33.65),
                new Coordinate(-117.84, 33.66),
                new Coordinate(-117.85, 33.66),
                new Coordinate(-117.85, 33.65),
            })) { SRID = 4326 };
            var feature = new Feature(poly, new AttributesTable());
            return GeoJsonSerializer.Serialize(feature);
        }

        [TestMethod]
        public async Task CreateAsync_PersistsRequestWithOpenStatus()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-PR2-{Guid.NewGuid():N}");
            var entity = await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmp.TreatmentBMPID, SamplePolygonGeoJson(), "test notes", _jurisdictionPerson);

            Assert.IsTrue(entity.RegionalSubbasinRevisionRequestID > 0);
            Assert.AreEqual((int)RegionalSubbasinRevisionRequestStatusEnum.Open, entity.RegionalSubbasinRevisionRequestStatusID);
            Assert.AreEqual(_jurisdictionPerson.PersonID, entity.RequestPersonID);
            Assert.AreEqual("test notes", entity.Notes);
            Assert.IsNotNull(entity.RegionalSubbasinRevisionRequestGeometry);
        }

        [TestMethod]
        public async Task CreateAsync_BlocksWhenOpenRequestExists()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-PR2-{Guid.NewGuid():N}");
            await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmp.TreatmentBMPID, SamplePolygonGeoJson(), null, _jurisdictionPerson);

            await Assert.ThrowsExactlyAsync<AssertionException>(async () =>
            {
                await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmp.TreatmentBMPID, SamplePolygonGeoJson(), null, _jurisdictionPerson);
            });
        }

        [TestMethod]
        public async Task CloseAsync_SetsAuditFieldsAndStatus()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-PR2-{Guid.NewGuid():N}");
            var created = await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmp.TreatmentBMPID, SamplePolygonGeoJson(), null, _jurisdictionPerson);

            var closed = await RegionalSubbasinRevisionRequests.CloseAsync(_dbContext, created.RegionalSubbasinRevisionRequestID, "all set", _adminPerson);

            Assert.AreEqual((int)RegionalSubbasinRevisionRequestStatusEnum.Closed, closed.RegionalSubbasinRevisionRequestStatusID);
            Assert.AreEqual(_adminPerson.PersonID, closed.ClosedByPersonID);
            Assert.AreEqual("all set", closed.CloseNotes);
            Assert.IsNotNull(closed.ClosedDate);
        }

        [TestMethod]
        public async Task GetByIDAsDto_ProjectsGeometryAs4326GeoJson()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-PR2-{Guid.NewGuid():N}");
            var created = await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmp.TreatmentBMPID, SamplePolygonGeoJson(), "n", _jurisdictionPerson);

            var dto = RegionalSubbasinRevisionRequests.GetByIDAsDto(_dbContext, created.RegionalSubbasinRevisionRequestID);

            Assert.IsNotNull(dto);
            Assert.AreEqual(bmp.TreatmentBMPID, dto!.TreatmentBMPID);
            Assert.AreEqual(bmp.TreatmentBMPName, dto.TreatmentBMPName);
            Assert.AreEqual("Open", dto.RegionalSubbasinRevisionRequestStatusDisplayName);
            Assert.IsFalse(string.IsNullOrEmpty(dto.GeometryGeoJson), "GeometryGeoJson should be populated");
            StringAssert.Contains(dto.GeometryGeoJson, "Polygon");
        }

        [TestMethod]
        public async Task ListAsDto_AdminSeesAllJurisdictions()
        {
            var bmpA = CreateBMP(_jurisdictionID, $"NPT-981-PR2-A-{Guid.NewGuid():N}");
            var bmpB = CreateBMP(_otherJurisdictionID, $"NPT-981-PR2-B-{Guid.NewGuid():N}");
            await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmpA.TreatmentBMPID, SamplePolygonGeoJson(), null, _jurisdictionPerson);
            await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmpB.TreatmentBMPID, SamplePolygonGeoJson(), null, _otherJurisdictionPerson);

            var dtos = RegionalSubbasinRevisionRequests.ListAsDto(_dbContext, _adminPerson);

            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == bmpA.TreatmentBMPID));
            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == bmpB.TreatmentBMPID));
        }

        [TestMethod]
        public async Task ListAsDto_JurisdictionPersonSeesOnlyOwnJurisdictionRequests()
        {
            var bmpA = CreateBMP(_jurisdictionID, $"NPT-981-PR2-A-{Guid.NewGuid():N}");
            var bmpB = CreateBMP(_otherJurisdictionID, $"NPT-981-PR2-B-{Guid.NewGuid():N}");
            await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmpA.TreatmentBMPID, SamplePolygonGeoJson(), null, _jurisdictionPerson);
            await RegionalSubbasinRevisionRequests.CreateAsync(_dbContext, bmpB.TreatmentBMPID, SamplePolygonGeoJson(), null, _otherJurisdictionPerson);

            var dtos = RegionalSubbasinRevisionRequests.ListAsDto(_dbContext, _jurisdictionPerson);

            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == bmpA.TreatmentBMPID));
            Assert.IsFalse(dtos.Any(x => x.TreatmentBMPID == bmpB.TreatmentBMPID));
        }
    }
}
