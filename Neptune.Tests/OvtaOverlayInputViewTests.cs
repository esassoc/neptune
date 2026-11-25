using System;
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
    /// against the local DB, rolled back; they need the updated view deployed there.
    /// </summary>
    [TestClass]
    public class OvtaOverlayInputViewTests
    {
        private const int InProgress = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress;
        private const int Complete = (int)OnlandVisualTrashAssessmentStatusEnum.Complete;

        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

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
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private bool IsOverlayInput(int onlandVisualTrashAssessmentAreaID)
        {
            return _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Any(x => x.OVTAID == onlandVisualTrashAssessmentAreaID);
        }

        /// <summary>
        /// An area whose only assessment is one completed progress assessment from the last 5 years
        /// (comfortably inside the window, so the test does not depend on today's date).
        /// </summary>
        private OnlandVisualTrashAssessment? AnySingleRecentProgressAssessment()
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-4));
            return _dbContext.OnlandVisualTrashAssessments.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentAreaID != null
                            && x.IsProgressAssessment
                            && x.OnlandVisualTrashAssessmentStatusID == Complete
                            && x.OnlandVisualTrashAssessmentScoreID != null
                            && x.CompletedDate >= cutoff
                            && x.OnlandVisualTrashAssessmentArea!.OnlandVisualTrashAssessmentBaselineScoreID == null
                            && x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Count == 1)
                .OrderBy(x => x.OnlandVisualTrashAssessmentID)
                .FirstOrDefault();
        }

        [TestMethod]
        public void EveryInput_HasABaselineScoreOrAProgressScore()
        {
            var inputIDs = _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking().Select(x => x.OVTAID).ToList();
            if (!inputIDs.Any()) Assert.Inconclusive("No OVTA Areas are overlay inputs in the local DB.");

            var withBaselineScore = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentBaselineScoreID != null)
                .Select(x => x.OnlandVisualTrashAssessmentAreaID).ToHashSet();
            var withProgressScore = _dbContext.vOnlandVisualTrashAssessmentAreaProgresses.AsNoTracking()
                .Select(x => x.OnlandVisualTrashAssessmentAreaID).ToHashSet();

            Assert.AreEqual(inputIDs.Count, inputIDs.Distinct().Count(), "An area is listed more than once.");
            Assert.IsTrue(inputIDs.All(x => withBaselineScore.Contains(x) || withProgressScore.Contains(x)));
        }

        [TestMethod]
        public void SingleRecentProgressAssessment_IsAnInput()
        {
            var assessment = AnySingleRecentProgressAssessment();
            if (assessment == null) Assert.Inconclusive("No area with a single recent progress assessment in the local DB.");

            Assert.IsTrue(IsOverlayInput(assessment.OnlandVisualTrashAssessmentAreaID!.Value));
        }

        [TestMethod]
        public void SingleProgressAssessment_ReturnedToEdit_IsNotAnInput()
        {
            var assessment = AnySingleRecentProgressAssessment();
            if (assessment == null) Assert.Inconclusive("No area with a single recent progress assessment in the local DB.");

            // returning to edit sets the status back and leaves the completed date
            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.OnlandVisualTrashAssessment SET OnlandVisualTrashAssessmentStatusID = {0} WHERE OnlandVisualTrashAssessmentID = {1}",
                InProgress, assessment.OnlandVisualTrashAssessmentID);

            Assert.IsFalse(IsOverlayInput(assessment.OnlandVisualTrashAssessmentAreaID!.Value));
        }

        [TestMethod]
        public void SingleProgressAssessment_OlderThanFiveYears_IsNotAnInput()
        {
            var assessment = AnySingleRecentProgressAssessment();
            if (assessment == null) Assert.Inconclusive("No area with a single recent progress assessment in the local DB.");

            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.OnlandVisualTrashAssessment SET CompletedDate = {0} WHERE OnlandVisualTrashAssessmentID = {1}",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-6)), assessment.OnlandVisualTrashAssessmentID);

            Assert.IsFalse(IsOverlayInput(assessment.OnlandVisualTrashAssessmentAreaID!.Value));
        }

        [TestMethod]
        public void SingleBaselineAssessment_IsNotAnInput()
        {
            var assessment = _dbContext.OnlandVisualTrashAssessments.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentAreaID != null
                            && !x.IsProgressAssessment
                            && x.OnlandVisualTrashAssessmentStatusID == Complete
                            && x.OnlandVisualTrashAssessmentArea!.OnlandVisualTrashAssessmentBaselineScoreID == null
                            && x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Count == 1)
                .OrderBy(x => x.OnlandVisualTrashAssessmentID)
                .FirstOrDefault();
            if (assessment == null) Assert.Inconclusive("No area with a single baseline assessment in the local DB.");

            Assert.IsFalse(IsOverlayInput(assessment.OnlandVisualTrashAssessmentAreaID!.Value));
        }

        [TestMethod]
        public void AreaWithABaselineScore_IsAnInput_DatedByItsLatestCompletedAssessment()
        {
            var area = _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentBaselineScoreID != null
                            && x.OnlandVisualTrashAssessments.Count(y => y.OnlandVisualTrashAssessmentStatusID == Complete && y.CompletedDate != null) >= 2)
                .OrderBy(x => x.OnlandVisualTrashAssessmentAreaID)
                .Select(x => new
                {
                    x.OnlandVisualTrashAssessmentAreaID,
                    LatestCompletedDate = x.OnlandVisualTrashAssessments
                        .Where(y => y.OnlandVisualTrashAssessmentStatusID == Complete && y.CompletedDate != null)
                        .Max(y => y.CompletedDate),
                })
                .FirstOrDefault();
            if (area == null) Assert.Inconclusive("No area with a Baseline Score in the local DB.");

            var input = _dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking()
                .Where(x => x.OVTAID == area.OnlandVisualTrashAssessmentAreaID)
                .Select(x => x.AssessDate)
                .ToList();

            Assert.AreEqual(1, input.Count);
            Assert.AreEqual(area.LatestCompletedDate!.Value.ToDateTime(TimeOnly.MinValue), input.Single());
        }
    }
}
