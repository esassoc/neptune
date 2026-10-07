using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Eval;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1132: the extraction eval's scorer. It has to apply extracted values the way the review
    /// wizard does (wqmp-review.component.ts makeField). If it's more lenient than the wizard, the eval
    /// overstates accuracy; if it's stricter, it penalizes values users would actually get.
    /// </summary>
    [TestClass]
    public class ExtractionEvalScorerTests
    {
        private static readonly Lookups Lookups = new()
        {
            PriorityIDByName = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase) { ["High"] = 1, ["Low"] = 2 },
        };

        private static FieldScore Score(string key, WaterQualityManagementPlan wqmp, string? extracted) =>
            EvalFields.All.Single(f => f.Key == key).Score(new GroundTruth { Wqmp = wqmp }, extracted, Lookups);

        [TestMethod]
        public void Lookup_MatchesOptionLabelCaseInsensitively()
        {
            var score = Score("WaterQualityManagementPlanPriority", new WaterQualityManagementPlan { WaterQualityManagementPlanPriorityID = 1 }, "high");
            Assert.AreEqual(FieldOutcome.Correct, score.Outcome);
        }

        [TestMethod]
        public void Lookup_LabelTheWizardCantMap_IsMissedAndFlaggedUnmapped()
        {
            // "High Priority" isn't an option label, so the wizard leaves the dropdown blank.
            var score = Score("WaterQualityManagementPlanPriority", new WaterQualityManagementPlan { WaterQualityManagementPlanPriorityID = 1 }, "High Priority");
            Assert.AreEqual(FieldOutcome.Missed, score.Outcome);
            Assert.IsTrue(score.Unmapped);
        }

        [TestMethod]
        public void Date_ParsesWrittenOutDates()
        {
            var score = Score("ApprovalDate", new WaterQualityManagementPlan { ApprovalDate = new System.DateTime(2019, 6, 5) }, "June 5, 2019");
            Assert.AreEqual(FieldOutcome.Correct, score.Outcome);
        }

        [TestMethod]
        public void Acres_RoundsToTwoDecimalsAndIgnoresUnits()
        {
            var score = Score("RecordedWQMPAreaInAcres", new WaterQualityManagementPlan { RecordedWQMPAreaInAcres = 1.23m }, "1.234 acres");
            Assert.AreEqual(FieldOutcome.Correct, score.Outcome);
        }

        [TestMethod]
        public void Acres_ComparesAtThePrecisionTheRecordWasEnteredWith()
        {
            var wqmp = new WaterQualityManagementPlan { RecordedWQMPAreaInAcres = 2.4m };
            Assert.AreEqual(FieldOutcome.Correct, Score("RecordedWQMPAreaInAcres", wqmp, "2.44").Outcome);
            Assert.AreEqual(FieldOutcome.Wrong, Score("RecordedWQMPAreaInAcres", wqmp, "2.6").Outcome);
        }

        [TestMethod]
        public void Phone_ComparesDigitsOnly()
        {
            var score = Score("MaintenanceContactPhone", new WaterQualityManagementPlan { MaintenanceContactPhone = "(714) 555-1212" }, "714.555.1212");
            Assert.AreEqual(FieldOutcome.Correct, score.Outcome);
        }

        [TestMethod]
        public void State_AcceptsFullNameOrCode()
        {
            var wqmp = new WaterQualityManagementPlan { MaintenanceContactState = "CA" };
            Assert.AreEqual(FieldOutcome.Correct, Score("MaintenanceContactState", wqmp, "California").Outcome);
            Assert.AreEqual(FieldOutcome.Correct, Score("MaintenanceContactState", wqmp, "ca").Outcome);
        }

        [TestMethod]
        public void Text_IgnoresCaseAndPunctuation()
        {
            var score = Score("MaintenanceContactAddress1", new WaterQualityManagementPlan { MaintenanceContactAddress1 = "123 Main St." }, "123 MAIN ST");
            Assert.AreEqual(FieldOutcome.Correct, score.Outcome);
        }

        [TestMethod]
        public void Text_NearMissIsClose()
        {
            var score = Score("MaintenanceContactAddress1", new WaterQualityManagementPlan { MaintenanceContactAddress1 = "123 Main St" }, "123 Main St Suite 400");
            Assert.AreEqual(FieldOutcome.Close, score.Outcome);
        }

        [TestMethod]
        public void Text_DifferentValueIsWrong()
        {
            var score = Score("MaintenanceContactCity", new WaterQualityManagementPlan { MaintenanceContactCity = "Irvine" }, "Anaheim");
            Assert.AreEqual(FieldOutcome.Wrong, score.Outcome);
        }

        [TestMethod]
        public void ValueOnlyInExtraction_IsExtraNotWrong()
        {
            // The hand-entered data may simply be incomplete, so this isn't counted against accuracy.
            var score = Score("MaintenanceContactCity", new WaterQualityManagementPlan(), "Irvine");
            Assert.AreEqual(FieldOutcome.Extra, score.Outcome);
        }

        [TestMethod]
        public void NothingExtracted_IsMissed()
        {
            var score = Score("MaintenanceContactCity", new WaterQualityManagementPlan { MaintenanceContactCity = "Irvine" }, null);
            Assert.AreEqual(FieldOutcome.Missed, score.Outcome);
        }

        [TestMethod]
        public void Pricing_CountsCacheWritesAtOneAndAQuarterTimesInput()
        {
            // Sonnet 4.6: $3/M input, $15/M output, $0.30/M cache read; cache write = 1.25 x input.
            var cost = Pricing.Cost("claude-sonnet-4-6",
            [
                new WqmpExtractionCallUsageDto { InputTokens = 1_000_000, CacheCreationInputTokens = 1_000_000, CacheReadInputTokens = 1_000_000, OutputTokens = 1_000_000 },
            ]);
            Assert.AreEqual(3.00m + 3.75m + 0.30m + 15.00m, cost);
        }
    }
}
