using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155: the views that decide what goes into the Load Generating Unit overlay and the model.
    /// Only what is modeled may take ground: a verified delineation (or a project's own), an Active
    /// WQMP. Integration tests against the local DB, rolled back; each one takes a record that is an
    /// input today, changes its status and checks it drops out (or stays). Requires the updated views
    /// to be deployed to the local DB.
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

        [TestMethod]
        public void DelineationLGUInput_DropsADelineationThatBecomesProvisional()
        {
            var delineationID = FirstIDOrNull("SELECT TOP 1 DelinID AS Value FROM dbo.vPyQgisDelineationLGUInput");
            if (delineationID == null) Assert.Inconclusive("No delineation LGU inputs in the local DB.");

            SetDelineationVerified(delineationID.Value, false);

            Assert.AreEqual(0, ScalarInt($"SELECT COUNT(*) AS Value FROM dbo.vPyQgisDelineationLGUInput WHERE DelinID = {delineationID}"));
        }

        [TestMethod]
        public void ProjectDelineationLGUInput_DropsAnExistingDelineationThatBecomesProvisional()
        {
            var delineationID = FirstIDOrNull("SELECT TOP 1 DelinID AS Value FROM dbo.vPyQgisProjectDelineationLGUInput WHERE ProjectID IS NULL");
            if (delineationID == null) Assert.Inconclusive("No existing delineation PLGU inputs in the local DB.");

            SetDelineationVerified(delineationID.Value, false);

            Assert.AreEqual(0, ScalarInt($"SELECT COUNT(*) AS Value FROM dbo.vPyQgisProjectDelineationLGUInput WHERE DelinID = {delineationID}"));
        }

        [TestMethod]
        public void ProjectDelineationLGUInput_KeepsAProjectsOwnProvisionalDelineation()
        {
            var delineationID = FirstIDOrNull("SELECT TOP 1 DelinID AS Value FROM dbo.vPyQgisProjectDelineationLGUInput WHERE ProjectID IS NOT NULL");
            if (delineationID == null) Assert.Inconclusive("No project delineation PLGU inputs in the local DB.");

            SetDelineationVerified(delineationID.Value, false);

            Assert.AreEqual(1, ScalarInt($"SELECT COUNT(*) AS Value FROM dbo.vPyQgisProjectDelineationLGUInput WHERE DelinID = {delineationID}"));
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
