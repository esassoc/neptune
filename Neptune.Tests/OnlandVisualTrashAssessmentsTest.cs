/*-----------------------------------------------------------------------
<copyright file="OnlandVisualTrashAssessmentsTest.cs" company="Sitka Technology Group">
Copyright (c) Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1065 progress-score rule: a progress score requires at least 1 completed
    /// progress OVTA within the last 5 years (previously 2 within 4 years). The score is the rounded
    /// average of the top 3 most recent qualifying assessments.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentsTest
    {
        // NumericValue mapping from OnlandVisualTrashAssessmentScore.Binding.cs: A=4, B=3, C=2, D=1
        private static OnlandVisualTrashAssessment BuildAssessment(
            OnlandVisualTrashAssessmentScore score,
            DateOnly completedDate,
            bool isProgressAssessment = true,
            bool isComplete = true)
        {
            return new OnlandVisualTrashAssessment
            {
                OnlandVisualTrashAssessmentStatusID = isComplete
                    ? OnlandVisualTrashAssessmentStatus.Complete.OnlandVisualTrashAssessmentStatusID
                    : OnlandVisualTrashAssessmentStatus.InProgress.OnlandVisualTrashAssessmentStatusID,
                IsProgressAssessment = isProgressAssessment,
                CompletedDate = completedDate,
                OnlandVisualTrashAssessmentScoreID = score.OnlandVisualTrashAssessmentScoreID,
            };
        }

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

        [TestMethod]
        public void SingleCompletedProgressAssessmentWithinFiveYearsProducesScore()
        {
            // New rule: a single completed progress OVTA within 5 years is now enough (was 2 within 4 years).
            var assessments = new List<OnlandVisualTrashAssessment>
            {
                BuildAssessment(OnlandVisualTrashAssessmentScore.B, Today),
            };

            var result = OnlandVisualTrashAssessments.CalculateProgressScore(assessments);

            Assert.IsNotNull(result, "A single completed progress assessment within 5 years should yield a score.");
            Assert.AreEqual(OnlandVisualTrashAssessmentScore.B, result);
        }

        [TestMethod]
        public void NoCompletedProgressAssessmentsProducesNull()
        {
            var result = OnlandVisualTrashAssessments.CalculateProgressScore(new List<OnlandVisualTrashAssessment>());

            Assert.IsNull(result, "With zero qualifying assessments there is no progress score.");
        }

        [TestMethod]
        public void ProgressAssessmentOlderThanFiveYearsIsExcluded()
        {
            // Just outside the 5-year window -> filtered out -> no score.
            var assessments = new List<OnlandVisualTrashAssessment>
            {
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today.AddYears(-6)),
            };

            var result = OnlandVisualTrashAssessments.CalculateProgressScore(assessments);

            Assert.IsNull(result, "A progress assessment completed more than 5 years ago must not count.");
        }

        [TestMethod]
        public void NonProgressAndIncompleteAssessmentsAreExcluded()
        {
            var assessments = new List<OnlandVisualTrashAssessment>
            {
                // Completed but a baseline (non-progress) assessment -> excluded.
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today, isProgressAssessment: false),
                // Progress but not completed -> excluded.
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today, isComplete: false),
            };

            var result = OnlandVisualTrashAssessments.CalculateProgressScore(assessments);

            Assert.IsNull(result, "Only completed progress assessments should contribute to the progress score.");
        }

        [TestMethod]
        public void OnlyTopThreeMostRecentAssessmentsAreAveraged()
        {
            // 3 most recent are A (numeric 4); the oldest is D (numeric 1).
            // Top-3 average = 4 -> A. Averaging all 4 would be (4+4+4+1)/4 = 3.25 -> round 3 -> B.
            // Asserting A proves only the top 3 most recent are used.
            var assessments = new List<OnlandVisualTrashAssessment>
            {
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today),
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today.AddDays(-10)),
                BuildAssessment(OnlandVisualTrashAssessmentScore.A, Today.AddDays(-20)),
                BuildAssessment(OnlandVisualTrashAssessmentScore.D, Today.AddDays(-30)),
            };

            var result = OnlandVisualTrashAssessments.CalculateProgressScore(assessments);

            Assert.AreEqual(OnlandVisualTrashAssessmentScore.A, result,
                "Only the top 3 most recent assessments should be averaged.");
        }
    }
}
