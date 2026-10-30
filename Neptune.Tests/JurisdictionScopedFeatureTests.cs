using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1129 — integration tests (real local DB, rolled back) for the jurisdiction-scoped gates.
    /// Each test takes an existing row, reads its owning jurisdiction, and drives
    /// <see cref="JurisdictionScopedFeature.Evaluate"/> with in-memory people: in-jurisdiction → allowed,
    /// out-of-jurisdiction → 403, Admin → allowed, missing id → 404 (before 403), Unassigned → 403.
    /// The role check itself runs in BaseAuthorizationAttribute and is pinned by
    /// FieldVisitEndpointAuthorizationTests.
    /// </summary>
    [TestClass]
    public class JurisdictionScopedFeatureTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

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
        }

        [TestCleanup]
        public void Cleanup()
        {
            _transaction.Rollback();
            _transaction.Dispose();
            _dbContext.Dispose();
        }

        private static Person PersonAssignedTo(RoleEnum role, int stormwaterJurisdictionID) => new()
        {
            RoleID = (int)role,
            StormwaterJurisdictionPeople =
            {
                new StormwaterJurisdictionPerson { StormwaterJurisdictionID = stormwaterJurisdictionID },
            },
        };

        private void AssertScoped(JurisdictionScopedFeature feature, RoleEnum inJurisdictionRole, int id, int stormwaterJurisdictionID)
        {
            var name = feature.GetType().Name;

            Assert.IsNull(feature.Evaluate(_dbContext, PersonAssignedTo(inJurisdictionRole, stormwaterJurisdictionID), id),
                $"{name}: a {inJurisdictionRole} assigned to the record's jurisdiction must be allowed.");

            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, PersonAssignedTo(inJurisdictionRole, -1), id), typeof(ForbidResult),
                $"{name}: a {inJurisdictionRole} assigned to a different jurisdiction must get 403.");

            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, PersonAssignedTo(RoleEnum.JurisdictionEditor, -1), id), typeof(ForbidResult),
                $"{name}: a JurisdictionEditor assigned to a different jurisdiction must get 403.");

            Assert.IsNull(feature.Evaluate(_dbContext, new Person { RoleID = (int)RoleEnum.Admin }, id),
                $"{name}: Admin with no assignments must be allowed.");

            Assert.IsNull(feature.Evaluate(_dbContext, new Person { RoleID = (int)RoleEnum.SitkaAdmin }, id),
                $"{name}: SitkaAdmin with no assignments must be allowed.");

            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, new Person { RoleID = (int)RoleEnum.Unassigned }, id), typeof(ForbidResult),
                $"{name}: an Unassigned user must get 403.");

            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, null, id), typeof(ForbidResult),
                $"{name}: no user must get 403.");

            // 404 comes before 403: a missing record is reported as missing even to an out-of-jurisdiction caller.
            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, PersonAssignedTo(inJurisdictionRole, stormwaterJurisdictionID), int.MaxValue), typeof(NotFoundResult),
                $"{name}: a nonexistent id must get 404.");
            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, PersonAssignedTo(inJurisdictionRole, -1), int.MaxValue), typeof(NotFoundResult),
                $"{name}: a nonexistent id must get 404 even for an out-of-jurisdiction caller.");
        }

        [TestMethod]
        public void FieldVisitFeatures_AreJurisdictionScoped()
        {
            var row = _dbContext.FieldVisits.AsNoTracking()
                .Select(x => new { x.FieldVisitID, x.TreatmentBMP.StormwaterJurisdictionID })
                .FirstOrDefault();
            if (row == null)
            {
                Assert.Inconclusive("No FieldVisits in local DB.");
                return;
            }

            AssertScoped(new FieldVisitEditFeature(), RoleEnum.JurisdictionEditor, row.FieldVisitID, row.StormwaterJurisdictionID);
            AssertScoped(new FieldVisitManageFeature(), RoleEnum.JurisdictionManager, row.FieldVisitID, row.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void MaintenanceRecordFeatures_AreJurisdictionScoped()
        {
            var row = _dbContext.MaintenanceRecords.AsNoTracking()
                .Select(x => new { x.MaintenanceRecordID, x.TreatmentBMP.StormwaterJurisdictionID })
                .FirstOrDefault();
            if (row == null)
            {
                Assert.Inconclusive("No MaintenanceRecords in local DB.");
                return;
            }

            AssertScoped(new MaintenanceRecordEditFeature(), RoleEnum.JurisdictionEditor, row.MaintenanceRecordID, row.StormwaterJurisdictionID);
            AssertScoped(new MaintenanceRecordManageFeature(), RoleEnum.JurisdictionManager, row.MaintenanceRecordID, row.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void TreatmentBMPAssessmentEditFeature_IsJurisdictionScoped()
        {
            var row = _dbContext.TreatmentBMPAssessments.AsNoTracking()
                .Select(x => new { x.TreatmentBMPAssessmentID, x.TreatmentBMP.StormwaterJurisdictionID })
                .FirstOrDefault();
            if (row == null)
            {
                Assert.Inconclusive("No TreatmentBMPAssessments in local DB.");
                return;
            }

            AssertScoped(new TreatmentBMPAssessmentEditFeature(), RoleEnum.JurisdictionEditor, row.TreatmentBMPAssessmentID, row.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void WaterQualityManagementPlanFeatures_AreJurisdictionScoped()
        {
            var row = _dbContext.WaterQualityManagementPlans.AsNoTracking()
                .Select(x => new { x.WaterQualityManagementPlanID, x.StormwaterJurisdictionID })
                .FirstOrDefault();
            if (row == null)
            {
                Assert.Inconclusive("No WaterQualityManagementPlans in local DB.");
                return;
            }

            AssertScoped(new WaterQualityManagementPlanEditFeature(), RoleEnum.JurisdictionEditor, row.WaterQualityManagementPlanID, row.StormwaterJurisdictionID);
            AssertScoped(new WaterQualityManagementPlanManageFeature(), RoleEnum.JurisdictionManager, row.WaterQualityManagementPlanID, row.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void TreatmentBMPEditFeature_IsJurisdictionScoped()
        {
            // Refactored onto JurisdictionScopedFeature in NPT-1129 — behavior must be unchanged.
            var row = _dbContext.TreatmentBMPs.AsNoTracking()
                .Select(x => new { x.TreatmentBMPID, x.StormwaterJurisdictionID })
                .FirstOrDefault();
            if (row == null)
            {
                Assert.Inconclusive("No TreatmentBMPs in local DB.");
                return;
            }

            AssertScoped(new TreatmentBMPEditFeature(), RoleEnum.JurisdictionEditor, row.TreatmentBMPID, row.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void ProjectViewFeature_IsJurisdictionScoped_WithOCTAReviewerException()
        {
            var project = _dbContext.Projects.FirstOrDefault();
            if (project == null)
            {
                Assert.Inconclusive("No Projects in local DB.");
                return;
            }

            project.ShareOCTAM2Tier2Scores = false;
            _dbContext.SaveChanges();
            var feature = new ProjectViewFeature();
            AssertScoped(feature, RoleEnum.JurisdictionEditor, project.ProjectID, project.StormwaterJurisdictionID);

            var unassignedReviewer = new Person { RoleID = (int)RoleEnum.Unassigned, IsOCTAGrantReviewer = true };
            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, unassignedReviewer, project.ProjectID), typeof(ForbidResult),
                "An OCTA grant reviewer must get 403 on a project that isn't shared with the program.");

            project.ShareOCTAM2Tier2Scores = true;
            _dbContext.SaveChanges();
            Assert.IsNull(feature.Evaluate(_dbContext, unassignedReviewer, project.ProjectID),
                "An Unassigned OCTA grant reviewer must be allowed on a shared project.");
            Assert.IsInstanceOfType(feature.Evaluate(_dbContext, new Person { RoleID = (int)RoleEnum.Unassigned }, project.ProjectID), typeof(ForbidResult),
                "An Unassigned non-reviewer must get 403 even on a shared project.");
        }

        [TestMethod]
        public void TrashScreenImporter_ViewableJurisdictions_EqualAssignedForEditor()
        {
            // PO decision on NPT-1129: the Trash Screen bulk upload checks rows against the caller's
            // ASSIGNED jurisdictions. TrashScreenFieldVisitImporter uses ListViewableByPersonForBMPs,
            // which must equal the assigned set for a JurisdictionEditor.
            var editor = _dbContext.People
                .Include(x => x.StormwaterJurisdictionPeople)
                .AsNoTracking()
                .FirstOrDefault(x => x.RoleID == (int)RoleEnum.JurisdictionEditor && x.StormwaterJurisdictionPeople.Any());
            if (editor == null)
            {
                Assert.Inconclusive("No JurisdictionEditor with jurisdiction assignments in local DB.");
                return;
            }

            var assignedIDs = editor.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToList();
            var viewableIDs = StormwaterJurisdictions.ListViewableByPersonForBMPs(_dbContext, editor)
                .Select(x => x.StormwaterJurisdictionID)
                .ToList();

            CollectionAssert.AreEquivalent(assignedIDs, viewableIDs,
                "For a JurisdictionEditor, the importer's viewable jurisdictions must equal the assigned jurisdictions.");
        }
    }
}
