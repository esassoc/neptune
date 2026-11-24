using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1155: which simple BMPs the solve sends to the model as treatment sites. A blank
    /// % Retained counts as zero, so it no longer keeps a simple BMP (and with it the whole WQMP)
    /// out of the model; a blank % of Site Treated or % Captured still does.
    /// </summary>
    [TestClass]
    public class QuickBMPsIsSentToModelTests
    {
        private static QuickBMP SimpleBMP(decimal? percentOfSiteTreated, decimal? percentCaptured, decimal? percentRetained, bool isModeledType = true)
        {
            return new QuickBMP
            {
                PercentOfSiteTreated = percentOfSiteTreated,
                PercentCaptured = percentCaptured,
                PercentRetained = percentRetained,
                TreatmentBMPType = new TreatmentBMPType { IsAnalyzedInModelingModule = isModeledType },
            };
        }

        [TestMethod]
        public void AllThreePercentages_IsSent()
        {
            Assert.IsTrue(QuickBMPs.IsSentToModelCompiled(SimpleBMP(100, 80, 40)));
        }

        [TestMethod]
        public void BlankPercentRetained_IsSent()
        {
            Assert.IsTrue(QuickBMPs.IsSentToModelCompiled(SimpleBMP(100, 80, null)));
        }

        [TestMethod]
        public void BlankPercentCaptured_IsNotSent()
        {
            Assert.IsFalse(QuickBMPs.IsSentToModelCompiled(SimpleBMP(100, null, 40)));
        }

        [TestMethod]
        public void BlankPercentOfSiteTreated_IsNotSent()
        {
            Assert.IsFalse(QuickBMPs.IsSentToModelCompiled(SimpleBMP(null, 80, 40)));
        }

        [TestMethod]
        public void NonModeledBMPType_IsNotSent()
        {
            Assert.IsFalse(QuickBMPs.IsSentToModelCompiled(SimpleBMP(100, 80, 40, isModeledType: false)));
        }
    }
}
