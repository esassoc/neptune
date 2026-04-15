using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    [TestClass]
    public class WaterQualityManagementPlanExtractionResultTests
    {
        private const int EditorPersonID = 101;
        private const int ApproverPersonID = 202;
        private static readonly DateTime FrozenNow = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);

        private static WaterQualityManagementPlanExtractionResult NewExtractionResult() => new()
        {
            WaterQualityManagementPlanID = 1,
            WaterQualityManagementPlanDocumentID = 1,
            ExtractionResultJson = "{\"version\":1}",
            ExtractedAt = FrozenNow.AddHours(-1),
        };

        [TestMethod]
        public void ApplyDraftOverlay_SetsOverlayAndStampsEditor()
        {
            var entity = NewExtractionResult();
            var draftJson = "{\"Jurisdiction\":{\"Value\":\"Costa Mesa\"}}";

            entity.ApplyDraftOverlay(draftJson, EditorPersonID, FrozenNow);

            Assert.AreEqual(draftJson, entity.DraftOverlayJson);
            Assert.AreEqual(EditorPersonID, entity.DraftUpdatedByPersonID);
            Assert.AreEqual(FrozenNow, entity.DraftUpdatedDate);
        }

        [TestMethod]
        public void ApplyDraftOverlay_OverwritesPreviousDraft()
        {
            var entity = NewExtractionResult();
            entity.ApplyDraftOverlay("{\"old\":true}", EditorPersonID, FrozenNow.AddMinutes(-5));

            var newJson = "{\"new\":true}";
            entity.ApplyDraftOverlay(newJson, EditorPersonID + 1, FrozenNow);

            Assert.AreEqual(newJson, entity.DraftOverlayJson);
            Assert.AreEqual(EditorPersonID + 1, entity.DraftUpdatedByPersonID);
            Assert.AreEqual(FrozenNow, entity.DraftUpdatedDate);
        }

        [TestMethod]
        public void ApplyDraftOverlay_OnApprovedEntity_Throws()
        {
            var entity = NewExtractionResult();
            entity.Approve(ApproverPersonID, FrozenNow.AddMinutes(-1));

            Assert.Throws<InvalidOperationException>(() =>
                entity.ApplyDraftOverlay("{}", EditorPersonID, FrozenNow));
        }

        [TestMethod]
        public void ClearDraftOverlay_RemovesDraftFieldsOnly()
        {
            var entity = NewExtractionResult();
            entity.ApplyDraftOverlay("{}", EditorPersonID, FrozenNow);

            entity.ClearDraftOverlay();

            Assert.IsNull(entity.DraftOverlayJson);
            Assert.IsNull(entity.DraftUpdatedByPersonID);
            Assert.IsNull(entity.DraftUpdatedDate);
        }

        [TestMethod]
        public void ClearDraftOverlay_DoesNotAffectApprovalState()
        {
            var entity = NewExtractionResult();
            entity.Approve(ApproverPersonID, FrozenNow);

            entity.ClearDraftOverlay();

            Assert.AreEqual(ApproverPersonID, entity.ApprovedByPersonID);
            Assert.AreEqual(FrozenNow, entity.ApprovedDate);
        }

        [TestMethod]
        public void Approve_StampsApproverAndClearsDraft()
        {
            var entity = NewExtractionResult();
            entity.ApplyDraftOverlay("{}", EditorPersonID, FrozenNow.AddMinutes(-1));

            entity.Approve(ApproverPersonID, FrozenNow);

            Assert.AreEqual(ApproverPersonID, entity.ApprovedByPersonID);
            Assert.AreEqual(FrozenNow, entity.ApprovedDate);
            Assert.IsNull(entity.DraftOverlayJson);
            Assert.IsNull(entity.DraftUpdatedByPersonID);
            Assert.IsNull(entity.DraftUpdatedDate);
        }

        [TestMethod]
        public void Approve_WithoutDraft_StillSucceeds()
        {
            var entity = NewExtractionResult();

            entity.Approve(ApproverPersonID, FrozenNow);

            Assert.AreEqual(ApproverPersonID, entity.ApprovedByPersonID);
            Assert.AreEqual(FrozenNow, entity.ApprovedDate);
        }

        [TestMethod]
        public void Approve_OnAlreadyApprovedEntity_Throws()
        {
            var entity = NewExtractionResult();
            entity.Approve(ApproverPersonID, FrozenNow.AddMinutes(-10));

            Assert.Throws<InvalidOperationException>(() =>
                entity.Approve(ApproverPersonID + 1, FrozenNow));
        }
    }
}
