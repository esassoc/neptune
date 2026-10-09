using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1128 rework: the OVTA Area grid gained acreage (from the native geometry), a baseline/progress split of
    /// the completed-assessment count, and comma-separated land use columns fed by a live Land Use Block intersect.
    /// These tests pin the projection in <see cref="OnlandVisualTrashAssessmentAreaExtensionMethods.AsGridDto"/>.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentAreaGridDtoTests
    {
        // 1 acre = 4046.856 m^2. A square with side sqrt(acres / 0.000247105) has the requested acreage (SRID 2771 is metres).
        private static Polygon SquareOfAcres(double acres)
        {
            var side = Math.Sqrt(acres / 0.000247105);
            return new Polygon(new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(side, 0),
                new Coordinate(side, side),
                new Coordinate(0, side),
                new Coordinate(0, 0),
            }));
        }

        private static OnlandVisualTrashAssessment Assessment(OnlandVisualTrashAssessmentStatusEnum status, bool isProgress, DateOnly? completedDate = null)
        {
            return new OnlandVisualTrashAssessment
            {
                OnlandVisualTrashAssessmentStatusID = (int)status,
                IsProgressAssessment = isProgress,
                CompletedDate = completedDate,
            };
        }

        private static OnlandVisualTrashAssessmentArea Area(double acres, params OnlandVisualTrashAssessment[] assessments)
        {
            return new OnlandVisualTrashAssessmentArea
            {
                OnlandVisualTrashAssessmentAreaID = 42,
                OnlandVisualTrashAssessmentAreaName = "Test Area",
                StormwaterJurisdictionID = 7,
                StormwaterJurisdiction = new StormwaterJurisdiction { Organization = new Organization { OrganizationName = "City of Test" } },
                OnlandVisualTrashAssessmentAreaGeometry = SquareOfAcres(acres),
                OnlandVisualTrashAssessments = new List<OnlandVisualTrashAssessment>(assessments),
            };
        }

        private static readonly IReadOnlyCollection<OnlandVisualTrashAssessmentAreaLandUseBlock> NoLandUse = Array.Empty<OnlandVisualTrashAssessmentAreaLandUseBlock>();

        [TestMethod]
        public void AreaAcres_ComesFromNativeGeometry_RoundedToTwoPlaces()
        {
            var dto = Area(12.3456).AsGridDto(NoLandUse);
            Assert.AreEqual(12.35, dto.AreaAcres, 0.001);
        }

        [TestMethod]
        public void CompletedCounts_SplitByBaselineAndProgress_AndTotalStillReported()
        {
            var area = Area(1,
                Assessment(OnlandVisualTrashAssessmentStatusEnum.Complete, isProgress: false, new DateOnly(2025, 1, 10)),
                Assessment(OnlandVisualTrashAssessmentStatusEnum.Complete, isProgress: false, new DateOnly(2025, 3, 10)),
                Assessment(OnlandVisualTrashAssessmentStatusEnum.Complete, isProgress: true, new DateOnly(2026, 2, 1)),
                Assessment(OnlandVisualTrashAssessmentStatusEnum.InProgress, isProgress: true),
                Assessment(OnlandVisualTrashAssessmentStatusEnum.InProgress, isProgress: false));

            var dto = area.AsGridDto(NoLandUse);

            Assert.AreEqual(2, dto.NumberOfBaselineAssessmentsCompleted);
            Assert.AreEqual(1, dto.NumberOfProgressAssessmentsCompleted);
            Assert.AreEqual(3, dto.NumberOfAssessmentsCompleted);
            Assert.AreEqual(2, dto.NumberOfAssessmentsInProgress);
            Assert.AreEqual(new DateOnly(2026, 2, 1), dto.LastAssessmentDate);
        }

        [TestMethod]
        public void LandUse_CommaSeparated_DistinctTypes_SortedBlockIDs()
        {
            var landUse = new List<OnlandVisualTrashAssessmentAreaLandUseBlock>
            {
                new(305, (int)PriorityLandUseTypeEnum.Industrial),
                new(12, (int)PriorityLandUseTypeEnum.Commercial),
                new(77, (int)PriorityLandUseTypeEnum.Commercial),
                new(90, (int)PriorityLandUseTypeEnum.ALU),
            };

            var dto = Area(1).AsGridDto(landUse);

            var commercial = PriorityLandUseType.Commercial.PriorityLandUseTypeDisplayName;
            var industrial = PriorityLandUseType.Industrial.PriorityLandUseTypeDisplayName;
            var alu = PriorityLandUseType.ALU.PriorityLandUseTypeDisplayName;
            var expectedTypes = new List<string> { commercial, industrial, alu };
            expectedTypes.Sort(StringComparer.Ordinal);

            Assert.AreEqual(string.Join(", ", expectedTypes), dto.LandUseTypes);
            Assert.AreEqual("12, 77, 90, 305", dto.LandUseBlockIDs);
        }

        [TestMethod]
        public void LandUse_BlocksWithoutPriorityType_StillListedByID_ButContributeNoTypeName()
        {
            var landUse = new List<OnlandVisualTrashAssessmentAreaLandUseBlock> { new(5, null), new(6, null) };

            var dto = Area(1).AsGridDto(landUse);

            Assert.IsNull(dto.LandUseTypes);
            Assert.AreEqual("5, 6", dto.LandUseBlockIDs);
        }

        [TestMethod]
        public void LandUse_NoOverlappingBlocks_YieldsNulls()
        {
            var dto = Area(1).AsGridDto(NoLandUse);

            Assert.IsNull(dto.LandUseTypes);
            Assert.IsNull(dto.LandUseBlockIDs);
        }
    }
}
