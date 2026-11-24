using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155: which model node a piece of ground (a Load Generating Unit) sends its load to.
    /// Nereid silently discards a land surface whose node is not in the graph, so ground must never
    /// be routed to a node the graph does not build: a WQMP that is not Active, a Detailed WQMP, or a
    /// provisional delineation that is not one of the project's own.
    /// </summary>
    [TestClass]
    public class NereidLandSurfaceNodeIDTests
    {
        private const int OCSurveyCatchmentID = 42;
        private const int WaterQualityManagementPlanID = 7;
        private const int DelineationID = 99;

        private const int Active = (int)WaterQualityManagementPlanStatusEnum.Active;
        private const int Inactive = (int)WaterQualityManagementPlanStatusEnum.Inactive;
        private const int Draft = (int)WaterQualityManagementPlanStatusEnum.Draft;
        private const int Simplified = (int)WaterQualityManagementPlanModelingApproachEnum.Simplified;
        private const int Detailed = (int)WaterQualityManagementPlanModelingApproachEnum.Detailed;

        private static readonly string RegionalSubbasinNodeID = NereidUtilities.RegionalSubbasinNodeID(OCSurveyCatchmentID);
        private static readonly string WaterQualityManagementPlanNodeID = NereidUtilities.WaterQualityManagementPlanNodeID(WaterQualityManagementPlanID, OCSurveyCatchmentID);
        private static readonly string DelineationNodeID = NereidUtilities.DelineationNodeID(DelineationID);

        private static vNereidLoadingInput GroundUnderWqmp(int? statusID, int modelingApproachID = Simplified)
        {
            return new vNereidLoadingInput
            {
                OCSurveyCatchmentID = OCSurveyCatchmentID,
                WaterQualityManagementPlanID = WaterQualityManagementPlanID,
                SpatiallyAssociatedModelingApproach = modelingApproachID,
                SpatiallyAssociatedWaterQualityManagementPlanStatusID = statusID,
            };
        }

        [TestMethod]
        public void NoWqmpNoDelineation_GoesToRegionalSubbasin()
        {
            var ground = new vNereidLoadingInput { OCSurveyCatchmentID = OCSurveyCatchmentID };
            Assert.AreEqual(RegionalSubbasinNodeID, NereidUtilities.LandSurfaceNodeID(ground));
        }

        [TestMethod]
        public void ActiveSimplifiedWqmp_GoesToWqmp()
        {
            Assert.AreEqual(WaterQualityManagementPlanNodeID, NereidUtilities.LandSurfaceNodeID(GroundUnderWqmp(Active)));
        }

        [TestMethod]
        [DataRow(Inactive)]
        [DataRow(Draft)]
        [DataRow(null)]
        public void NonActiveWqmp_GoesToRegionalSubbasin(int? statusID)
        {
            Assert.AreEqual(RegionalSubbasinNodeID, NereidUtilities.LandSurfaceNodeID(GroundUnderWqmp(statusID)));
        }

        [TestMethod]
        public void ActiveDetailedWqmp_GoesToRegionalSubbasin()
        {
            Assert.AreEqual(RegionalSubbasinNodeID, NereidUtilities.LandSurfaceNodeID(GroundUnderWqmp(Active, Detailed)));
        }

        [TestMethod]
        public void VerifiedDelineation_GoesToDelineation_EvenOverAWqmp()
        {
            var ground = GroundUnderWqmp(Active);
            ground.DelineationID = DelineationID;
            ground.DelineationIsVerified = true;
            Assert.AreEqual(DelineationNodeID, NereidUtilities.LandSurfaceNodeID(ground));
        }

        [TestMethod]
        public void ProvisionalDelineation_FallsBackToActiveWqmp()
        {
            var ground = GroundUnderWqmp(Active);
            ground.DelineationID = DelineationID;
            ground.DelineationIsVerified = false;
            Assert.AreEqual(WaterQualityManagementPlanNodeID, NereidUtilities.LandSurfaceNodeID(ground));
        }

        [TestMethod]
        public void ProvisionalDelineation_OverNonActiveWqmp_GoesToRegionalSubbasin()
        {
            var ground = GroundUnderWqmp(Inactive);
            ground.DelineationID = DelineationID;
            ground.DelineationIsVerified = false;
            Assert.AreEqual(RegionalSubbasinNodeID, NereidUtilities.LandSurfaceNodeID(ground));
        }

        [TestMethod]
        public void ProvisionalDelineation_OfTheProjectBeingSolved_GoesToDelineation()
        {
            var ground = new vNereidLoadingInput
            {
                OCSurveyCatchmentID = OCSurveyCatchmentID,
                DelineationID = DelineationID,
                DelineationIsVerified = false,
            };
            Assert.AreEqual(DelineationNodeID, NereidUtilities.LandSurfaceNodeID(ground, new List<int> { DelineationID }));
        }

        [TestMethod]
        public void VerifiedDelineation_OnASimplifiedWqmpBmp_IsNotModeled()
        {
            var ground = new vNereidLoadingInput
            {
                OCSurveyCatchmentID = OCSurveyCatchmentID,
                DelineationID = DelineationID,
                DelineationIsVerified = true,
                RelationallyAssociatedModelingApproach = Simplified,
            };
            Assert.AreEqual(RegionalSubbasinNodeID, NereidUtilities.LandSurfaceNodeID(ground));
        }
    }
}
