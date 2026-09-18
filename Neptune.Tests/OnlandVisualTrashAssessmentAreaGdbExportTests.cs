using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1128 rework: the OVTA Area GDB download carries every OVTA Area grid column and no longer emits
    /// "CreatedOn" (it was the newest assessment's creation date, not the area's). The GDAL zip step is
    /// integration-only, so these tests target <see cref="OnlandVisualTrashAssessmentAreaGdbExport.ToFeatureCollection"/>.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentAreaGdbExportTests
    {
        private static readonly string[] ExpectedAttributeNames =
        {
            "OVTAAreaID", "OVTAAreaName", "Jurisdiction", "BaselineScore", "ProgressScore",
            "AssessmentsInProgress", "CompletedBaselineAssessments", "CompletedProgressAssessments",
            "Area_Acres", "LastAssessmentDate", "LandUseTypes", "LandUseBlockIDs", "Description",
        };

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

        private static OnlandVisualTrashAssessmentArea Area(int id, double acres)
        {
            return new OnlandVisualTrashAssessmentArea
            {
                OnlandVisualTrashAssessmentAreaID = id,
                OnlandVisualTrashAssessmentAreaName = $"Area {id}",
                AssessmentAreaDescription = "Along the creek",
                StormwaterJurisdictionID = 7,
                StormwaterJurisdiction = new StormwaterJurisdiction { Organization = new Organization { OrganizationName = "City of Test" } },
                OnlandVisualTrashAssessmentAreaGeometry = SquareOfAcres(acres),
                OnlandVisualTrashAssessmentBaselineScoreID = (int)OnlandVisualTrashAssessmentScoreEnum.B,
                OnlandVisualTrashAssessments = new List<OnlandVisualTrashAssessment>
                {
                    new() { OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.Complete, IsProgressAssessment = false, CompletedDate = new DateOnly(2025, 4, 1) },
                    new() { OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.Complete, IsProgressAssessment = false, CompletedDate = new DateOnly(2025, 6, 15) },
                    new() { OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress, IsProgressAssessment = true },
                },
            };
        }

        private static IReadOnlyDictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>> NoLandUse =>
            new Dictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>>();

        [TestMethod]
        public void ToFeatureCollection_EmitsGridColumns_AndNoCreatedOn()
        {
            var landUse = new Dictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>>
            {
                [1] = new() { new(200, (int)PriorityLandUseTypeEnum.Commercial), new(100, (int)PriorityLandUseTypeEnum.Commercial) },
            };

            var fc = OnlandVisualTrashAssessmentAreaGdbExport.ToFeatureCollection(new[] { Area(1, 10) }, landUse);

            Assert.AreEqual(1, fc.Count);
            var feature = fc[0];
            Assert.IsNotNull(feature.Geometry);
            CollectionAssert.AreEquivalent(ExpectedAttributeNames, feature.Attributes.GetNames());
            Assert.IsFalse(feature.Attributes.Exists("CreatedOn"));

            Assert.AreEqual(1, feature.Attributes["OVTAAreaID"]);
            Assert.AreEqual("Area 1", feature.Attributes["OVTAAreaName"]);
            Assert.AreEqual("City of Test", feature.Attributes["Jurisdiction"]);
            Assert.AreEqual(OnlandVisualTrashAssessmentScore.B.OnlandVisualTrashAssessmentScoreDisplayName, feature.Attributes["BaselineScore"]);
            Assert.IsNull(feature.Attributes["ProgressScore"]);
            Assert.AreEqual(1, feature.Attributes["AssessmentsInProgress"]);
            Assert.AreEqual(2, feature.Attributes["CompletedBaselineAssessments"]);
            Assert.AreEqual(0, feature.Attributes["CompletedProgressAssessments"]);
            Assert.AreEqual(10.0, (double)feature.Attributes["Area_Acres"]!, 0.01);
            Assert.AreEqual("2025-06-15", feature.Attributes["LastAssessmentDate"]);
            Assert.AreEqual(PriorityLandUseType.Commercial.PriorityLandUseTypeDisplayName, feature.Attributes["LandUseTypes"]);
            Assert.AreEqual("100, 200", feature.Attributes["LandUseBlockIDs"]);
            Assert.AreEqual("Along the creek", feature.Attributes["Description"]);
        }

        [TestMethod]
        public void ToFeatureCollection_NoAreas_EmitsSchemaOnlyFeature_WithSameColumns()
        {
            var fc = OnlandVisualTrashAssessmentAreaGdbExport.ToFeatureCollection(Array.Empty<OnlandVisualTrashAssessmentArea>(), NoLandUse);

            Assert.AreEqual(1, fc.Count);
            var feature = fc[0];
            Assert.IsNull(feature.Geometry);
            CollectionAssert.AreEquivalent(ExpectedAttributeNames, feature.Attributes.GetNames());
            Assert.IsTrue(feature.Attributes.GetNames().All(name => feature.Attributes[name] == null));
        }

        [TestMethod]
        public void ToFeatureCollection_AreaWithoutLandUse_HasNullLandUseColumns()
        {
            var fc = OnlandVisualTrashAssessmentAreaGdbExport.ToFeatureCollection(new[] { Area(3, 2) }, NoLandUse);

            var feature = fc[0];
            Assert.IsNull(feature.Attributes["LandUseTypes"]);
            Assert.IsNull(feature.Attributes["LandUseBlockIDs"]);
        }
    }
}
