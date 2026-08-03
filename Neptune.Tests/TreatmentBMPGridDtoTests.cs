using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1116 — the Treatment BMP list "Status" column maps the InventoryIsVerified flag on the
    /// vTreatmentBMPDetailed view to the plain-text "Verified"/"Provisional" values. DB-independent.
    /// </summary>
    [TestClass]
    public class TreatmentBMPGridDtoTests
    {
        [TestMethod]
        public void AsGridDto_VerifiedInventory_MapsToVerified()
        {
            var entity = new vTreatmentBMPDetailed { InventoryIsVerified = true };
            Assert.AreEqual("Verified", entity.AsGridDto().InventoryStatus);
        }

        [TestMethod]
        public void AsGridDto_UnverifiedInventory_MapsToProvisional()
        {
            var entity = new vTreatmentBMPDetailed { InventoryIsVerified = false };
            Assert.AreEqual("Provisional", entity.AsGridDto().InventoryStatus);
        }
    }
}
