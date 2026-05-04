using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the NPT-1032 OVTA improvements: the SecondAssessor free-text field flows through
    /// the grid/detail/review-and-finalize DTOs, and OvtaAreaSourceTypeID is preserved on the
    /// SimpleDto so the workflow can pick the right toggle on the Select Area step.
    /// </summary>
    [TestClass]
    public class OnlandVisualTrashAssessmentExtensionMethodsTests
    {
        private static OnlandVisualTrashAssessment BuildOvta(
            string? secondAssessorName = null,
            int? ovtaAreaSourceTypeID = null)
        {
            return new OnlandVisualTrashAssessment
            {
                OnlandVisualTrashAssessmentID = 1,
                CreatedByPersonID = 100,
                CreatedDate = new DateTime(2026, 1, 1),
                StormwaterJurisdictionID = 7,
                OnlandVisualTrashAssessmentStatusID = (int)OnlandVisualTrashAssessmentStatusEnum.InProgress,
                IsTransectBackingAssessment = false,
                IsProgressAssessment = false,
                SecondAssessorName = secondAssessorName,
                OvtaAreaSourceTypeID = ovtaAreaSourceTypeID,
                CreatedByPerson = new Person
                {
                    PersonID = 100,
                    FirstName = "Ada",
                    LastName = "Lovelace",
                },
                StormwaterJurisdiction = new StormwaterJurisdiction
                {
                    Organization = new Organization { OrganizationName = "Test Jurisdiction" }
                },
                OnlandVisualTrashAssessmentObservations = new List<OnlandVisualTrashAssessmentObservation>(),
                OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes =
                    new List<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType>(),
            };
        }

        [TestMethod]
        public void AsGridDto_PopulatesSecondAssessorName_WhenSet()
        {
            var ovta = BuildOvta(secondAssessorName: "Grace Hopper");
            var dto = ovta.AsGridDto();
            Assert.AreEqual("Grace Hopper", dto.SecondAssessorName);
        }

        [TestMethod]
        public void AsGridDto_LeavesSecondAssessorNameNull_WhenNotSet()
        {
            var ovta = BuildOvta();
            var dto = ovta.AsGridDto();
            Assert.IsNull(dto.SecondAssessorName);
        }

        [TestMethod]
        public void AsDetailDto_PopulatesSecondAssessorName_WhenSet()
        {
            var ovta = BuildOvta(secondAssessorName: "Grace Hopper");
            var dto = ovta.AsDetailDto();
            Assert.AreEqual("Grace Hopper", dto.SecondAssessorName);
        }

        [TestMethod]
        public void AsReviewAndFinalizeDto_PopulatesSecondAssessorName_WhenSet()
        {
            var ovta = BuildOvta(secondAssessorName: "Grace Hopper");
            var dto = ovta.AsReviewAndFinalizeDto();
            Assert.AreEqual("Grace Hopper", dto.SecondAssessorName);
        }

        [TestMethod]
        public void AsSimpleDto_RoundTripsOvtaAreaSourceTypeID()
        {
            var ovta = BuildOvta(ovtaAreaSourceTypeID: (int)OvtaAreaSourceTypeEnum.LandUseBlock);
            var dto = ovta.AsSimpleDto();
            Assert.AreEqual((int)OvtaAreaSourceTypeEnum.LandUseBlock, dto.OvtaAreaSourceTypeID);
        }

        [TestMethod]
        public void OvtaAreaSourceType_StaticEnumExposesParcelAndLandUseBlock()
        {
            Assert.AreEqual(2, OvtaAreaSourceType.All.Count);
            Assert.AreEqual((int)OvtaAreaSourceTypeEnum.Parcel, OvtaAreaSourceType.Parcel.OvtaAreaSourceTypeID);
            Assert.AreEqual((int)OvtaAreaSourceTypeEnum.LandUseBlock, OvtaAreaSourceType.LandUseBlock.OvtaAreaSourceTypeID);
        }
    }
}
