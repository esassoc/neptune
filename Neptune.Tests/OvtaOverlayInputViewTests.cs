using System;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1144: which OVTA Areas go into the Trash Generating Unit overlay
    /// (vPyQgisOnlandVisualTrashAssessmentAreaDated). An area is an input when it has a Baseline Score
    /// or a Progress Score; a single progress assessment in the last 5 years is enough, a single
    /// baseline assessment is not, and an assessment returned to edit does not count. Integration tests
    /// against the local DB; each creates its own area and assessments inside a transaction that is
    /// rolled back. They need the updated view deployed there.
    /// </summary>
    [TestClass]
    public class OvtaOverlayInputViewTests
    {
        private const int InProgress = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress;
        private const int Complete = (int)OnlandVisualTrashAssessmentStatusEnum.Complete;

        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _personID;
        private int _scoreID;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                TestDatabase.ConnectionString,
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

            _jurisdictionID = ScalarInts("SELECT TOP 1 StormwaterJurisdictionID AS Value FROM dbo.StormwaterJurisdiction ORDER BY StormwaterJurisdictionID").FirstOrDefault();
            _personID = ScalarInts("SELECT TOP 1 PersonID AS Value FROM dbo.Person ORDER BY PersonID").FirstOrDefault();
            _scoreID = ScalarInts("SELECT TOP 1 OnlandVisualTrashAssessmentScoreID AS Value FROM dbo.OnlandVisualTrashAssessmentScore ORDER BY OnlandVisualTrashAssessmentScoreID").FirstOrDefault();
            if (_jurisdictionID == 0 || _personID == 0 || _scoreID == 0)
            {
                Assert.Inconclusive("The local database needs a jurisdiction, a person and an OVTA score.");
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        [TestMethod]
        public void SingleRecentProgressAssessment_IsAnInput()
        {
            var areaID = InsertArea();
            InsertAssessment(areaID, Complete, isProgress: true, completedDate: YearsAgo(1));

            Assert.IsTrue(IsOverlayInput(areaID));
        }

        [TestMethod]
        public void SingleProgressAssessment_ReturnedToEdit_IsNotAnInput()
        {
            // returning to edit sets the status back and leaves the completed date and the score
            var areaID = InsertArea();
            InsertAssessment(areaID, InProgress, isProgress: true, completedDate: YearsAgo(1));

            Assert.IsFalse(IsOverlayInput(areaID));
        }

        [TestMethod]
        public void SingleProgressAssessment_OlderThanFiveYears_IsNotAnInput()
        {
            var areaID = InsertArea();
            InsertAssessment(areaID, Complete, isProgress: true, completedDate: YearsAgo(6));

            Assert.IsFalse(IsOverlayInput(areaID));
        }

        [TestMethod]
        public void SingleBaselineAssessment_IsNotAnInput()
        {
            var areaID = InsertArea();
            InsertAssessment(areaID, Complete, isProgress: false, completedDate: YearsAgo(1));

            Assert.IsFalse(IsOverlayInput(areaID));
        }

        [TestMethod]
        public void TwoCompletedAssessments_WithNoScore_IsNotAnInput()
        {
            // the old gate took any area with two completed assessments; one baseline plus one lapsed
            // progress assessment gives neither a Baseline Score nor a Progress Score
            var areaID = InsertArea();
            InsertAssessment(areaID, Complete, isProgress: false, completedDate: YearsAgo(7));
            InsertAssessment(areaID, Complete, isProgress: true, completedDate: YearsAgo(6));

            Assert.IsFalse(IsOverlayInput(areaID));
        }

        [TestMethod]
        public void AreaWithABaselineScore_IsAnInput_DatedByItsLatestCompletedAssessment()
        {
            // the Baseline Score is stored on the area (the application sets it once there are two
            // completed baseline assessments), so the fixture stores it too
            var areaID = InsertArea(hasBaselineScore: true);
            var latestCompleted = YearsAgo(2);
            InsertAssessment(areaID, Complete, isProgress: false, completedDate: YearsAgo(3));
            InsertAssessment(areaID, Complete, isProgress: false, completedDate: latestCompleted);
            // returned to edit with a later date: must not date the area
            InsertAssessment(areaID, InProgress, isProgress: false, completedDate: YearsAgo(1));

            var assessDates = _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking()
                .Where(x => x.OVTAID == areaID)
                .Select(x => x.AssessDate)
                .ToList();

            Assert.AreEqual(1, assessDates.Count);
            Assert.AreEqual(latestCompleted.ToDateTime(TimeOnly.MinValue), assessDates.Single());
        }

        [TestMethod]
        public void EveryInput_IsListedOnce_AndHasABaselineScoreOrAProgressScore()
        {
            var areaID = InsertArea();
            InsertAssessment(areaID, Complete, isProgress: true, completedDate: YearsAgo(1));
            InsertAssessment(areaID, Complete, isProgress: true, completedDate: YearsAgo(2));

            var inputIDs = _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Select(x => x.OVTAID).ToList();
            var withBaselineScore = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentBaselineScoreID != null)
                .Select(x => x.OnlandVisualTrashAssessmentAreaID).ToHashSet();
            var withProgressScore = _dbContext.vOnlandVisualTrashAssessmentAreaProgresses.AsNoTracking()
                .Select(x => x.OnlandVisualTrashAssessmentAreaID).ToHashSet();

            Assert.AreEqual(1, inputIDs.Count(x => x == areaID));
            Assert.AreEqual(inputIDs.Count, inputIDs.Distinct().Count(), "An area is listed more than once.");
            Assert.IsTrue(inputIDs.All(x => withBaselineScore.Contains(x) || withProgressScore.Contains(x)));
        }

        private bool IsOverlayInput(int onlandVisualTrashAssessmentAreaID)
        {
            return _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Any(x => x.OVTAID == onlandVisualTrashAssessmentAreaID);
        }

        private static DateOnly YearsAgo(int years) => DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-years);

        // A 100 m square far outside Orange County.
        private const string FixtureGeometry = "POLYGON((100000 100000, 100100 100000, 100100 100100, 100000 100100, 100000 100000))";

        private int InsertArea(bool hasBaselineScore = false) =>
            ScalarInts($@"
                INSERT INTO dbo.OnlandVisualTrashAssessmentArea (OnlandVisualTrashAssessmentAreaName, StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaGeometry, OnlandVisualTrashAssessmentBaselineScoreID)
                OUTPUT inserted.OnlandVisualTrashAssessmentAreaID AS Value
                VALUES ('NPT-1144 test {Guid.NewGuid():N}', {_jurisdictionID}, geometry::STGeomFromText('{FixtureGeometry}', 2771), {(hasBaselineScore ? _scoreID.ToString() : "NULL")})").Single();

        /// <summary>
        /// An assessment with a completed date and a score whatever its status, which is what an
        /// assessment returned to edit looks like.
        /// </summary>
        private void InsertAssessment(int areaID, int statusID, bool isProgress, DateOnly completedDate)
        {
            _dbContext.Database.ExecuteSqlRaw($@"
                INSERT INTO dbo.OnlandVisualTrashAssessment (CreatedByPersonID, CreatedDate, StormwaterJurisdictionID, OnlandVisualTrashAssessmentAreaID,
                    OnlandVisualTrashAssessmentStatusID, CompletedDate, OnlandVisualTrashAssessmentScoreID, IsTransectBackingAssessment, IsProgressAssessment)
                VALUES ({_personID}, GETDATE(), {_jurisdictionID}, {areaID},
                    {statusID}, '{completedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}', {_scoreID}, 0, {(isProgress ? 1 : 0)})");
        }

        private int[] ScalarInts(string sql) => _dbContext.Database.SqlQueryRaw<int>(sql).AsEnumerable().ToArray();
    }
}
