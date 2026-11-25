using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155, NPT-1157: the queries and views that decide what goes into the overlays and the model.
    /// Only what is modeled may take ground in the Load Generating Unit overlay: a verified
    /// delineation (or a project's own), an Active WQMP. Integration tests against the local DB,
    /// rolled back; each one takes a record, changes its status and checks it drops out (or stays).
    /// The view tests need the current views deployed to the local DB.
    /// </summary>
    [TestClass]
    public class ModelingInputViewTests
    {
        private NeptuneDbContext _dbContext = null!;
        private IDbContextTransaction _transaction = null!;

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                TestDatabase.ConnectionString,
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

        private int ScalarInt(string sql) => _dbContext.Database.SqlQueryRaw<int>(sql).AsEnumerable().First();

        private int? FirstIDOrNull(string sql)
        {
            var ids = _dbContext.Database.SqlQueryRaw<int>(sql).AsEnumerable().ToList();
            return ids.Any() ? ids.First() : null;
        }

        private void SetDelineationVerified(int delineationID, bool isVerified)
        {
            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.Delineation SET IsVerified = {0} WHERE DelineationID = {1}", isVerified, delineationID);
        }

        private bool IsLoadGeneratingUnitInput(int delineationID) =>
            Delineations.ListLoadGeneratingUnitInputs(_dbContext).Any(x => x.DelineationID == delineationID);

        private bool IsProjectLoadGeneratingUnitInput(int delineationID, int projectID) =>
            Delineations.ListProjectLoadGeneratingUnitInputs(_dbContext, projectID).Any(x => x.DelineationID == delineationID);

        private bool IsTrashGeneratingUnitInput(int delineationID) =>
            Delineations.ListTrashGeneratingUnitInputs(_dbContext).Any(x => x.DelineationID == delineationID);

        // a distributed delineation on a modeled BMP type; projectFilter picks existing or project BMPs
        private int? FirstModeledDistributedDelineationID(string projectFilter, bool? isVerified = null)
        {
            var verifiedFilter = isVerified == null ? "" : $" AND d.IsVerified = {(isVerified.Value ? 1 : 0)}";
            return FirstIDOrNull($@"
                SELECT TOP 1 d.DelineationID AS Value
                FROM dbo.Delineation d
                    JOIN dbo.TreatmentBMP t ON d.TreatmentBMPID = t.TreatmentBMPID
                    JOIN dbo.TreatmentBMPType tt ON t.TreatmentBMPTypeID = tt.TreatmentBMPTypeID
                WHERE d.DelineationTypeID = {(int)DelineationTypeEnum.Distributed} AND tt.TreatmentBMPModelingTypeID IS NOT NULL
                    AND t.ProjectID {projectFilter}{verifiedFilter}
                ORDER BY d.DelineationID");
        }

        private int ProjectIDOfDelineation(int delineationID) => ScalarInt(
            $"SELECT t.ProjectID AS Value FROM dbo.Delineation d JOIN dbo.TreatmentBMP t ON d.TreatmentBMPID = t.TreatmentBMPID WHERE d.DelineationID = {delineationID}");

        [TestMethod]
        public void LoadGeneratingUnitInputs_DropADelineationThatBecomesProvisional()
        {
            var delineationID = FirstModeledDistributedDelineationID("IS NULL", isVerified: true);
            if (delineationID == null) Assert.Inconclusive("No delineation LGU inputs in the local DB.");
            Assert.IsTrue(IsLoadGeneratingUnitInput(delineationID.Value));

            SetDelineationVerified(delineationID.Value, false);

            Assert.IsFalse(IsLoadGeneratingUnitInput(delineationID.Value));
        }

        [TestMethod]
        public void LoadGeneratingUnitInputs_LeaveOutAProjectDelineation_EvenWhenVerified()
        {
            var delineationID = FirstModeledDistributedDelineationID("IS NOT NULL");
            if (delineationID == null) Assert.Inconclusive("No project delineations in the local DB.");

            SetDelineationVerified(delineationID.Value, true);

            Assert.IsFalse(IsLoadGeneratingUnitInput(delineationID.Value));
        }

        [TestMethod]
        public void ProjectLoadGeneratingUnitInputs_DropAnExistingDelineationThatBecomesProvisional()
        {
            var delineationID = FirstModeledDistributedDelineationID("IS NULL", isVerified: true);
            var projectID = FirstIDOrNull("SELECT TOP 1 ProjectID AS Value FROM dbo.Project ORDER BY ProjectID");
            if (delineationID == null || projectID == null) Assert.Inconclusive("The local DB needs a delineation LGU input and a project.");
            Assert.IsTrue(IsProjectLoadGeneratingUnitInput(delineationID.Value, projectID.Value));

            SetDelineationVerified(delineationID.Value, false);

            Assert.IsFalse(IsProjectLoadGeneratingUnitInput(delineationID.Value, projectID.Value));
        }

        [TestMethod]
        public void ProjectLoadGeneratingUnitInputs_KeepTheProjectsOwnProvisionalDelineation()
        {
            var delineationID = FirstModeledDistributedDelineationID("IS NOT NULL");
            if (delineationID == null) Assert.Inconclusive("No project delineations in the local DB.");
            var projectID = ProjectIDOfDelineation(delineationID.Value);

            SetDelineationVerified(delineationID.Value, false);

            Assert.IsTrue(IsProjectLoadGeneratingUnitInput(delineationID.Value, projectID));
        }

        [TestMethod]
        public void ProjectLoadGeneratingUnitInputs_LeaveOutAnotherProjectsDelineation_EvenWhenVerified()
        {
            var delineationID = FirstModeledDistributedDelineationID("IS NOT NULL");
            if (delineationID == null) Assert.Inconclusive("No project delineations in the local DB.");
            var otherProjectID = FirstIDOrNull($"SELECT TOP 1 ProjectID AS Value FROM dbo.Project WHERE ProjectID <> {ProjectIDOfDelineation(delineationID.Value)} ORDER BY ProjectID");
            if (otherProjectID == null) Assert.Inconclusive("The local DB needs a second project.");

            SetDelineationVerified(delineationID.Value, true);

            Assert.IsFalse(IsProjectLoadGeneratingUnitInput(delineationID.Value, otherProjectID.Value));
        }

        [TestMethod]
        public void TrashGeneratingUnitInputs_AreVerifiedDelineationsOfEitherType()
        {
            // a centralized one where there is one: the trash overlay takes both types
            var delineationID = FirstIDOrNull($@"
                SELECT TOP 1 d.DelineationID AS Value
                FROM dbo.Delineation d JOIN dbo.TreatmentBMP t ON d.TreatmentBMPID = t.TreatmentBMPID
                WHERE t.ProjectID IS NULL AND d.IsVerified = 1
                ORDER BY CASE WHEN d.DelineationTypeID = {(int)DelineationTypeEnum.Centralized} THEN 0 ELSE 1 END, d.DelineationID");
            if (delineationID == null) Assert.Inconclusive("No verified delineations in the local DB.");
            Assert.IsTrue(IsTrashGeneratingUnitInput(delineationID.Value));

            SetDelineationVerified(delineationID.Value, false);

            Assert.IsFalse(IsTrashGeneratingUnitInput(delineationID.Value));
        }

        [TestMethod]
        public void TrashGeneratingUnitInputs_LeaveOutAProjectDelineation_EvenWhenVerified()
        {
            // NPT-1157: this used to hold only because no project delineation happened to be verified
            var delineationID = FirstIDOrNull(@"
                SELECT TOP 1 d.DelineationID AS Value
                FROM dbo.Delineation d JOIN dbo.TreatmentBMP t ON d.TreatmentBMPID = t.TreatmentBMPID
                WHERE t.ProjectID IS NOT NULL ORDER BY d.DelineationID");
            if (delineationID == null) Assert.Inconclusive("No project delineations in the local DB.");

            SetDelineationVerified(delineationID.Value, true);

            Assert.IsFalse(IsTrashGeneratingUnitInput(delineationID.Value));
        }

        [TestMethod]
        [DataRow((int)WaterQualityManagementPlanStatusEnum.Inactive)]
        [DataRow((int)WaterQualityManagementPlanStatusEnum.Draft)]
        public void WaterQualityManagementPlanLGUInput_DropsAWqmpThatLeavesActive(int newStatusID)
        {
            var waterQualityManagementPlanID = FirstIDOrNull("SELECT TOP 1 WQMPID AS Value FROM dbo.vPyQgisWaterQualityManagementPlanLGUInput");
            if (waterQualityManagementPlanID == null) Assert.Inconclusive("No WQMP LGU inputs in the local DB.");

            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.WaterQualityManagementPlan SET WaterQualityManagementPlanStatusID = {0} WHERE WaterQualityManagementPlanID = {1}", newStatusID, waterQualityManagementPlanID.Value);

            Assert.AreEqual(0, ScalarInt($"SELECT COUNT(*) AS Value FROM dbo.vPyQgisWaterQualityManagementPlanLGUInput WHERE WQMPID = {waterQualityManagementPlanID}"));
        }

        [TestMethod]
        public void RegionalSubbasinCentralizedBMP_DropsABmpWhoseDelineationBecomesProvisional()
        {
            var treatmentBMPID = FirstIDOrNull("SELECT TOP 1 TreatmentBMPID AS Value FROM dbo.vNereidRegionalSubbasinCentralizedBMP");
            if (treatmentBMPID == null) Assert.Inconclusive("No centralized BMPs in the local DB.");

            _dbContext.Database.ExecuteSqlRaw("UPDATE dbo.Delineation SET IsVerified = 0 WHERE TreatmentBMPID = {0}", treatmentBMPID.Value);

            Assert.AreEqual(0, ScalarInt($"SELECT COUNT(*) AS Value FROM dbo.vNereidRegionalSubbasinCentralizedBMP WHERE TreatmentBMPID = {treatmentBMPID}"));
        }

        [TestMethod]
        public void LoadingInput_ExposesTheStatusOfTheWqmpTheGroundSitsUnder()
        {
            // every row with a WQMP carries that WQMP's status (the routing in NereidUtilities.LandSurfaceNodeID reads it)
            var mismatches = ScalarInt(@"
                SELECT COUNT(*) AS Value
                FROM dbo.vNereidLoadingInput v
                JOIN dbo.WaterQualityManagementPlan w ON w.WaterQualityManagementPlanID = v.WaterQualityManagementPlanID
                WHERE ISNULL(v.SpatiallyAssociatedWaterQualityManagementPlanStatusID, 0) <> ISNULL(w.WaterQualityManagementPlanStatusID, 0)");
            Assert.AreEqual(0, mismatches);
        }
    }
}
