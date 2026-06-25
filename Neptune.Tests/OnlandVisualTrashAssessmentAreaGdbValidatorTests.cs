using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1075 round 2 — covers <see cref="OnlandVisualTrashAssessmentAreaGdbValidator.Validate"/>,
    /// the per-row validator that replaces the previous silent geometry filter + generic
    /// "file may be corrupted or invalid" catch-all in the OVTA Area GDB upload pipeline.
    /// Pure-function: no DbContext, no DB hits.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentAreaGdbValidatorTests
    {
        private static readonly GeometryFactory GeomFactory =
            NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 2771);

        /// <summary>1×1 square polygon, valid, Area = 1.</summary>
        private static Polygon ValidSquare() => GeomFactory.CreatePolygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(1, 0), new Coordinate(1, 1), new Coordinate(0, 1), new Coordinate(0, 0),
        });

        /// <summary>Empty MultiPolygon — geometrically valid but zero area.</summary>
        private static MultiPolygon EmptyMultiPolygon() => GeomFactory.CreateMultiPolygon(new Polygon[0]);

        /// <summary>
        /// Self-intersecting "bowtie" ring — NTS reports <c>IsValid == false</c>. Exercises the
        /// validator branch that the empty-MultiPolygon doesn't (empty is IsValid=true, Area=0).
        /// </summary>
        private static Polygon BowtiePolygon() => GeomFactory.CreatePolygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(1, 1), new Coordinate(1, 0), new Coordinate(0, 1), new Coordinate(0, 0),
        });

        [TestMethod]
        public void NullAreaName_Errors()
        {
            // Cast through null! to mirror the EF entity's non-nullable string property — the
            // GDAL/GeoJsonSerializer path can land us in this state when the source GDB has a
            // null in the AreaName field.
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = null!, Geometry = ValidSquare(), Description = null, StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "Feature 1");
            StringAssert.Contains(errors[0], "OVTA Area Name is missing");
        }

        [TestMethod]
        public void BlankAreaName_Errors()
        {
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "   ", Geometry = ValidSquare(), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "OVTA Area Name is missing");
        }

        [TestMethod]
        public void InvalidGeometry_Errors()
        {
            // Sanity-check that NTS actually reports the bowtie as invalid; if a future NTS
            // upgrade changes that, the test would silently pass against the zero-area branch
            // instead of the IsValid=false branch we're trying to cover.
            var bowtie = BowtiePolygon();
            Assert.IsFalse(bowtie.IsValid, "Bowtie polygon must be reported as IsValid=false to exercise the right validator branch.");

            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_Bowtie_1", Geometry = bowtie, StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "Feature 1");
            StringAssert.Contains(errors[0], "LN_Bowtie_1");
            StringAssert.Contains(errors[0], "NTS reports IsValid = false");
        }

        [TestMethod]
        public void NullGeometry_ErrorsWithMissingMessage()
        {
            // Defensive: the GDAL pipeline shouldn't normally produce a null Geometry, but the
            // staging entity allows it during deserialization. The validator must produce a
            // distinct "geometry is missing" message rather than misleading users with the
            // NTS-IsValid wording from the invalid-geometry branch.
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_NullGeom_1", Geometry = null!, StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "geometry is missing");
            Assert.IsFalse(errors[0].Contains("IsValid"), "Null geometry should not be reported as IsValid=false.");
        }

        [TestMethod]
        public void ZeroAreaGeometry_Errors()
        {
            // Empty MultiPolygon: IsValid = true, but Area = 0 — the legacy silent filter dropped
            // these without telling the user; now we surface a specific message.
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_ZeroArea_1", Geometry = EmptyMultiPolygon(), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "Feature 1");
            StringAssert.Contains(errors[0], "LN_ZeroArea_1");
            StringAssert.Contains(errors[0], "zero or negative area");
        }

        [TestMethod]
        public void OversizedDescription_Errors()
        {
            var oversized = new string('x', OnlandVisualTrashAssessmentAreaGdbValidator.MaxDescriptionLength + 1);
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_LongDesc_1", Geometry = ValidSquare(), Description = oversized, StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "Feature 1");
            StringAssert.Contains(errors[0], "LN_LongDesc_1");
            StringAssert.Contains(errors[0], $"{OnlandVisualTrashAssessmentAreaGdbValidator.MaxDescriptionLength}-character limit");
            StringAssert.Contains(errors[0], $"{oversized.Length} characters provided");
        }

        [TestMethod]
        public void ValidStagings_NoErrors()
        {
            // Regression guard: clean rows should produce zero errors so the upload can proceed.
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_OK_1", Geometry = ValidSquare(), Description = "short", StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
                new() { AreaName = "LN_OK_2", Geometry = ValidSquare(), Description = null, StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
                new() { AreaName = "LN_OK_3", Geometry = ValidSquare(), Description = new string('y', OnlandVisualTrashAssessmentAreaGdbValidator.MaxDescriptionLength), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }

        [TestMethod]
        public void MultipleBadRows_ProduceErrorPerRowWithCorrectFeatureNumbers()
        {
            // Row indexing is 1-based and matches the position in the input list, so a user can
            // map errors back to the feature they see in their GIS tooling.
            var stagings = new List<OnlandVisualTrashAssessmentAreaStaging>
            {
                new() { AreaName = "LN_OK", Geometry = ValidSquare(), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
                new() { AreaName = "", Geometry = ValidSquare(), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
                new() { AreaName = "LN_ZeroArea", Geometry = EmptyMultiPolygon(), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
                new() { AreaName = "LN_LongDesc", Geometry = ValidSquare(), Description = new string('z', 501), StormwaterJurisdictionID = 1, UploadedByPersonID = 1 },
            };

            var errors = OnlandVisualTrashAssessmentAreaGdbValidator.Validate(stagings);

            Assert.AreEqual(3, errors.Count);
            Assert.IsTrue(errors.Any(e => e.Contains("Feature 2") && e.Contains("OVTA Area Name is missing")));
            Assert.IsTrue(errors.Any(e => e.Contains("Feature 3") && e.Contains("LN_ZeroArea")));
            Assert.IsTrue(errors.Any(e => e.Contains("Feature 4") && e.Contains("LN_LongDesc")));
        }
    }
}
