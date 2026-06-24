using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    [TestClass]
    public class CustomAttributesMarkDirtyTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _treatmentBMPTypeID;
        private PersonDto _callingUser = null!;

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

            _treatmentBMPTypeID = _dbContext.TreatmentBMPTypes.AsNoTracking()
                .OrderBy(x => x.TreatmentBMPTypeID).Select(x => x.TreatmentBMPTypeID).First();

            var personID = _dbContext.People.AsNoTracking().OrderBy(x => x.PersonID).Select(x => x.PersonID).First();
            _callingUser = new PersonDto { PersonID = personID };
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

        [TestMethod]
        public async Task UpdateModelingCustomAttributes_MarksBMPDirty()
        {
            // NPT-1062: editing a BMP's Modeling attributes must create a DirtyModelNode so the detail page shows
            // "awaiting calculation" and the hourly delta solve recalculates it (previously it never marked dirty).
            var bmp = CreateBMP($"NPT-1062-{Guid.NewGuid():N}");
            Assert.IsFalse(_dbContext.DirtyModelNodes.Any(x => x.TreatmentBMPID == bmp.TreatmentBMPID),
                "Precondition: no dirty node before the modeling-attribute save.");

            await CustomAttributes.UpdateCustomAttributesAsync(_dbContext, bmp.TreatmentBMPID,
                (int)CustomAttributeTypePurposeEnum.Modeling, new List<CustomAttributeUpsertDto>(), _callingUser);

            Assert.IsTrue(_dbContext.DirtyModelNodes.Any(x => x.TreatmentBMPID == bmp.TreatmentBMPID),
                "Saving Modeling custom attributes should mark the BMP dirty.");
        }

        [TestMethod]
        public async Task UpdateNonModelingCustomAttributes_DoesNotMarkBMPDirty()
        {
            // Scoping guard: non-modeling custom-attribute purposes must NOT mark the BMP dirty.
            var bmp = CreateBMP($"NPT-1062-NM-{Guid.NewGuid():N}");

            await CustomAttributes.UpdateCustomAttributesAsync(_dbContext, bmp.TreatmentBMPID,
                (int)CustomAttributeTypePurposeEnum.OtherDesignAttributes, new List<CustomAttributeUpsertDto>(), _callingUser);

            Assert.IsFalse(_dbContext.DirtyModelNodes.Any(x => x.TreatmentBMPID == bmp.TreatmentBMPID),
                "Non-Modeling custom-attribute edits should not mark the BMP dirty.");
        }
    }
}
