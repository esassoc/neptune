using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Helpers;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using static Neptune.API.Helpers.OVTAResultsTableBuilder;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1126: the OVTA-Based Results table has one row per land use type. PLU rows are keyed by Priority Land Use
    /// Type and ALU rows by Land Use for TGR. Also keeps the NPT-1128 guarantee that PLU and ALU acreage never bleed
    /// into each other.
    /// </summary>
    [TestClass]
    public class OVTAResultsTableBuilderTests
    {
        private const int Commercial = (int)PriorityLandUseTypeEnum.Commercial;
        private const int Industrial = (int)PriorityLandUseTypeEnum.Industrial;
        private const int ALU = (int)PriorityLandUseTypeEnum.ALU;
        private const int A = (int)OnlandVisualTrashAssessmentScoreEnum.A;
        private const int B = (int)OnlandVisualTrashAssessmentScoreEnum.B;
        private const int C = (int)OnlandVisualTrashAssessmentScoreEnum.C;
        private const int D = (int)OnlandVisualTrashAssessmentScoreEnum.D;

        private static TrashGeneratingUnitAcres Tgu(int? priorityLandUseTypeID, string landUseForTGR, double acres,
            int? baseline = null, int? progress = null, int completedAssessments = 0)
        {
            return new TrashGeneratingUnitAcres(priorityLandUseTypeID, landUseForTGR, baseline, progress, completedAssessments, acres);
        }

        private static OVTAResultsLandUseRowDtoLookup Build(IEnumerable<LandUseAcres> landUse, IEnumerable<TrashGeneratingUnitAcres> tgus)
        {
            return new OVTAResultsLandUseRowDtoLookup(BuildRows(landUse, tgus));
        }

        [TestMethod]
        public void PriorityAndAlternate_SameScore_DoNotBleedIntoEachOther()
        {
            var rows = Build(
                new[] { new LandUseAcres(Commercial, "Commercial", 20), new LandUseAcres(ALU, "Commercial", 10) },
                new[] { Tgu(Commercial, "Commercial", 10, baseline: A), Tgu(ALU, "Commercial", 3, baseline: A) });

            Assert.AreEqual(10, rows.Priority("Commercial").BaselineAcresA, 1e-9);
            Assert.AreEqual(3, rows.Alternate("Commercial").BaselineAcresA, 1e-9);
        }

        [TestMethod]
        public void AlternateLandUse_IsSplitByLandUseForTGR_AndPriorityByType()
        {
            var rows = Build(
                new[]
                {
                    new LandUseAcres(Commercial, "Commercial", 5),
                    new LandUseAcres(Commercial, "Other", 1), // PLU rows ignore Land Use for TGR
                    new LandUseAcres(ALU, "Residential", 50),
                    new LandUseAcres(ALU, "Urban Park", 7),
                },
                new TrashGeneratingUnitAcres[0]);

            Assert.AreEqual(3, rows.All.Count);
            Assert.AreEqual(6, rows.Priority("Commercial").TotalMappedAcres, 1e-9);
            Assert.AreEqual(50, rows.Alternate("Residential").TotalMappedAcres, 1e-9);
            Assert.AreEqual(7, rows.Alternate("Urban Park").TotalMappedAcres, 1e-9);
        }

        [TestMethod]
        public void PriorityRow_UsesPriorityLandUseTypeDisplayName()
        {
            var rows = Build(new[] { new LandUseAcres((int)PriorityLandUseTypeEnum.CommercialRetail, null, 1) }, new TrashGeneratingUnitAcres[0]);

            Assert.AreEqual("Commercial-Retail", rows.All.Single().LandUseTypeName);
        }

        [TestMethod]
        public void LandUseTypeWithNoAssessedArea_StillGetsAZeroRow()
        {
            var rows = Build(new[] { new LandUseAcres(Industrial, null, 12) }, new TrashGeneratingUnitAcres[0]);

            var industrial = rows.Priority("Industrial");
            Assert.AreEqual(12, industrial.TotalMappedAcres, 1e-9);
            Assert.AreEqual(0, industrial.BaselineAcresA + industrial.BaselineAcresB + industrial.BaselineAcresC + industrial.BaselineAcresD);
            Assert.AreEqual(12, industrial.NotYetAssessedAcres, 1e-9);
        }

        [TestMethod]
        public void BaselineAndProgressScores_AreBucketedIndependently()
        {
            var rows = Build(
                new[] { new LandUseAcres(Commercial, null, 100) },
                new[]
                {
                    Tgu(Commercial, null, 10, baseline: A, progress: B, completedAssessments: 3),
                    Tgu(Commercial, null, 4, baseline: C, completedAssessments: 2),
                    Tgu(Commercial, null, 6, progress: D, completedAssessments: 1),
                });

            var row = rows.Priority("Commercial");
            Assert.AreEqual(10, row.BaselineAcresA, 1e-9);
            Assert.AreEqual(4, row.BaselineAcresC, 1e-9);
            Assert.AreEqual(0, row.BaselineAcresB + row.BaselineAcresD);
            Assert.AreEqual(10, row.ProgressAcresB, 1e-9);
            Assert.AreEqual(6, row.ProgressAcresD, 1e-9);
            Assert.AreEqual(0, row.ProgressAcresA + row.ProgressAcresC);
        }

        [TestMethod]
        public void AssessmentCountBuckets_SumToTotalMappedLandUse()
        {
            var rows = Build(
                new[] { new LandUseAcres(ALU, "Residential", 100) },
                new[]
                {
                    Tgu(ALU, "Residential", 40, completedAssessments: 0),
                    Tgu(ALU, "Residential", 11, completedAssessments: 1),
                    Tgu(ALU, "Residential", 12, completedAssessments: 2),
                    Tgu(ALU, "Residential", 13, completedAssessments: 3),
                    Tgu(ALU, "Residential", 14, completedAssessments: 7),
                });

            var row = rows.Alternate("Residential");
            Assert.AreEqual(11, row.AssessedOnceAcres, 1e-9);
            Assert.AreEqual(12, row.AssessedTwiceAcres, 1e-9);
            Assert.AreEqual(27, row.AssessedThreeOrMoreAcres, 1e-9);
            Assert.AreEqual(50, row.NotYetAssessedAcres, 1e-9);
            Assert.AreEqual(row.TotalMappedAcres, row.NotYetAssessedAcres + AssessedAcres(row), 1e-9);
        }

        [TestMethod]
        public void AssessedAreaOverTotal_NotYetAssessedClampsToZero()
        {
            var rows = Build(
                new[] { new LandUseAcres(Commercial, null, 10) },
                new[] { Tgu(Commercial, null, 10.5, baseline: A, completedAssessments: 2) });

            Assert.AreEqual(0, rows.Priority("Commercial").NotYetAssessedAcres);
        }

        [TestMethod]
        public void BlankLandUseForTGR_OnAlternateLandUse_IsUnspecified()
        {
            var rows = Build(
                new[] { new LandUseAcres(ALU, null, 2), new LandUseAcres(ALU, "  ", 3), new LandUseAcres(ALU, " Other ", 1) },
                new TrashGeneratingUnitAcres[0]);

            Assert.AreEqual(5, rows.Alternate(UnspecifiedLandUseTypeName).TotalMappedAcres, 1e-9);
            Assert.AreEqual(1, rows.Alternate("Other").TotalMappedAcres, 1e-9);
        }

        [TestMethod]
        public void AlternateLandUseForTGR_DifferingOnlyInCase_SharesOneRow()
        {
            // The totals are grouped by a case-insensitive SQL collation, so the TGU spellings must merge into the same row.
            var rows = Build(
                new[] { new LandUseAcres(ALU, "Residential", 100) },
                new[] { Tgu(ALU, "RESIDENTIAL", 30, baseline: A, completedAssessments: 2), Tgu(ALU, "residential ", 10, completedAssessments: 1) });

            var residential = rows.Alternate("Residential");
            Assert.AreEqual(1, rows.All.Count);
            Assert.AreEqual(30, residential.BaselineAcresA, 1e-9);
            Assert.AreEqual(60, residential.NotYetAssessedAcres, 1e-9);
        }

        [TestMethod]
        public void Rows_ArePriorityFirst_ThenLargestTotalFirst()
        {
            var rows = BuildRows(
                new[]
                {
                    new LandUseAcres(ALU, "Residential", 500),
                    new LandUseAcres(Industrial, null, 5),
                    new LandUseAcres(Commercial, null, 50),
                    new LandUseAcres(ALU, "Other", 80),
                },
                new TrashGeneratingUnitAcres[0]);

            CollectionAssert.AreEqual(new[] { "Commercial", "Industrial", "Residential", "Other" }, rows.Select(x => x.LandUseTypeName).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, rows.Select(x => x.SortOrder).ToArray());
        }

        private class OVTAResultsLandUseRowDtoLookup(List<OVTAResultsLandUseRowDto> rows)
        {
            public List<OVTAResultsLandUseRowDto> All { get; } = rows;
            public OVTAResultsLandUseRowDto Priority(string name) => All.Single(x => x.IsPriorityLandUse && x.LandUseTypeName == name);
            public OVTAResultsLandUseRowDto Alternate(string name) => All.Single(x => !x.IsPriorityLandUse && x.LandUseTypeName == name);
        }
    }
}
