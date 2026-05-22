using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1051: Pure-decision matrix for <see cref="WaterQualityManagementPlans.ClassifyStatusTransition"/>.
    /// The function decides what side effects a Status transition needs (none, cleanup-on-leave, or
    /// dirty-on-enter); the DB-touching <see cref="WaterQualityManagementPlans.HandleStatusTransitionAsync"/>
    /// dispatches on this classification. Status semantics (per Kathleen 2026-04-30):
    /// Active = binding legal record (the only state that participates in modeling),
    /// Draft = transcription in progress, Inactive = agreement no longer in force.
    /// </summary>
    [TestClass]
    public class WaterQualityManagementPlansClassifyStatusTransitionTests
    {
        private const int Active = (int)WaterQualityManagementPlanStatusEnum.Active;
        private const int Inactive = (int)WaterQualityManagementPlanStatusEnum.Inactive;
        private const int Draft = (int)WaterQualityManagementPlanStatusEnum.Draft;

        [TestMethod]
        public void SameStatus_Active_None()
        {
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(Active, Active));
        }

        [TestMethod]
        public void SameStatus_Draft_None()
        {
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(Draft, Draft));
        }

        [TestMethod]
        public void SameStatus_Inactive_None()
        {
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(Inactive, Inactive));
        }

        [TestMethod]
        public void ActiveToInactive_LeavingActive()
        {
            Assert.AreEqual(StatusTransitionEffect.LeavingActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(Active, Inactive));
        }

        [TestMethod]
        public void ActiveToDraft_LeavingActive()
        {
            // Data-quality rollback case — Kathleen confirmed this is a valid transition
            // (e.g., transcription error discovered after promotion).
            Assert.AreEqual(StatusTransitionEffect.LeavingActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(Active, Draft));
        }

        [TestMethod]
        public void InactiveToActive_EnteringActive()
        {
            Assert.AreEqual(StatusTransitionEffect.EnteringActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(Inactive, Active));
        }

        [TestMethod]
        public void DraftToActive_EnteringActive()
        {
            // The Promote endpoint path — Draft transcription becomes the binding legal record.
            Assert.AreEqual(StatusTransitionEffect.EnteringActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(Draft, Active));
        }

        [TestMethod]
        public void DraftToInactive_None()
        {
            // Neither state is in the modeling graph — no cleanup or dirtying needed.
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(Draft, Inactive));
        }

        [TestMethod]
        public void InactiveToDraft_None()
        {
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(Inactive, Draft));
        }

        [TestMethod]
        public void NullOldStatusToActive_EnteringActive()
        {
            // Defensive: a freshly created WQMP record could have a null old status if a caller
            // captures it before the row's status was set. Treat null as "not yet Active."
            Assert.AreEqual(StatusTransitionEffect.EnteringActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(null, Active));
        }

        [TestMethod]
        public void NullOldStatusToDraft_None()
        {
            Assert.AreEqual(StatusTransitionEffect.None,
                WaterQualityManagementPlans.ClassifyStatusTransition(null, Draft));
        }

        [TestMethod]
        public void ActiveToNullNewStatus_LeavingActive()
        {
            // Symmetric defensive case (in practice the entity's status column is NOT NULL,
            // but the classifier handles the null variant rather than blowing up).
            Assert.AreEqual(StatusTransitionEffect.LeavingActive,
                WaterQualityManagementPlans.ClassifyStatusTransition(Active, null));
        }
    }
}
