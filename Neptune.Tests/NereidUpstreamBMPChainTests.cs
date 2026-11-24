using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;
using Neptune.Jobs.Services;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155: BMP chains in the model graph when a centralized BMP in the chain is not modeled
    /// (its delineation is provisional). It gets no node, so no edge may point at it or leave it:
    /// the BMP upstream of it flows to its own Regional Subbasin instead.
    /// </summary>
    [TestClass]
    public class NereidUpstreamBMPChainTests
    {
        private const int UpstreamBMPID = 1;
        private const int CentralizedBMPID = 2;
        private const int DownstreamBMPID = 3;
        private const int UpstreamRegionalSubbasinID = 10;
        private const int UpstreamOCSurveyCatchmentID = 1000;

        private static readonly Dictionary<int, int> OCSurveyCatchmentIDsByRegionalSubbasinID = new()
        {
            { UpstreamRegionalSubbasinID, UpstreamOCSurveyCatchmentID },
        };

        private static vNereidBMPColocation Link(int upstreamBMPID, int downstreamBMPID, int? upstreamRegionalSubbasinID = UpstreamRegionalSubbasinID)
        {
            return new vNereidBMPColocation { UpstreamBMPID = upstreamBMPID, DownstreamBMPID = downstreamBMPID, UpstreamRSBID = upstreamRegionalSubbasinID };
        }

        [TestMethod]
        public void AllModeled_LinksEachBMPToTheNext()
        {
            var chain = new List<vNereidBMPColocation> { Link(UpstreamBMPID, CentralizedBMPID), Link(CentralizedBMPID, DownstreamBMPID) };

            NereidService.MakeUpstreamBMPNodesAndEdges(chain, new HashSet<int>(), OCSurveyCatchmentIDsByRegionalSubbasinID, out var edges, out var nodes);

            CollectionAssert.AreEquivalent(new[] { "BMP_1", "BMP_2" }, nodes.Select(x => x.ID).ToList());
            CollectionAssert.AreEquivalent(new[] { "BMP_1>BMP_2", "BMP_2>BMP_3" }, edges.Select(x => $"{x.SourceID}>{x.TargetID}").ToList());
        }

        [TestMethod]
        public void NotModeledCentralizedBMPDownstream_UpstreamFlowsToItsOwnRegionalSubbasin()
        {
            var chain = new List<vNereidBMPColocation> { Link(UpstreamBMPID, CentralizedBMPID) };

            NereidService.MakeUpstreamBMPNodesAndEdges(chain, new HashSet<int> { CentralizedBMPID }, OCSurveyCatchmentIDsByRegionalSubbasinID, out var edges, out var nodes);

            CollectionAssert.AreEquivalent(new[] { "BMP_1" }, nodes.Select(x => x.ID).ToList());
            Assert.AreEqual(1, edges.Count);
            Assert.AreEqual(NereidUtilities.RegionalSubbasinNodeID(UpstreamOCSurveyCatchmentID), edges.Single().TargetID);
        }

        [TestMethod]
        public void NotModeledCentralizedBMPInTheMiddle_NothingPointsAtItOrLeavesIt()
        {
            var chain = new List<vNereidBMPColocation> { Link(UpstreamBMPID, CentralizedBMPID), Link(CentralizedBMPID, DownstreamBMPID) };

            NereidService.MakeUpstreamBMPNodesAndEdges(chain, new HashSet<int> { CentralizedBMPID }, OCSurveyCatchmentIDsByRegionalSubbasinID, out var edges, out var nodes);

            var notModeledNodeID = NereidUtilities.TreatmentBMPNodeID(CentralizedBMPID);
            Assert.IsFalse(nodes.Any(x => x.ID == notModeledNodeID));
            Assert.IsFalse(edges.Any(x => x.SourceID == notModeledNodeID || x.TargetID == notModeledNodeID));
        }

        [TestMethod]
        public void NotModeledCentralizedBMPDownstream_UpstreamWithNoModeledRegionalSubbasin_IsLeftOut()
        {
            var chain = new List<vNereidBMPColocation> { Link(UpstreamBMPID, CentralizedBMPID, upstreamRegionalSubbasinID: null) };

            NereidService.MakeUpstreamBMPNodesAndEdges(chain, new HashSet<int> { CentralizedBMPID }, OCSurveyCatchmentIDsByRegionalSubbasinID, out var edges, out var nodes);

            Assert.AreEqual(0, nodes.Count);
            Assert.AreEqual(0, edges.Count);
        }
    }
}
