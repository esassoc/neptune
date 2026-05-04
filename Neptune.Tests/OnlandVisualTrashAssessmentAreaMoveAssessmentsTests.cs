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
    public class OnlandVisualTrashAssessmentAreaMoveAssessmentsTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _otherJurisdictionID;
        private int _personID;

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

            _personID = _dbContext.People.AsNoTracking().OrderBy(x => x.PersonID).Select(x => x.PersonID).First();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private static Geometry MakeSquare(double x0, double y0, double size)
        {
            var wkt = $"POLYGON(({x0} {y0}, {x0 + size} {y0}, {x0 + size} {y0 + size}, {x0} {y0 + size}, {x0} {y0}))";
            return GeometryHelper.FromWKT(wkt, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
        }

        private OnlandVisualTrashAssessmentArea CreateArea(int jurisdictionID, string name, double xOffset = 0)
        {
            var area = new OnlandVisualTrashAssessmentArea
            {
                OnlandVisualTrashAssessmentAreaName = name,
                StormwaterJurisdictionID = jurisdictionID,
                OnlandVisualTrashAssessmentAreaGeometry = MakeSquare(1_840_000 + xOffset, 660_000, 100),
            };
            _dbContext.OnlandVisualTrashAssessmentAreas.Add(area);
            _dbContext.SaveChanges();
            return area;
        }

        private OnlandVisualTrashAssessment CreateCompletedAssessment(
            int jurisdictionID,
            int areaID,
            DateOnly completedDate,
            OnlandVisualTrashAssessmentScoreEnum score,
            bool isProgressAssessment = false,
            bool isTransectBacking = false)
        {
            var assessment = new OnlandVisualTrashAssessment
            {
                CreatedByPersonID = _personID,
                CreatedDate = DateTime.UtcNow,
                OnlandVisualTrashAssessmentAreaID = areaID,
                StormwaterJurisdictionID = jurisdictionID,
                OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.Complete,
                OnlandVisualTrashAssessmentScoreID = (int)score,
                CompletedDate = completedDate,
                IsTransectBackingAssessment = isTransectBacking,
                IsProgressAssessment = isProgressAssessment,
            };
            _dbContext.OnlandVisualTrashAssessments.Add(assessment);
            _dbContext.SaveChanges();
            return assessment;
        }

        [TestMethod]
        public async Task MoveAssessments_MovesAllAssessments_FromSourceToTarget()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Test Source {unique}", 0);
            var target = CreateArea(_jurisdictionID, $"Test Target {unique}", 200);

            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2020, 5, 1), OnlandVisualTrashAssessmentScoreEnum.B);
            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2021, 5, 1), OnlandVisualTrashAssessmentScoreEnum.C);

            await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, target.OnlandVisualTrashAssessmentAreaID);

            var sourceAssessments = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Count(x => x.OnlandVisualTrashAssessmentAreaID == source.OnlandVisualTrashAssessmentAreaID);
            var targetAssessments = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Count(x => x.OnlandVisualTrashAssessmentAreaID == target.OnlandVisualTrashAssessmentAreaID);

            Assert.AreEqual(0, sourceAssessments, "All assessments should have moved off the source.");
            Assert.AreEqual(2, targetAssessments, "All assessments should now point to the target.");
        }

        [TestMethod]
        public async Task MoveAssessments_RecomputesTargetBaselineScore()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Test Source {unique}", 0);
            var target = CreateArea(_jurisdictionID, $"Test Target {unique}", 200);

            // Existing baseline on target (1 assessment) — not enough alone
            CreateCompletedAssessment(_jurisdictionID, target.OnlandVisualTrashAssessmentAreaID, new DateOnly(2019, 5, 1), OnlandVisualTrashAssessmentScoreEnum.B);
            // Source has 2 baselines that, together with target's 1, average to a single rounded score
            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2020, 5, 1), OnlandVisualTrashAssessmentScoreEnum.A); // 1
            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2021, 5, 1), OnlandVisualTrashAssessmentScoreEnum.A); // 1

            await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, target.OnlandVisualTrashAssessmentAreaID);

            var refreshedTarget = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Single(x => x.OnlandVisualTrashAssessmentAreaID == target.OnlandVisualTrashAssessmentAreaID);
            // Average of A(1), A(1), B(2) = 1.33 → rounds to 1 → score A
            Assert.AreEqual((int)OnlandVisualTrashAssessmentScoreEnum.A, refreshedTarget.OnlandVisualTrashAssessmentBaselineScoreID);
        }

        [TestMethod]
        public async Task MoveAssessments_RejectsCrossJurisdiction()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Test Source {unique}", 0);
            var target = CreateArea(_otherJurisdictionID, $"Test Target {unique}", 200);

            await Assert.ThrowsAsync<Common.DesignByContract.PreconditionException>(async () =>
                await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, target.OnlandVisualTrashAssessmentAreaID));
        }

        [TestMethod]
        public async Task MoveAssessments_RejectsSelfMove()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Test Source {unique}", 0);

            await Assert.ThrowsAsync<Common.DesignByContract.PreconditionException>(async () =>
                await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, source.OnlandVisualTrashAssessmentAreaID));
        }

        [TestMethod]
        public async Task MoveAssessments_RejectsWhenSourceHasInProgressAssessment()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Test Source {unique}", 0);
            var target = CreateArea(_jurisdictionID, $"Test Target {unique}", 200);

            // In-progress assessment with the source as official area is forbidden by check constraint, so use a "complete except status" hack via direct SQL.
            // Simplest: assessment with OnlandVisualTrashAssessmentAreaID set but Status != Complete, no DraftGeometry — passes the CHECK constraint.
            var inProgress = new OnlandVisualTrashAssessment
            {
                CreatedByPersonID = _personID,
                CreatedDate = DateTime.UtcNow,
                OnlandVisualTrashAssessmentAreaID = source.OnlandVisualTrashAssessmentAreaID,
                StormwaterJurisdictionID = _jurisdictionID,
                OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress,
                IsTransectBackingAssessment = false,
                IsProgressAssessment = false,
            };
            _dbContext.OnlandVisualTrashAssessments.Add(inProgress);
            _dbContext.SaveChanges();

            await Assert.ThrowsAsync<Common.DesignByContract.PreconditionException>(async () =>
                await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, target.OnlandVisualTrashAssessmentAreaID));
        }

        [TestMethod]
        public async Task DeleteArea_NullsOutTrashGeneratingUnitForeignKeys()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var area = CreateArea(_jurisdictionID, $"Test Area {unique}", 0);

            var tgu = new TrashGeneratingUnit
            {
                StormwaterJurisdictionID = _jurisdictionID,
                OnlandVisualTrashAssessmentAreaID = area.OnlandVisualTrashAssessmentAreaID,
                TrashGeneratingUnitGeometry = MakeSquare(1_840_000, 660_000, 50),
            };
            _dbContext.TrashGeneratingUnits.Add(tgu);
            _dbContext.SaveChanges();

            await OnlandVisualTrashAssessmentAreas.DeleteAreaAsync(_dbContext, area.OnlandVisualTrashAssessmentAreaID);

            var refreshedTgu = _dbContext.TrashGeneratingUnits.AsNoTracking().Single(x => x.TrashGeneratingUnitID == tgu.TrashGeneratingUnitID);
            Assert.IsNull(refreshedTgu.OnlandVisualTrashAssessmentAreaID, "TGU should have its OVTA Area FK nulled out, not be deleted.");

            var areaStillExists = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Any(x => x.OnlandVisualTrashAssessmentAreaID == area.OnlandVisualTrashAssessmentAreaID);
            Assert.IsFalse(areaStillExists, "Area should have been deleted.");
        }

        [TestMethod]
        public async Task DeleteArea_CascadesAssessments()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var area = CreateArea(_jurisdictionID, $"Test Area {unique}", 0);
            var assessment = CreateCompletedAssessment(_jurisdictionID, area.OnlandVisualTrashAssessmentAreaID, new DateOnly(2020, 5, 1), OnlandVisualTrashAssessmentScoreEnum.B);

            await OnlandVisualTrashAssessmentAreas.DeleteAreaAsync(_dbContext, area.OnlandVisualTrashAssessmentAreaID);

            var areaStillExists = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Any(x => x.OnlandVisualTrashAssessmentAreaID == area.OnlandVisualTrashAssessmentAreaID);
            Assert.IsFalse(areaStillExists, "Area should have been deleted.");

            var assessmentStillExists = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Any(x => x.OnlandVisualTrashAssessmentID == assessment.OnlandVisualTrashAssessmentID);
            Assert.IsFalse(assessmentStillExists, "Assessment should have been cascade-deleted with the area.");
        }

        [TestMethod]
        public async Task MoveThenDelete_FullPath_LeavesNoOrphans()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var source = CreateArea(_jurisdictionID, $"Camino Real Dup {unique}", 0);
            var target = CreateArea(_jurisdictionID, $"Camino Real {unique}", 200);

            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2020, 5, 1), OnlandVisualTrashAssessmentScoreEnum.B);
            CreateCompletedAssessment(_jurisdictionID, source.OnlandVisualTrashAssessmentAreaID, new DateOnly(2021, 5, 1), OnlandVisualTrashAssessmentScoreEnum.B);
            CreateCompletedAssessment(_jurisdictionID, target.OnlandVisualTrashAssessmentAreaID, new DateOnly(2018, 5, 1), OnlandVisualTrashAssessmentScoreEnum.A);

            await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID, target.OnlandVisualTrashAssessmentAreaID);
            await OnlandVisualTrashAssessmentAreas.DeleteAreaAsync(_dbContext, source.OnlandVisualTrashAssessmentAreaID);

            var sourceExists = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Any(x => x.OnlandVisualTrashAssessmentAreaID == source.OnlandVisualTrashAssessmentAreaID);
            Assert.IsFalse(sourceExists, "Source area should be deleted.");

            var targetAssessmentCount = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Count(x => x.OnlandVisualTrashAssessmentAreaID == target.OnlandVisualTrashAssessmentAreaID);
            Assert.AreEqual(3, targetAssessmentCount, "All three assessments should now live on the target.");

            var transectBackingCount = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Count(x => x.OnlandVisualTrashAssessmentAreaID == target.OnlandVisualTrashAssessmentAreaID && x.IsTransectBackingAssessment);
            Assert.AreEqual(1, transectBackingCount, "Exactly one assessment on the target should be transect-backing.");
        }
    }
}
