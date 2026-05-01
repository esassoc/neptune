using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1051 promotion gate. <see cref="WaterQualityManagementPlans.ValidateForPromote"/>
    /// is the legal-record completeness check that runs before flipping a Draft WQMP to Active —
    /// missing fields are surfaced to the SPA so the reviewer can fill them in before retrying.
    /// </summary>
    [TestClass]
    public class WaterQualityManagementPlansValidateForPromoteTests
    {
        private static WaterQualityManagementPlan FullyPopulated() => new()
        {
            WaterQualityManagementPlanName = "Test WQMP",
            HydrologicSubareaID = 1,
            WaterQualityManagementPlanLandUseID = 1,
            WaterQualityManagementPlanPriorityID = 1,
            WaterQualityManagementPlanDevelopmentTypeID = 1,
            WaterQualityManagementPlanPermitTermID = 1,
            HydromodificationAppliesTypeID = 1,
            RecordNumber = "REC-001",
            RecordedWQMPAreaInAcres = 5.5m,
            ApprovalDate = new DateTime(2024, 1, 1),
            DateOfConstruction = new DateTime(2024, 6, 1),
            MaintenanceContactName = "Jane Doe",
            MaintenanceContactOrganization = "Acme Corp",
        };

        [TestMethod]
        public void FullyPopulated_NoMissing()
        {
            var missing = WaterQualityManagementPlans.ValidateForPromote(FullyPopulated());
            Assert.AreEqual(0, missing.Count);
        }

        [TestMethod]
        public void NullName_FlagsName()
        {
            var entity = FullyPopulated();
            entity.WaterQualityManagementPlanName = null;
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("WQMP Name"));
        }

        [TestMethod]
        public void WhitespaceName_FlagsName()
        {
            var entity = FullyPopulated();
            entity.WaterQualityManagementPlanName = "   ";
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("WQMP Name"));
        }

        [TestMethod]
        public void NullHydrologicSubarea_FlagsHydrologicSubarea()
        {
            var entity = FullyPopulated();
            entity.HydrologicSubareaID = null;
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Hydrologic Subarea"));
        }

        [TestMethod]
        public void NullRecordedAcreage_FlagsAcreage()
        {
            var entity = FullyPopulated();
            entity.RecordedWQMPAreaInAcres = null;
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Recorded WQMP Area (Acres)"));
        }

        [TestMethod]
        public void NullApprovalDate_FlagsApprovalDate()
        {
            var entity = FullyPopulated();
            entity.ApprovalDate = null;
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Approval Date"));
        }

        [TestMethod]
        public void NullMaintenanceContactOrganization_FlagsOrganization()
        {
            var entity = FullyPopulated();
            entity.MaintenanceContactOrganization = null;
            Assert.IsTrue(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Maintenance Contact Organization"));
        }

        [TestMethod]
        public void EmptyEntity_FlagsAllRequiredFields()
        {
            var missing = WaterQualityManagementPlans.ValidateForPromote(new WaterQualityManagementPlan
            {
                WaterQualityManagementPlanName = null,
                RecordNumber = null,
                MaintenanceContactName = null,
                MaintenanceContactOrganization = null,
            });
            // Every single field on FullyPopulated maps to one missing-field entry.
            Assert.AreEqual(13, missing.Count);
        }

        [TestMethod]
        public void MultipleMissing_ReturnsAllOfThem()
        {
            var entity = FullyPopulated();
            entity.RecordNumber = null;
            entity.ApprovalDate = null;
            entity.HydromodificationAppliesTypeID = null;
            var missing = WaterQualityManagementPlans.ValidateForPromote(entity);
            Assert.AreEqual(3, missing.Count);
            Assert.IsTrue(missing.Contains("Record Number"));
            Assert.IsTrue(missing.Contains("Approval Date"));
            Assert.IsTrue(missing.Contains("Hydromodification Applies"));
        }
    }
}
