using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common.GeoSpatial;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    // Regression coverage for NPT-1095: the area's TransectLine must follow the backing assessment's observation
    // points even for assessments finalized before the IsTransectBackingAssessment flag existed (flag == false,
    // area already has a TransectLine). Before the fix, the isBacking gate skipped the update forever.
    [TestClass]
    public class OnlandVisualTrashAssessmentTransectBackingTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;
        private int _jurisdictionID;
        private int _personID;

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

            var jurisdictionID = _dbContext.StormwaterJurisdictions.AsNoTracking().OrderBy(x => x.StormwaterJurisdictionID).Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            Assert.IsNotNull(jurisdictionID, "Tests require at least 1 StormwaterJurisdiction row in the local DB.");
            _jurisdictionID = jurisdictionID.Value;

            var personID = _dbContext.People.AsNoTracking().OrderBy(x => x.PersonID).Select(x => (int?)x.PersonID).FirstOrDefault();
            Assert.IsNotNull(personID, "Tests require at least 1 Person row in the local DB.");
            _personID = personID.Value;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private static Geometry MakeSquare(double x0, double y0, double size)
        {
            var wkt = $"POLYGON(({x0} {y0}, {x0 + size} {y0}, {x0 + size} {y0 + size}, {x0} {y0 + size}, {x0} {y0}))";
            return GeometryHelper.FromWKT(wkt, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
        }

        // A 3-vertex line in the area's projection — stands in for a pre-existing (legacy) TransectLine. The
        // observation-driven transect below has 2 vertices, so NumPoints cleanly distinguishes "moved" (->2)
        // from "untouched" (stays 3) without depending on floating-point coordinate equality.
        private static Geometry MakeSeedTransect()
        {
            return GeometryHelper.FromWKT("LINESTRING (1840000 660000, 1840050 660050, 1840100 660100)", Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
        }

        private OnlandVisualTrashAssessmentArea CreateAreaWithSeededTransect(string name, double xOffset = 0)
        {
            var seedTransect = MakeSeedTransect();
            var area = new OnlandVisualTrashAssessmentArea
            {
                OnlandVisualTrashAssessmentAreaName = name,
                StormwaterJurisdictionID = _jurisdictionID,
                OnlandVisualTrashAssessmentAreaGeometry = MakeSquare(1_840_000 + xOffset, 660_000, 100),
                TransectLine = seedTransect,
                TransectLine4326 = seedTransect.ProjectTo4326(),
            };
            _dbContext.OnlandVisualTrashAssessmentAreas.Add(area);
            _dbContext.SaveChanges();
            return area;
        }

        private OnlandVisualTrashAssessment CreateCompletedAssessment(int areaID, DateOnly completedDate, bool isTransectBacking)
        {
            var assessment = new OnlandVisualTrashAssessment
            {
                CreatedByPersonID = _personID,
                CreatedDate = DateTime.UtcNow,
                OnlandVisualTrashAssessmentAreaID = areaID,
                StormwaterJurisdictionID = _jurisdictionID,
                OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.Complete,
                OnlandVisualTrashAssessmentScoreID = (int)OnlandVisualTrashAssessmentScoreEnum.B,
                CompletedDate = completedDate,
                IsTransectBackingAssessment = isTransectBacking,
                IsProgressAssessment = false,
            };
            _dbContext.OnlandVisualTrashAssessments.Add(assessment);
            _dbContext.SaveChanges();
            return assessment;
        }

        private static List<OnlandVisualTrashAssessmentObservationWithPhotoDto> TwoObservations()
        {
            return new List<OnlandVisualTrashAssessmentObservationWithPhotoDto>
            {
                new() { Latitude = 33.700, Longitude = -117.850, ObservationDatetime = new DateTime(2022, 1, 1, 9, 0, 0, DateTimeKind.Utc) },
                new() { Latitude = 33.701, Longitude = -117.851, ObservationDatetime = new DateTime(2022, 1, 1, 9, 5, 0, DateTimeKind.Utc) },
            };
        }

        private int? GetAreaTransectNumPoints(int areaID)
        {
            return _dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
                .Where(x => x.OnlandVisualTrashAssessmentAreaID == areaID)
                .Select(x => x.TransectLine)
                .Single()?.NumPoints;
        }

        // Core regression: a single Complete assessment, flag false, area already has a TransectLine (the legacy
        // state). Editing its observations must move the transect and stamp the flag.
        [TestMethod]
        public async Task EditingObservations_OnLegacyUnflaggedSoleAssessment_MovesTransectAndStampsFlag()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var area = CreateAreaWithSeededTransect($"NPT-1095 Legacy {unique}", 0);
            var assessment = CreateCompletedAssessment(area.OnlandVisualTrashAssessmentAreaID, new DateOnly(2021, 5, 1), isTransectBacking: false);

            Assert.AreEqual(3, GetAreaTransectNumPoints(area.OnlandVisualTrashAssessmentAreaID), "Precondition: area starts with the seeded 3-point legacy transect.");

            await OnlandVisualTrashAssessmentObservations.Update(_dbContext, assessment.OnlandVisualTrashAssessmentID, TwoObservations());

            Assert.AreEqual(2, GetAreaTransectNumPoints(area.OnlandVisualTrashAssessmentAreaID),
                "Transect should have moved to the 2 edited observation points (the NPT-1095 bug left it on the old 3-point line).");

            var refreshed = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Single(x => x.OnlandVisualTrashAssessmentID == assessment.OnlandVisualTrashAssessmentID);
            Assert.IsTrue(refreshed.IsTransectBackingAssessment, "The sole assessment should now be stamped as the transect-backing assessment.");
        }

        // No-regression: a repeat (non-backing) assessment in an area that already has a flagged backing assessment
        // must NOT move the transect — noOtherBackingExists is false because another assessment is flagged.
        [TestMethod]
        public async Task EditingObservations_OnRepeatAssessment_WhenBackingAlreadyFlagged_LeavesTransectAlone()
        {
            var unique = Guid.NewGuid().ToString().Substring(0, 8);
            var area = CreateAreaWithSeededTransect($"NPT-1095 Repeat {unique}", 200);
            // Earliest, flagged as backing.
            CreateCompletedAssessment(area.OnlandVisualTrashAssessmentAreaID, new DateOnly(2020, 5, 1), isTransectBacking: true);
            // Later repeat assessment, not backing — the one we edit.
            var repeat = CreateCompletedAssessment(area.OnlandVisualTrashAssessmentAreaID, new DateOnly(2021, 5, 1), isTransectBacking: false);

            await OnlandVisualTrashAssessmentObservations.Update(_dbContext, repeat.OnlandVisualTrashAssessmentID, TwoObservations());

            Assert.AreEqual(3, GetAreaTransectNumPoints(area.OnlandVisualTrashAssessmentAreaID),
                "Editing a non-backing repeat assessment must leave the backing-derived transect untouched.");

            var refreshed = _dbContext.OnlandVisualTrashAssessments.AsNoTracking().Single(x => x.OnlandVisualTrashAssessmentID == repeat.OnlandVisualTrashAssessmentID);
            Assert.IsFalse(refreshed.IsTransectBackingAssessment, "A repeat assessment must not be promoted to backing when one already exists.");
        }
    }
}
