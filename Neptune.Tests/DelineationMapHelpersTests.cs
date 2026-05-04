using System;
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
    [TestClass]
    public class DelineationMapHelpersTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _otherJurisdictionID;
        private int _personID;
        private int _adminPersonID;
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

            _adminPersonID = _dbContext.People.AsNoTracking()
                .Where(x => x.RoleID == (int)RoleEnum.Admin || x.RoleID == (int)RoleEnum.SitkaAdmin)
                .OrderBy(x => x.PersonID).Select(x => x.PersonID).FirstOrDefault();

            var jurisdictionPerson = _dbContext.StormwaterJurisdictionPeople.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID == _jurisdictionID)
                .Join(_dbContext.People.AsNoTracking(),
                    sjp => sjp.PersonID,
                    p => p.PersonID,
                    (sjp, p) => p)
                .Where(p => p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                .OrderBy(p => p.PersonID).FirstOrDefault();
            Assert.IsNotNull(jurisdictionPerson, $"Tests require a JurisdictionEditor/Manager assigned to jurisdiction {_jurisdictionID}.");
            _personID = jurisdictionPerson.PersonID;

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

        private Delineation CreateDelineation(int treatmentBMPID, DelineationTypeEnum type, bool isVerified)
        {
            var poly4326 = (Geometry)new Polygon(new LinearRing(new[]
            {
                new Coordinate(-117.85, 33.65),
                new Coordinate(-117.84, 33.65),
                new Coordinate(-117.84, 33.66),
                new Coordinate(-117.85, 33.66),
                new Coordinate(-117.85, 33.65),
            })) { SRID = 4326 };
            var delineation = new Delineation
            {
                TreatmentBMPID = treatmentBMPID,
                DelineationTypeID = (int)type,
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

        [TestMethod]
        public void GetByTreatmentBMPIDAsDto_ReturnsDtoWhenDelineationExists()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-Test-{Guid.NewGuid():N}");
            var delineation = CreateDelineation(bmp.TreatmentBMPID, DelineationTypeEnum.Distributed, isVerified: false);

            var dto = Delineations.GetByTreatmentBMPIDAsDto(_dbContext, bmp.TreatmentBMPID);

            Assert.IsNotNull(dto);
            Assert.AreEqual(delineation.DelineationID, dto!.DelineationID);
            Assert.AreEqual((int)DelineationTypeEnum.Distributed, dto.DelineationTypeID);
            Assert.IsFalse(dto.IsVerified);
            Assert.IsFalse(string.IsNullOrEmpty(dto.Geometry), "Geometry should be serialized to GeoJSON");
        }

        [TestMethod]
        public void GetByTreatmentBMPIDAsDto_ReturnsNullWhenNoDelineation()
        {
            var bmp = CreateBMP(_jurisdictionID, $"NPT-981-Test-{Guid.NewGuid():N}");

            var dto = Delineations.GetByTreatmentBMPIDAsDto(_dbContext, bmp.TreatmentBMPID);

            Assert.IsNull(dto);
        }

        [TestMethod]
        public async Task ListForDelineationMap_FiltersByJurisdictionForNonAdmin()
        {
            var myBMP = CreateBMP(_jurisdictionID, $"NPT-981-Mine-{Guid.NewGuid():N}");
            var otherBMP = CreateBMP(_otherJurisdictionID, $"NPT-981-Other-{Guid.NewGuid():N}");

            var person = People.GetByID(_dbContext, _personID);
            var dtos = await TreatmentBMPs.ListForDelineationMapAsync(_dbContext, person);

            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == myBMP.TreatmentBMPID), "Caller's jurisdiction BMP should be visible.");
            Assert.IsFalse(dtos.Any(x => x.TreatmentBMPID == otherBMP.TreatmentBMPID), "Other jurisdiction BMP should be hidden for non-admin.");
        }

        [TestMethod]
        public async Task ListForDelineationMap_AdminSeesAllJurisdictions()
        {
            if (_adminPersonID == 0)
            {
                Assert.Inconclusive("No Admin/SitkaAdmin user found in the local DB; skipping admin scope test.");
            }

            var myBMP = CreateBMP(_jurisdictionID, $"NPT-981-Mine-{Guid.NewGuid():N}");
            var otherBMP = CreateBMP(_otherJurisdictionID, $"NPT-981-Other-{Guid.NewGuid():N}");

            var admin = People.GetByID(_dbContext, _adminPersonID);
            var dtos = await TreatmentBMPs.ListForDelineationMapAsync(_dbContext, admin);

            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == myBMP.TreatmentBMPID));
            Assert.IsTrue(dtos.Any(x => x.TreatmentBMPID == otherBMP.TreatmentBMPID));
        }

        [TestMethod]
        public async Task ListForDelineationMap_PopulatesDelineationFlags()
        {
            var bmpWith = CreateBMP(_jurisdictionID, $"NPT-981-With-{Guid.NewGuid():N}");
            CreateDelineation(bmpWith.TreatmentBMPID, DelineationTypeEnum.Distributed, isVerified: true);
            var bmpWithout = CreateBMP(_jurisdictionID, $"NPT-981-Without-{Guid.NewGuid():N}");

            var person = People.GetByID(_dbContext, _personID);
            var dtos = await TreatmentBMPs.ListForDelineationMapAsync(_dbContext, person);

            var withRow = dtos.Single(x => x.TreatmentBMPID == bmpWith.TreatmentBMPID);
            Assert.IsTrue(withRow.HasDelineation);
            Assert.AreEqual((int)DelineationTypeEnum.Distributed, withRow.DelineationTypeID);
            Assert.IsTrue(withRow.IsVerified == true);

            var withoutRow = dtos.Single(x => x.TreatmentBMPID == bmpWithout.TreatmentBMPID);
            Assert.IsFalse(withoutRow.HasDelineation);
            Assert.IsNull(withoutRow.DelineationID);
        }
    }
}
