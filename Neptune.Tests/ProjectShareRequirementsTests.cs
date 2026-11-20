using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Workflows;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1152 — a project can be shared with the OCTA M2 Tier 2 Grant Program only when the OCTA box is checked and
    /// either every Treatment BMP has model results or the project is flagged as having no Treatment BMPs.
    /// Most tests need no database; the DB-backed one reads the local NeptuneDB (read-only) and is Inconclusive without projects.
    /// </summary>
    [TestClass]
    public class ProjectShareRequirementsTests
    {
        private static Project BuildProject(int[] treatmentBMPIDs, int[] treatmentBMPIDsWithResults)
        {
            var project = new Project { ProjectID = 1 };
            foreach (var treatmentBMPID in treatmentBMPIDs)
            {
                project.TreatmentBMPs.Add(new TreatmentBMP { TreatmentBMPID = treatmentBMPID, ProjectID = 1 });
            }
            foreach (var treatmentBMPID in treatmentBMPIDsWithResults)
            {
                project.ProjectNereidResults.Add(new ProjectNereidResult { ProjectID = 1, TreatmentBMPID = treatmentBMPID });
            }
            // Non-BMP results (e.g. land surface nodes) have no TreatmentBMPID and must not count.
            project.ProjectNereidResults.Add(new ProjectNereidResult { ProjectID = 1, TreatmentBMPID = null });
            return project;
        }

        [TestMethod]
        public void OCTABoxUnchecked_IsRejected_EvenWithModelResults()
        {
            var errors = Projects.ValidateOCTAShareRequirements(false, false, true);
            CollectionAssert.AreEqual(new[] { Projects.OCTAShareRequiresCheckboxMessage }, errors);
        }

        [TestMethod]
        public void OCTABoxUnchecked_AndNoModelResults_ReportsBoth()
        {
            var errors = Projects.ValidateOCTAShareRequirements(false, false, false);
            CollectionAssert.AreEqual(new[] { Projects.OCTAShareRequiresCheckboxMessage, Projects.OCTAShareRequiresModelResultsMessage }, errors);
        }

        [TestMethod]
        public void OCTABoxChecked_BMPsLackModelResults_IsRejected()
        {
            var errors = Projects.ValidateOCTAShareRequirements(true, false, false);
            CollectionAssert.AreEqual(new[] { Projects.OCTAShareRequiresModelResultsMessage }, errors);
        }

        [TestMethod]
        public void OCTABoxChecked_DoesNotIncludeTreatmentBMPs_IsAllowed()
        {
            Assert.AreEqual(0, Projects.ValidateOCTAShareRequirements(true, true, false).Count);
        }

        [TestMethod]
        public void OCTABoxChecked_AllBMPsModeled_IsAllowed()
        {
            Assert.AreEqual(0, Projects.ValidateOCTAShareRequirements(true, false, true).Count);
        }

        [TestMethod]
        public void AllTreatmentBMPsHaveModelResults_NoBMPs_IsFalse()
        {
            Assert.IsFalse(ProjectWorkflowProgress.AllTreatmentBMPsHaveModelResults(BuildProject([], [])));
        }

        [TestMethod]
        public void AllTreatmentBMPsHaveModelResults_SomeBMPsModeled_IsFalse()
        {
            Assert.IsFalse(ProjectWorkflowProgress.AllTreatmentBMPsHaveModelResults(BuildProject([10, 11], [10])));
        }

        [TestMethod]
        public void AllTreatmentBMPsHaveModelResults_EveryBMPModeled_IsTrue()
        {
            Assert.IsTrue(ProjectWorkflowProgress.AllTreatmentBMPsHaveModelResults(BuildProject([10, 11], [10, 11, 11])));
        }

        [TestMethod]
        public void ModeledPerformanceStep_DoesNotIncludeTreatmentBMPs_IsComplete()
        {
            var project = BuildProject([], []);
            project.DoesNotIncludeTreatmentBMPs = true;
            Assert.IsTrue(ProjectWorkflowProgress.WorkflowStepComplete(project, ProjectWorkflowProgress.ProjectWorkflowStep.ModeledPerformanceAndGrantMetrics,
                new System.Collections.Generic.Dictionary<int, vTreatmentBMPModelingAttribute>()));
        }

        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = TestDatabase.ConnectionString;
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        [TestMethod]
        public async Task AllTreatmentBMPsHaveModelResultsAsync_MatchesWorkflowRule_ForEveryProject()
        {
            await using var dbContext = GetDbContext();
            var projectIDs = await dbContext.Projects.AsNoTracking().Select(x => x.ProjectID).ToListAsync();
            if (projectIDs.Count == 0)
            {
                Assert.Inconclusive("No projects in the local database.");
            }

            foreach (var projectID in projectIDs)
            {
                var project = Projects.GetByIDWithTrackingForWorkflow(dbContext, projectID);
                Assert.AreEqual(ProjectWorkflowProgress.AllTreatmentBMPsHaveModelResults(project),
                    await Projects.AllTreatmentBMPsHaveModelResultsAsync(dbContext, projectID), $"ProjectID {projectID}");
                dbContext.ChangeTracker.Clear();
            }
        }
    }
}
