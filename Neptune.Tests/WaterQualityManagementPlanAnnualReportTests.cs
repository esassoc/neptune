using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1063 — covers the FY math used by the WQMP Annual Report and the
    /// post-construction aggregation logic that the API endpoint relies on.
    /// </summary>
    [TestClass]
    public class WaterQualityManagementPlanAnnualReportTests
    {
        [TestMethod]
        public void GetAnnualReportPeriodStart_IsJulyFirstOfPriorYear()
        {
            var start = WaterQualityManagementPlans.GetAnnualReportPeriodStart(2025);
            Assert.AreEqual(new DateTime(2024, 7, 1), start);
        }

        [TestMethod]
        public void GetAnnualReportPeriodEnd_IsJuneThirtieth()
        {
            var end = WaterQualityManagementPlans.GetAnnualReportPeriodEnd(2025);
            Assert.AreEqual(new DateTime(2025, 6, 30), end);
        }

        [TestMethod]
        public void GetSelectableAnnualReportYears_StartsAt2022AndIsDescending()
        {
            var years = WaterQualityManagementPlans.GetSelectableAnnualReportYears();
            Assert.IsTrue(years.Count >= 1);
            Assert.AreEqual(WaterQualityManagementPlans.AnnualReportMinimumReportingYear, years.Min());
            CollectionAssert.AreEqual(years.OrderByDescending(x => x).ToList(), years);
        }

        private static vWaterQualityManagementPlanAnnualReport BuildRow(
            int wqmpID,
            string name,
            DateOnly verificationDate,
            int verifyID,
            int? treatmentBMPCount,
            int? treatmentBMPAdequate,
            int? treatmentBMPDeficient,
            string? treatmentBMPNotes,
            int? quickBMPCount,
            int? quickBMPAdequate,
            int? quickBMPDeficient,
            string? quickBMPNotes,
            string? enforcement,
            int statusID = (int)WaterQualityManagementPlanVerifyStatusEnum.AdequateOAndMofWQMPisVerified)
        {
            return new vWaterQualityManagementPlanAnnualReport
            {
                WaterQualityManagementPlanID = wqmpID,
                WaterQualityManagementPlanName = name,
                WaterQualityManagementPlanStatusID = (int)WaterQualityManagementPlanStatusEnum.Active,
                StormwaterJurisdictionID = 1,
                StormwaterJurisdictionPublicWQMPVisibilityTypeID = 1,
                WaterQualityManagementPlanVerifyID = verifyID,
                WaterQualityManagementPlanVerifyStatusID = statusID,
                WaterQualityManagementPlanVerifyVerificationDate = verificationDate,
                EnforcementOrFollowupActions = enforcement,
                WaterQualityManagementPlanVerifyTreatmentBMPCount = treatmentBMPCount,
                WaterQualityManagementPlanVerifyTreatmentBMPIsAdequateCount = treatmentBMPAdequate,
                WaterQualityManagementPlanVerifyTreatmentBMPIsDeficientCount = treatmentBMPDeficient,
                WaterQualityManagementPlanVerifyTreatmentBMPNotes = treatmentBMPNotes,
                WaterQualityManagementPlanVerifyQuickBMPCount = quickBMPCount,
                WaterQualityManagementPlanVerifyQuickBMPIsAdequateCount = quickBMPAdequate,
                WaterQualityManagementPlanVerifyQuickBMPIsDeficient = quickBMPDeficient,
                WaterQualityManagementPlanVerifyQuickBMPNotes = quickBMPNotes,
            };
        }

        [TestMethod]
        public void BuildPostConstructionGridDtos_PrefersTreatmentBMPRollupWhenCountPresent()
        {
            var row = BuildRow(
                wqmpID: 1, name: "WQMP A",
                verificationDate: new DateOnly(2025, 3, 15), verifyID: 10,
                treatmentBMPCount: 5, treatmentBMPAdequate: 3, treatmentBMPDeficient: 2, treatmentBMPNotes: "tbmp note",
                quickBMPCount: 99, quickBMPAdequate: 99, quickBMPDeficient: 99, quickBMPNotes: "quick note",
                enforcement: "follow-up");

            var dtos = vWaterQualityManagementPlanAnnualReportExtensionMethods.BuildPostConstructionGridDtos(new[] { row });
            Assert.AreEqual(1, dtos.Count);
            var dto = dtos[0];
            Assert.AreEqual(5, dto.NumberOfBMPs);
            Assert.AreEqual(3, dto.NumberOfBMPsAdequate);
            Assert.AreEqual(2, dto.NumberOfBMPsDeficient);
            Assert.AreEqual("tbmp note; follow-up", dto.WQMPVerificationComments);
        }

        [TestMethod]
        public void BuildPostConstructionGridDtos_FallsBackToQuickBMPWhenTreatmentCountIsNull()
        {
            var row = BuildRow(
                wqmpID: 2, name: "WQMP B",
                verificationDate: new DateOnly(2025, 3, 15), verifyID: 11,
                treatmentBMPCount: null, treatmentBMPAdequate: null, treatmentBMPDeficient: null, treatmentBMPNotes: null,
                quickBMPCount: 7, quickBMPAdequate: 4, quickBMPDeficient: 3, quickBMPNotes: "quick note",
                enforcement: "follow-up");

            var dto = vWaterQualityManagementPlanAnnualReportExtensionMethods.BuildPostConstructionGridDtos(new[] { row }).Single();
            Assert.AreEqual(7, dto.NumberOfBMPs);
            Assert.AreEqual(4, dto.NumberOfBMPsAdequate);
            Assert.AreEqual(3, dto.NumberOfBMPsDeficient);
            Assert.AreEqual("quick note; follow-up", dto.WQMPVerificationComments);
        }

        [TestMethod]
        public void BuildPostConstructionGridDtos_OmitsSeparatorWhenBMPNotesEmpty()
        {
            var row = BuildRow(
                wqmpID: 3, name: "WQMP C",
                verificationDate: new DateOnly(2025, 3, 15), verifyID: 12,
                treatmentBMPCount: 1, treatmentBMPAdequate: 1, treatmentBMPDeficient: 0, treatmentBMPNotes: null,
                quickBMPCount: null, quickBMPAdequate: null, quickBMPDeficient: null, quickBMPNotes: null,
                enforcement: "follow-up only");

            var dto = vWaterQualityManagementPlanAnnualReportExtensionMethods.BuildPostConstructionGridDtos(new[] { row }).Single();
            Assert.AreEqual("follow-up only", dto.WQMPVerificationComments);
        }

        [TestMethod]
        public void BuildPostConstructionGridDtos_PicksMostRecentVerificationPerWQMP()
        {
            var older = BuildRow(
                wqmpID: 4, name: "WQMP D",
                verificationDate: new DateOnly(2024, 12, 1), verifyID: 20,
                treatmentBMPCount: 1, treatmentBMPAdequate: 1, treatmentBMPDeficient: 0, treatmentBMPNotes: "old",
                quickBMPCount: null, quickBMPAdequate: null, quickBMPDeficient: null, quickBMPNotes: null,
                enforcement: "");
            var newer = BuildRow(
                wqmpID: 4, name: "WQMP D",
                verificationDate: new DateOnly(2025, 5, 1), verifyID: 21,
                treatmentBMPCount: 3, treatmentBMPAdequate: 2, treatmentBMPDeficient: 1, treatmentBMPNotes: "new",
                quickBMPCount: null, quickBMPAdequate: null, quickBMPDeficient: null, quickBMPNotes: null,
                enforcement: "");

            var dto = vWaterQualityManagementPlanAnnualReportExtensionMethods.BuildPostConstructionGridDtos(new[] { older, newer }).Single();
            Assert.AreEqual(3, dto.NumberOfBMPs);
            Assert.AreEqual(2, dto.NumberOfBMPsAdequate);
            Assert.AreEqual(1, dto.NumberOfBMPsDeficient);
            Assert.AreEqual("new", dto.WQMPVerificationComments.TrimEnd(';', ' '));
        }

        [TestMethod]
        public void BuildPostConstructionGridDtos_ResolvesVerifyStatusDisplayName()
        {
            var row = BuildRow(
                wqmpID: 5, name: "WQMP E",
                verificationDate: new DateOnly(2025, 3, 15), verifyID: 30,
                treatmentBMPCount: 1, treatmentBMPAdequate: 1, treatmentBMPDeficient: 0, treatmentBMPNotes: null,
                quickBMPCount: null, quickBMPAdequate: null, quickBMPDeficient: null, quickBMPNotes: null,
                enforcement: null,
                statusID: (int)WaterQualityManagementPlanVerifyStatusEnum.DeficienciesarePresentandFollowupisRequired);

            var dto = vWaterQualityManagementPlanAnnualReportExtensionMethods.BuildPostConstructionGridDtos(new[] { row }).Single();
            Assert.AreEqual("Deficiencies are Present and Follow-up is Required", dto.WaterQualityManagementPlanVerifyStatusName);
        }
    }
}
