using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1126: integration tests (real local DB, rolled back) for dbo.vLandUseBlockOVTAAssessmentCoverage, which feeds
    /// the OVTA-Based Results "Acres of Land Use Type by # of Assessments" view. The fixtures are 100 m squares far outside
    /// Orange County (SRID 2771, metres), so no real OVTA Area touches them. Requires the view to be deployed locally.
    /// </summary>
    [TestClass]
    public class LandUseBlockOVTAAssessmentCoverageTests
    {
        private const int CompleteStatusID = 2;
        private const int InProgressStatusID = 1;
        private const double Tolerance = 0.01;

        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _otherJurisdictionID;
        private int _personID;
        private int _scoreID;

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

            var jurisdictionIDs = ScalarInts("SELECT TOP 2 StormwaterJurisdictionID AS Value FROM dbo.StormwaterJurisdiction ORDER BY StormwaterJurisdictionID");
            var personID = ScalarInts("SELECT TOP 1 PersonID AS Value FROM dbo.Person ORDER BY PersonID").FirstOrDefault();
            var scoreID = ScalarInts("SELECT TOP 1 OnlandVisualTrashAssessmentScoreID AS Value FROM dbo.OnlandVisualTrashAssessmentScore ORDER BY OnlandVisualTrashAssessmentScoreID").FirstOrDefault();
            if (jurisdictionIDs.Length < 2 || personID == 0 || scoreID == 0)
            {
                Assert.Inconclusive("The local database needs 2 jurisdictions, a person and an OVTA score.");
            }

            _jurisdictionID = jurisdictionIDs[0];
            _otherJurisdictionID = jurisdictionIDs[1];
            _personID = personID;
            _scoreID = scoreID;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        [TestMethod]
        public void OverlappingAreas_CountOnceAtHighestTier()
        {
            // Block: x 0-100. Area P (1 completed): x 0-60. Area Q (3 completed): x 40-100, overlapping P on x 40-60.
            // Area R (2 completed): x 0-20, y 0-50, inside P.
            var blockID = InsertLandUseBlock(Square(0, 0, 100, 100));
            InsertArea(Square(0, 0, 60, 100), completed: 1);
            InsertArea(Square(40, 0, 100, 100), completed: 3);
            InsertArea(Square(0, 0, 20, 50), completed: 2);

            var row = GetCoverage(blockID);

            Assert.AreEqual(10000, row.AreaAssessedOnceOrMore, Tolerance, ">= 1 is the union of all three areas, the whole block.");
            Assert.AreEqual(7000, row.AreaAssessedTwiceOrMore, Tolerance, ">= 2 is Q (6000) plus R (1000).");
            Assert.AreEqual(6000, row.AreaAssessedThreeOrMore, Tolerance, ">= 3 is Q only; the P/Q overlap counts once, at 3.");
        }

        [TestMethod]
        public void OnlyCompletedAssessmentsInTheSameJurisdictionCount()
        {
            var blockID = InsertLandUseBlock(Square(0, 0, 100, 100));
            // 1 completed + 2 in progress: still assessed once, not three times.
            var areaID = InsertArea(Square(0, 0, 50, 100), completed: 1);
            InsertAssessment(areaID, InProgressStatusID);
            InsertAssessment(areaID, InProgressStatusID);
            // Another jurisdiction's area over the whole block is ignored.
            InsertArea(Square(0, 0, 100, 100), completed: 3, jurisdictionID: _otherJurisdictionID);

            var row = GetCoverage(blockID);

            Assert.AreEqual(5000, row.AreaAssessedOnceOrMore, Tolerance);
            Assert.AreEqual(0, row.AreaAssessedTwiceOrMore, Tolerance);
            Assert.AreEqual(0, row.AreaAssessedThreeOrMore, Tolerance);
        }

        [TestMethod]
        public void BlockWithNoIntersectingAreas_ReturnsZerosNotNulls()
        {
            var blockID = InsertLandUseBlock(Square(0, 0, 100, 100));
            InsertArea(Square(500, 500, 600, 600), completed: 3);

            var row = GetCoverage(blockID);

            Assert.AreEqual(0, row.AreaAssessedOnceOrMore);
            Assert.AreEqual(0, row.AreaAssessedTwiceOrMore);
            Assert.AreEqual(0, row.AreaAssessedThreeOrMore);
        }

        [TestMethod]
        public void InvalidBlockGeometry_IsRepairedNotSkippedAndDoesNotThrow()
        {
            // Self-intersecting bowtie: two 2,500 m² triangles once repaired with MakeValid.
            var blockID = InsertLandUseBlock(Offset("POLYGON(({0} {1}, {2} {3}, {2} {1}, {0} {3}, {0} {1}))", 0, 0, 100, 100));
            Assert.AreEqual(0, ScalarInts($"SELECT CAST(LandUseBlockGeometry.STIsValid() AS int) AS Value FROM dbo.LandUseBlock WHERE LandUseBlockID = {blockID}").Single(),
                "Fixture should be invalid geometry.");
            InsertArea(Square(0, 0, 100, 100), completed: 3);

            var row = GetCoverage(blockID);

            Assert.AreEqual(5000, row.AreaAssessedOnceOrMore, Tolerance);
            Assert.AreEqual(5000, row.AreaAssessedThreeOrMore, Tolerance);
        }

        // Fixtures sit at (FixtureX, FixtureY) + the given local metres, far outside Orange County.
        private const double FixtureX = 100000;
        private const double FixtureY = 100000;

        private static string Square(double minX, double minY, double maxX, double maxY) =>
            Offset("POLYGON(({0} {1}, {2} {1}, {2} {3}, {0} {3}, {0} {1}))", minX, minY, maxX, maxY);

        private static string Offset(string wktFormat, double minX, double minY, double maxX, double maxY) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, wktFormat, FixtureX + minX, FixtureY + minY, FixtureX + maxX, FixtureY + maxY);

        private int InsertLandUseBlock(string wkt) =>
            ScalarInts($@"
                INSERT INTO dbo.LandUseBlock (LandUseBlockGeometry, StormwaterJurisdictionID, PermitTypeID)
                OUTPUT inserted.LandUseBlockID AS Value
                VALUES (geometry::STGeomFromText('{wkt}', 2771), {_jurisdictionID}, 1)").Single();

        private int InsertArea(string wkt, int completed, int? jurisdictionID = null)
        {
            var areaID = ScalarInts($@"
                INSERT INTO dbo.OnlandVisualTrashAssessmentArea (OnlandVisualTrashAssessmentAreaName, StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaGeometry)
                OUTPUT inserted.OnlandVisualTrashAssessmentAreaID AS Value
                VALUES ('NPT-1126 test {Guid.NewGuid():N}', {jurisdictionID ?? _jurisdictionID}, geometry::STGeomFromText('{wkt}', 2771))").Single();
            for (var i = 0; i < completed; i++)
            {
                InsertAssessment(areaID, CompleteStatusID, jurisdictionID);
            }
            return areaID;
        }

        private void InsertAssessment(int areaID, int statusID, int? jurisdictionID = null)
        {
            var isComplete = statusID == CompleteStatusID;
            _dbContext.Database.ExecuteSqlRaw($@"
                INSERT INTO dbo.OnlandVisualTrashAssessment (CreatedByPersonID, CreatedDate, StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaID,
                    OnlandVisualTrashAssessmentStatusID, CompletedDate, OnlandVisualTrashAssessmentScoreID, IsTransectBackingAssessment, IsProgressAssessment)
                VALUES ({_personID}, GETDATE(), {jurisdictionID ?? _jurisdictionID}, {areaID},
                    {statusID}, {(isComplete ? "GETDATE()" : "NULL")}, {(isComplete ? _scoreID.ToString() : "NULL")}, 0, 0)");
        }

        private vLandUseBlockOVTAAssessmentCoverage GetCoverage(int landUseBlockID) =>
            _dbContext.vLandUseBlockOVTAAssessmentCoverages.AsNoTracking().Single(x => x.LandUseBlockID == landUseBlockID);

        private int[] ScalarInts(string sql) => _dbContext.Database.SqlQueryRaw<int>(sql).AsEnumerable().ToArray();
    }
}
