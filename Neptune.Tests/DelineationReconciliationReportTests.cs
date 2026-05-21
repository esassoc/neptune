using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1064 — Covers the post-materialize DelineationType display-name resolution
    /// used by the SPA reconciliation report endpoints. The SQL-side projections
    /// (Delineation.DtoProjections.cs) emit the ID; this resolver fills the name from
    /// the static enum lookup so we don't pay a JOIN against the lookup table.
    /// </summary>
    [TestClass]
    public class DelineationReconciliationReportTests
    {
        [TestMethod]
        public void ResolveDelineationTypeNames_FillsCentralizedAndDistributedNames()
        {
            var dtos = new List<DelineationReconciliationDiscrepancyGridDto>
            {
                new() { DelineationTypeID = (int)DelineationTypeEnum.Centralized },
                new() { DelineationTypeID = (int)DelineationTypeEnum.Distributed },
            };

            Delineations.ResolveDelineationTypeNames(dtos);

            Assert.AreEqual("Centralized", dtos[0].DelineationTypeName);
            Assert.AreEqual("Distributed", dtos[1].DelineationTypeName);
        }

        [TestMethod]
        public void ResolveDelineationTypeNames_TolerantOfEmptyList()
        {
            var dtos = new List<DelineationReconciliationDiscrepancyGridDto>();
            Delineations.ResolveDelineationTypeNames(dtos);
            Assert.AreEqual(0, dtos.Count);
        }
    }
}
