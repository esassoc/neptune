using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.WebMvc.Common;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1051 Status filter on the trash result calculation path: only Active WQMPs
    /// contribute trash-capture credit. A TGU joined to a Draft (or Inactive) WQMP must behave as
    /// if it had no WQMP association — the legal record gate is independent of data completeness.
    /// </summary>
    [TestClass]
    public class TrashGeneratingUnitHelperStatusFilterTests
    {
        private static TrashGeneratingUnit TguWithWqmpStatus(int? statusID, int trashCaptureStatusTypeID)
        {
            return new TrashGeneratingUnit
            {
                WaterQualityManagementPlan = new WaterQualityManagementPlan
                {
                    WaterQualityManagementPlanStatusID = statusID,
                    TrashCaptureStatusTypeID = trashCaptureStatusTypeID,
                },
            };
        }

        [TestMethod]
        public void IsFullTrashCapture_ActiveWqmpWithFullCapture_True()
        {
            var tgu = TguWithWqmpStatus((int)WaterQualityManagementPlanStatusEnum.Active, (int)TrashCaptureStatusTypeEnum.Full);
            Assert.IsTrue(tgu.IsFullTrashCapture());
        }

        [TestMethod]
        public void IsFullTrashCapture_DraftWqmpWithFullCapture_False()
        {
            var tgu = TguWithWqmpStatus((int)WaterQualityManagementPlanStatusEnum.Draft, (int)TrashCaptureStatusTypeEnum.Full);
            Assert.IsFalse(tgu.IsFullTrashCapture());
        }

        [TestMethod]
        public void IsFullTrashCapture_InactiveWqmpWithFullCapture_False()
        {
            var tgu = TguWithWqmpStatus((int)WaterQualityManagementPlanStatusEnum.Inactive, (int)TrashCaptureStatusTypeEnum.Full);
            Assert.IsFalse(tgu.IsFullTrashCapture());
        }

        [TestMethod]
        public void IsFullTrashCapture_NullStatusWqmpWithFullCapture_False()
        {
            var tgu = TguWithWqmpStatus(null, (int)TrashCaptureStatusTypeEnum.Full);
            Assert.IsFalse(tgu.IsFullTrashCapture());
        }

        [TestMethod]
        public void IsFullTrashCapture_NoWqmpAndNoDelineation_False()
        {
            var tgu = new TrashGeneratingUnit();
            Assert.IsFalse(tgu.IsFullTrashCapture());
        }
    }
}
