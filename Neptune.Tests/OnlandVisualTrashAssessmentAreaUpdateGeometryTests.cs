using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1066: the Edit OVTA Area Location page gained a Land Use Block source option.
    /// OnlandVisualTrashAssessmentAreas.UpdateGeometry now branches on OvtaAreaSourceTypeID, so
    /// the LandUseBlock branch must union the selected blocks onto the area geometry (and project
    /// the 4326 copy). Real-DB integration test inside a rolled-back transaction, mirroring
    /// OnlandVisualTrashAssessmentAreaMoveAssessmentsTests.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentAreaUpdateGeometryTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

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

        [TestMethod]
        public void UpdateGeometry_LandUseBlockSource_UnionsSelectedBlocksOntoArea()
        {
            // Find a jurisdiction that already has at least two land use blocks with geometry.
            var jurisdictionWithBlocks = _dbContext.LandUseBlocks.AsNoTracking()
                .Where(x => x.LandUseBlockGeometry != null)
                .GroupBy(x => x.StormwaterJurisdictionID)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key)
                .FirstOrDefault();
            if (jurisdictionWithBlocks == 0)
            {
                Assert.Inconclusive("Local NeptuneDB has no jurisdiction with >= 2 land use blocks to exercise the LUB union branch.");
            }

            var blockIDs = _dbContext.LandUseBlocks.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID == jurisdictionWithBlocks && x.LandUseBlockGeometry != null)
                .OrderBy(x => x.LandUseBlockID)
                .Select(x => x.LandUseBlockID)
                .Take(2)
                .ToList();

            // Seed an OVTA area in that jurisdiction with a throwaway starting geometry.
            var expectedUnion = LandUseBlocks.UnionAggregateByLandUseBlockIDs(_dbContext, blockIDs, jurisdictionWithBlocks);
            var startingGeometry = GeometryHelper.FromWKT(
                "POLYGON((1840000 660000, 1840100 660000, 1840100 660100, 1840000 660100, 1840000 660000))",
                Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
            var area = new OnlandVisualTrashAssessmentArea
            {
                OnlandVisualTrashAssessmentAreaName = $"NPT-1066 test {Guid.NewGuid()}",
                StormwaterJurisdictionID = jurisdictionWithBlocks,
                OnlandVisualTrashAssessmentAreaGeometry = startingGeometry,
            };
            _dbContext.OnlandVisualTrashAssessmentAreas.Add(area);
            _dbContext.SaveChanges();

            var dto = new OnlandVisualTrashAssessmentAreaGeometryDto
            {
                OnlandVisualTrashAssessmentAreaID = area.OnlandVisualTrashAssessmentAreaID,
                OvtaAreaSourceTypeID = (int)OvtaAreaSourceTypeEnum.LandUseBlock,
                SelectedLandUseBlockIDs = blockIDs,
            };

            OnlandVisualTrashAssessmentAreas.UpdateGeometry(_dbContext, dto);
            _dbContext.SaveChanges();

            var saved = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Single(x => x.OnlandVisualTrashAssessmentAreaID == area.OnlandVisualTrashAssessmentAreaID);

            Assert.IsNotNull(saved.OnlandVisualTrashAssessmentAreaGeometry, "State Plane geometry should be set from the LUB union.");
            Assert.IsNotNull(saved.OnlandVisualTrashAssessmentAreaGeometry4326, "4326 geometry should be projected from the union.");
            Assert.AreEqual(expectedUnion!.Area, saved.OnlandVisualTrashAssessmentAreaGeometry.Area, 1.0,
                "Saved area geometry should equal the union of the selected land use blocks.");
        }
    }
}
