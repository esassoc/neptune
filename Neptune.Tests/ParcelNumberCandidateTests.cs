using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1132: APN formats a WQMP may print, mapped to the forms the Parcel table stores.
    /// The review wizard's lookup is an exact string match, so an APN copied as "691-101-001"
    /// never resolved to "691-101-01".
    /// </summary>
    [TestClass]
    public class ParcelNumberCandidateTests
    {
        [TestMethod]
        public void ZeroPaddedLastGroup_DropsThePadding()
        {
            CollectionAssert.Contains(Parcels.CandidateParcelNumbers("691-101-001"), "691-101-01");
        }

        [TestMethod]
        public void NoDashes_OffersBothStoredLayouts()
        {
            var candidates = Parcels.CandidateParcelNumbers("69110101");
            CollectionAssert.Contains(candidates, "691-101-01");
            CollectionAssert.Contains(candidates, "691-10-101"); // condominium layout
        }

        [TestMethod]
        public void AlreadyStoredForm_ComesFirst()
        {
            Assert.AreEqual("939-59-269", Parcels.CandidateParcelNumbers(" 939-59-269 ")[0]);
        }

        [TestMethod]
        public void NineDigitsWithoutPaddingZero_IsLeftAlone()
        {
            // Not an OC APN shape; no reformatted guesses.
            CollectionAssert.AreEqual(new[] { "691-101-123" }, Parcels.CandidateParcelNumbers("691-101-123"));
        }

        [TestMethod]
        public void Blank_HasNoCandidates()
        {
            Assert.AreEqual(0, Parcels.CandidateParcelNumbers("  ").Count);
        }
    }
}
