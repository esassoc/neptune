using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1051 promotion gate. <see cref="WaterQualityManagementPlans.ValidateForPromote"/>
    /// is the legal-record completeness check that runs before flipping a Draft WQMP to Active —
    /// missing fields are surfaced to the SPA so the reviewer can fill them in before retrying.
    ///
    /// Per the post-NPT-1051 loosening (dbb7d2567), validation mirrors the Basics editor modal's
    /// required-field set. Only WQMP Name needs an explicit check here: Jurisdiction,
    /// ModelingApproach, and TrashCaptureStatus are also modal-required but are NOT NULL on the
    /// entity and so are guaranteed to be populated. Everything else (record numbers, dates,
    /// contact info, hydromod) is optional in the modal and therefore optional at Promote.
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
        public void NullHydrologicSubarea_DoesNotFlag_OptionalAtPromote()
        {
            var entity = FullyPopulated();
            entity.HydrologicSubareaID = null;
            Assert.IsFalse(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Hydrologic Subarea"));
        }

        [TestMethod]
        public void NullRecordedAcreage_DoesNotFlag_OptionalAtPromote()
        {
            var entity = FullyPopulated();
            entity.RecordedWQMPAreaInAcres = null;
            Assert.IsFalse(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Recorded WQMP Area (Acres)"));
        }

        [TestMethod]
        public void NullApprovalDate_DoesNotFlag_OptionalAtPromote()
        {
            var entity = FullyPopulated();
            entity.ApprovalDate = null;
            Assert.IsFalse(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Approval Date"));
        }

        [TestMethod]
        public void NullMaintenanceContactOrganization_DoesNotFlag_OptionalAtPromote()
        {
            var entity = FullyPopulated();
            entity.MaintenanceContactOrganization = null;
            Assert.IsFalse(WaterQualityManagementPlans.ValidateForPromote(entity).Contains("Maintenance Contact Organization"));
        }

        [TestMethod]
        public void EmptyEntity_FlagsOnlyName()
        {
            var missing = WaterQualityManagementPlans.ValidateForPromote(new WaterQualityManagementPlan
            {
                WaterQualityManagementPlanName = null,
                RecordNumber = null,
                MaintenanceContactName = null,
                MaintenanceContactOrganization = null,
            });
            // Post-loosening, only Name is explicitly required at Promote — everything else on
            // FullyPopulated is modal-optional and therefore Promote-optional.
            Assert.AreEqual(1, missing.Count);
            Assert.IsTrue(missing.Contains("WQMP Name"));
        }

        [TestMethod]
        public void MultipleMissingOptional_FlagsNone()
        {
            var entity = FullyPopulated();
            entity.RecordNumber = null;
            entity.ApprovalDate = null;
            entity.HydromodificationAppliesTypeID = null;
            // All three are modal-optional, so Promote should accept the entity as-is.
            var missing = WaterQualityManagementPlans.ValidateForPromote(entity);
            Assert.AreEqual(0, missing.Count);
        }
    }
}
