using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Entities.AI;

namespace Neptune.Tests
{
    /// <summary>
    /// Validates the FieldKey → property setter map used by the WQMP AI review
    /// workflow's per-field endpoint (NPT-1020). Each test exercises one row of
    /// the dictionary so a regression in one field doesn't hide as a generic
    /// "didn't write" — and the unknown / non-rejectable / parse-error paths
    /// each have their own assertion.
    /// </summary>
    [TestClass]
    public class WqmpExtractionFieldApplierTests
    {
        private static WaterQualityManagementPlan NewWqmp() => new()
        {
            WaterQualityManagementPlanID = 1,
            StormwaterJurisdictionID = 999,
            TrashCaptureStatusTypeID = 999,
            WaterQualityManagementPlanModelingApproachID = 1,
        };

        [TestMethod]
        public void Apply_AcceptText_SetsTrimmedValue()
        {
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "WaterQualityManagementPlanName", "  Site A  ", "accept");
            Assert.AreEqual("Site A", wqmp.WaterQualityManagementPlanName);
        }

        [TestMethod]
        public void Apply_RejectText_NullsValue()
        {
            var wqmp = NewWqmp();
            wqmp.WaterQualityManagementPlanName = "Old";
            WqmpExtractionFieldApplier.Apply(wqmp, "WaterQualityManagementPlanName", null, "reject");
            Assert.IsNull(wqmp.WaterQualityManagementPlanName);
        }

        [TestMethod]
        public void Apply_AcceptRequiredLookup_ParsesId()
        {
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "Jurisdiction", "37", "accept");
            Assert.AreEqual(37, wqmp.StormwaterJurisdictionID);
        }

        [TestMethod]
        public void Apply_RejectRequiredField_Throws()
        {
            var wqmp = NewWqmp();
            // Jurisdiction is non-nullable on the entity — workflow must refuse to clear it.
            Assert.Throws<WqmpExtractionFieldApplier.FieldNotRejectableException>(() =>
                WqmpExtractionFieldApplier.Apply(wqmp, "Jurisdiction", null, "reject"));
        }

        [TestMethod]
        public void Apply_AcceptNullableLookup_ParsesId()
        {
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "WaterQualityManagementPlanPriority", "2", "accept");
            Assert.AreEqual(2, wqmp.WaterQualityManagementPlanPriorityID);
        }

        [TestMethod]
        public void Apply_RejectNullableLookup_Nulls()
        {
            var wqmp = NewWqmp();
            wqmp.WaterQualityManagementPlanPriorityID = 2;
            WqmpExtractionFieldApplier.Apply(wqmp, "WaterQualityManagementPlanPriority", null, "reject");
            Assert.IsNull(wqmp.WaterQualityManagementPlanPriorityID);
        }

        [TestMethod]
        public void Apply_AcceptDecimal_Parses()
        {
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "RecordedWQMPAreaInAcres", "12.5", "accept");
            Assert.AreEqual(12.5m, wqmp.RecordedWQMPAreaInAcres);
        }

        [TestMethod]
        public void Apply_AcceptIsoDate_Parses()
        {
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "ApprovalDate", "2026-04-15", "accept");
            Assert.AreEqual(new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc), wqmp.ApprovalDate);
        }

        [TestMethod]
        public void Apply_AcceptInvalidDate_Throws()
        {
            var wqmp = NewWqmp();
            Assert.Throws<WqmpExtractionFieldApplier.InvalidFieldValueException>(() =>
                WqmpExtractionFieldApplier.Apply(wqmp, "ApprovalDate", "not a date", "accept"));
        }

        [TestMethod]
        public void Apply_AcceptInvalidInt_Throws()
        {
            var wqmp = NewWqmp();
            Assert.Throws<WqmpExtractionFieldApplier.InvalidFieldValueException>(() =>
                WqmpExtractionFieldApplier.Apply(wqmp, "Jurisdiction", "abc", "accept"));
        }

        [TestMethod]
        public void Apply_UnknownFieldKey_Throws()
        {
            var wqmp = NewWqmp();
            Assert.Throws<WqmpExtractionFieldApplier.UnknownFieldKeyException>(() =>
                WqmpExtractionFieldApplier.Apply(wqmp, "MadeUpField", "x", "accept"));
        }

        [TestMethod]
        public void IsKnownFieldKey_KnownAndUnknown()
        {
            Assert.IsTrue(WqmpExtractionFieldApplier.IsKnownFieldKey("Jurisdiction"));
            Assert.IsTrue(WqmpExtractionFieldApplier.IsKnownFieldKey("MaintenanceContactZip"));
            Assert.IsFalse(WqmpExtractionFieldApplier.IsKnownFieldKey("NotARealField"));
        }

        [TestMethod]
        public void Apply_EditAction_BehavesLikeAccept()
        {
            // The DraftOverlayJson distinguishes accepted vs edited so the Review summary
            // can render the right pill, but the entity write path is identical: parse + set.
            var wqmp = NewWqmp();
            WqmpExtractionFieldApplier.Apply(wqmp, "MaintenanceContactZip", "92626", "edit");
            Assert.AreEqual("92626", wqmp.MaintenanceContactZip);
        }

        [TestMethod]
        public void Apply_AllNullableTextFields_NullOnReject()
        {
            // Spot-check a representative sweep — all the contact fields should null on reject.
            var wqmp = NewWqmp();
            wqmp.MaintenanceContactName = "x";
            wqmp.MaintenanceContactOrganization = "x";
            wqmp.MaintenanceContactPhone = "x";
            wqmp.MaintenanceContactAddress1 = "x";
            wqmp.MaintenanceContactCity = "x";
            wqmp.RecordNumber = "x";

            foreach (var key in new[] {
                "MaintenanceContactName", "MaintenanceContactOrganization",
                "MaintenanceContactPhone", "MaintenanceContactAddress1",
                "MaintenanceContactCity", "RecordNumber",
            })
            {
                WqmpExtractionFieldApplier.Apply(wqmp, key, null, "reject");
            }

            Assert.IsNull(wqmp.MaintenanceContactName);
            Assert.IsNull(wqmp.MaintenanceContactOrganization);
            Assert.IsNull(wqmp.MaintenanceContactPhone);
            Assert.IsNull(wqmp.MaintenanceContactAddress1);
            Assert.IsNull(wqmp.MaintenanceContactCity);
            Assert.IsNull(wqmp.RecordNumber);
        }
    }
}
