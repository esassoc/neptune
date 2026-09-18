using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Helpers;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1128: the OVTA-Based Results ALU row was reporting PLU acreage because the Alternate and Priority
    /// acreage helpers both filtered IsPLU(). These tests pin down that PLU and ALU are partitioned by land use
    /// and by OVTA baseline score.
    /// </summary>
    [TestClass]
    public class TrashGeneratingUnitHelperOVTAScoreAcreageTests
    {
        // 1 acre = 4046.856 m^2. A square with side sqrt(acres * 4046.856) has the requested acreage.
        private static Polygon SquareOfAcres(double acres)
        {
            var side = System.Math.Sqrt(acres / 0.000247105);
            return new Polygon(new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(side, 0),
                new Coordinate(side, side),
                new Coordinate(0, side),
                new Coordinate(0, 0),
            }));
        }

        private static TrashGeneratingUnit Tgu(double acres, int? priorityLandUseTypeID, OnlandVisualTrashAssessmentScoreEnum? baselineScore)
        {
            return new TrashGeneratingUnit
            {
                TrashGeneratingUnitGeometry = SquareOfAcres(acres),
                LandUseBlock = priorityLandUseTypeID.HasValue
                    ? new LandUseBlock { PriorityLandUseTypeID = priorityLandUseTypeID.Value }
                    : null,
                OnlandVisualTrashAssessmentArea = baselineScore.HasValue
                    ? new OnlandVisualTrashAssessmentArea { OnlandVisualTrashAssessmentBaselineScoreID = (int)baselineScore.Value }
                    : null,
            };
        }

        private const int PLU = (int)PriorityLandUseTypeEnum.Commercial;
        private const int ALU = (int)PriorityLandUseTypeEnum.ALU;

        [TestMethod]
        public void MixedLandUse_SameScore_PriorityAndAlternateReportDifferentAcreage()
        {
            var tgus = new List<TrashGeneratingUnit>
            {
                Tgu(10, PLU, OnlandVisualTrashAssessmentScoreEnum.A),
                Tgu(3, ALU, OnlandVisualTrashAssessmentScoreEnum.A),
            };

            Assert.AreEqual(10, tgus.PriorityOVTAScoreAAcreage());
            Assert.AreEqual(3, tgus.AlternateOVTAScoreAAcreage());
        }

        [TestMethod]
        public void AllScores_PartitionedByLandUseAndScore()
        {
            var tgus = new List<TrashGeneratingUnit>
            {
                Tgu(1, PLU, OnlandVisualTrashAssessmentScoreEnum.A),
                Tgu(2, PLU, OnlandVisualTrashAssessmentScoreEnum.B),
                Tgu(3, PLU, OnlandVisualTrashAssessmentScoreEnum.C),
                Tgu(4, PLU, OnlandVisualTrashAssessmentScoreEnum.D),
                Tgu(5, ALU, OnlandVisualTrashAssessmentScoreEnum.A),
                Tgu(6, ALU, OnlandVisualTrashAssessmentScoreEnum.B),
                Tgu(7, ALU, OnlandVisualTrashAssessmentScoreEnum.C),
                Tgu(8, ALU, OnlandVisualTrashAssessmentScoreEnum.D),
            };

            Assert.AreEqual(1, tgus.PriorityOVTAScoreAAcreage());
            Assert.AreEqual(2, tgus.PriorityOVTAScoreBAcreage());
            Assert.AreEqual(3, tgus.PriorityOVTAScoreCAcreage());
            Assert.AreEqual(4, tgus.PriorityOVTAScoreDAcreage());
            Assert.AreEqual(5, tgus.AlternateOVTAScoreAAcreage());
            Assert.AreEqual(6, tgus.AlternateOVTAScoreBAcreage());
            Assert.AreEqual(7, tgus.AlternateOVTAScoreCAcreage());
            Assert.AreEqual(8, tgus.AlternateOVTAScoreDAcreage());
        }

        [TestMethod]
        public void SameLandUse_SumsAcrossTgusAtSameScore()
        {
            var tgus = new List<TrashGeneratingUnit>
            {
                Tgu(2, ALU, OnlandVisualTrashAssessmentScoreEnum.C),
                Tgu(5, ALU, OnlandVisualTrashAssessmentScoreEnum.C),
                Tgu(9, PLU, OnlandVisualTrashAssessmentScoreEnum.C),
            };

            Assert.AreEqual(7, tgus.AlternateOVTAScoreCAcreage());
            Assert.AreEqual(9, tgus.PriorityOVTAScoreCAcreage());
        }

        [TestMethod]
        public void NoLandUseBlock_CountsAsPriority()
        {
            // Existing IsPLU() semantics: anything that is not explicitly ALU is treated as PLU.
            var tgus = new List<TrashGeneratingUnit> { Tgu(4, null, OnlandVisualTrashAssessmentScoreEnum.B) };

            Assert.AreEqual(4, tgus.PriorityOVTAScoreBAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreBAcreage());
        }

        [TestMethod]
        public void NoOvtaArea_CountsTowardNeither()
        {
            var tgus = new List<TrashGeneratingUnit>
            {
                Tgu(4, PLU, null),
                Tgu(6, ALU, null),
            };

            Assert.AreEqual(0, tgus.PriorityOVTAScoreAAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreAAcreage());
            Assert.AreEqual(0, tgus.PriorityOVTAScoreDAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreDAcreage());
        }

        [TestMethod]
        public void OvtaAreaWithoutBaselineScore_CountsTowardNeither()
        {
            // An OVTA Area only gets a baseline score once it has >= 2 completed baseline assessments
            // (OnlandVisualTrashAssessmentAreas.CalculateBaselineScoreFromBackingData). Areas below that
            // threshold must contribute nothing to any score bucket.
            var tgus = new List<TrashGeneratingUnit>
            {
                new()
                {
                    TrashGeneratingUnitGeometry = SquareOfAcres(5),
                    LandUseBlock = new LandUseBlock { PriorityLandUseTypeID = PLU },
                    OnlandVisualTrashAssessmentArea = new OnlandVisualTrashAssessmentArea { OnlandVisualTrashAssessmentBaselineScoreID = null },
                },
                new()
                {
                    TrashGeneratingUnitGeometry = SquareOfAcres(7),
                    LandUseBlock = new LandUseBlock { PriorityLandUseTypeID = ALU },
                    OnlandVisualTrashAssessmentArea = new OnlandVisualTrashAssessmentArea { OnlandVisualTrashAssessmentBaselineScoreID = null },
                },
            };

            Assert.AreEqual(0, tgus.PriorityOVTAScoreAAcreage());
            Assert.AreEqual(0, tgus.PriorityOVTAScoreBAcreage());
            Assert.AreEqual(0, tgus.PriorityOVTAScoreCAcreage());
            Assert.AreEqual(0, tgus.PriorityOVTAScoreDAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreAAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreBAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreCAcreage());
            Assert.AreEqual(0, tgus.AlternateOVTAScoreDAcreage());
        }
    }
}
