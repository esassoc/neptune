using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the validation helper shared by the manual <c>MergeQuickBMPs</c> endpoint
    /// and the AI-extraction approve endpoint (NPT-1047). Both call sites must enforce
    /// the same rules so that BMPs created via AI approval can't bypass guards the
    /// manual UI applies.
    /// </summary>
    [TestClass]
    public class QuickBMPsValidateTests
    {
        private static QuickBMPUpsertDto NewBMP(
            string name = "BMP-1",
            int? typeID = 1,
            int? count = 1,
            decimal? siteTreated = 25,
            decimal? captured = 80,
            decimal? retained = 50,
            string? note = null) => new()
        {
            QuickBMPName = name,
            TreatmentBMPTypeID = typeID,
            NumberOfIndividualBMPs = count,
            PercentOfSiteTreated = siteTreated,
            PercentCaptured = captured,
            PercentRetained = retained,
            QuickBMPNote = note,
        };

        [TestMethod]
        public void Validate_NullList_Succeeds()
        {
            Assert.IsNull(QuickBMPs.Validate(null));
        }

        [TestMethod]
        public void Validate_EmptyList_Succeeds()
        {
            Assert.IsNull(QuickBMPs.Validate(new List<QuickBMPUpsertDto>()));
        }

        [TestMethod]
        public void Validate_HappyPath_Succeeds()
        {
            var bmps = new List<QuickBMPUpsertDto>
            {
                NewBMP(name: "BMP-1", siteTreated: 40, captured: 90, retained: 60),
                NewBMP(name: "BMP-2", siteTreated: 30, captured: 70, retained: 70),
            };

            Assert.IsNull(QuickBMPs.Validate(bmps));
        }

        [TestMethod]
        public void Validate_DuplicateNames_ReturnsError()
        {
            var bmps = new List<QuickBMPUpsertDto>
            {
                NewBMP(name: "Bioretention A"),
                NewBMP(name: "Bioretention A"),
            };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "Duplicate");
            StringAssert.Contains(result, "Bioretention A");
        }

        [TestMethod]
        public void Validate_NoteOverMaxLength_ReturnsError()
        {
            var note = new string('x', QuickBMP.FieldLengths.QuickBMPNote + 1);
            var bmps = new List<QuickBMPUpsertDto> { NewBMP(name: "BMP-X", note: note) };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "BMP-X");
            StringAssert.Contains(result, "note exceeds");
        }

        [TestMethod]
        public void Validate_RetainedExceedsCaptured_ReturnsError()
        {
            // Anything captured but not retained is treated and discharged — retained
            // can never exceed captured. Per WQMP guidance.
            var bmps = new List<QuickBMPUpsertDto> { NewBMP(captured: 40, retained: 60) };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "Captured");
            StringAssert.Contains(result, "Retained");
        }

        [TestMethod]
        public void Validate_PercentOfSiteTreatedOutOfRange_ReturnsError()
        {
            var bmps = new List<QuickBMPUpsertDto> { NewBMP(siteTreated: 120) };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "Site Treated");
            StringAssert.Contains(result, "between 0 and 100");
        }

        [TestMethod]
        public void Validate_PercentCapturedOutOfRange_ReturnsError()
        {
            var bmps = new List<QuickBMPUpsertDto> { NewBMP(captured: -5, retained: -10) };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "Captured");
            StringAssert.Contains(result, "between 0 and 100");
        }

        [TestMethod]
        public void Validate_PercentRetainedOutOfRange_ReturnsError()
        {
            // captured >= retained and captured in range, so both earlier checks pass —
            // negative retained is the rule that trips. (Retained > 100 with a higher
            // captured isn't reachable: captured would also be > 100 and trip first.)
            var bmps = new List<QuickBMPUpsertDto> { NewBMP(captured: 5, retained: -10) };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "Retained");
            StringAssert.Contains(result, "between 0 and 100");
        }

        [TestMethod]
        public void Validate_SumOfSiteTreatedExceeds100_ReturnsError()
        {
            var bmps = new List<QuickBMPUpsertDto>
            {
                NewBMP(name: "BMP-1", siteTreated: 60),
                NewBMP(name: "BMP-2", siteTreated: 50),
            };

            var result = QuickBMPs.Validate(bmps);

            StringAssert.Contains(result, "exceeds 100 percent");
        }

        [TestMethod]
        public void Validate_SumOfSiteTreatedExactly100_Succeeds()
        {
            var bmps = new List<QuickBMPUpsertDto>
            {
                NewBMP(name: "BMP-1", siteTreated: 60),
                NewBMP(name: "BMP-2", siteTreated: 40),
            };

            Assert.IsNull(QuickBMPs.Validate(bmps));
        }

        [TestMethod]
        public void Validate_SiteTreatedNullSkipsSumCheck()
        {
            // The sum check only applies if any BMP has a non-null PercentOfSiteTreated.
            // This case has none — no sum, no error.
            var bmps = new List<QuickBMPUpsertDto>
            {
                NewBMP(name: "BMP-1", siteTreated: null),
                NewBMP(name: "BMP-2", siteTreated: null),
            };

            Assert.IsNull(QuickBMPs.Validate(bmps));
        }
    }
}
