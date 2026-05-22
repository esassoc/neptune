using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// Covers the state transitions on FieldVisit that drive the workflow's
    /// verify / provisional / return-to-edit affordances. These methods used to
    /// live as extension methods in WebMvc; NPT-984 lifted them onto the EFModels
    /// partial class so the API can use them. These tests pin down their behavior.
    /// </summary>
    [TestClass]
    public class FieldVisitStateTransitionTests
    {
        private static FieldVisit BuildInProgressVisit()
        {
            return new FieldVisit
            {
                FieldVisitID = 42,
                FieldVisitStatusID = (int)FieldVisitStatusEnum.InProgress,
                IsFieldVisitVerified = false,
            };
        }

        [TestMethod]
        public void VerifyFieldVisit_SetsVerifiedAndComplete()
        {
            var visit = BuildInProgressVisit();

            visit.VerifyFieldVisit();

            Assert.IsTrue(visit.IsFieldVisitVerified);
            Assert.AreEqual((int)FieldVisitStatusEnum.Complete, visit.FieldVisitStatusID);
        }

        [TestMethod]
        public void MarkFieldVisitAsProvisional_ClearsVerifiedFlagButLeavesStatus()
        {
            var visit = BuildInProgressVisit();
            visit.IsFieldVisitVerified = true;
            visit.FieldVisitStatusID = (int)FieldVisitStatusEnum.Complete;

            visit.MarkFieldVisitAsProvisional();

            Assert.IsFalse(visit.IsFieldVisitVerified);
            Assert.AreEqual((int)FieldVisitStatusEnum.Complete, visit.FieldVisitStatusID,
                "MarkFieldVisitAsProvisional must not change the FieldVisitStatusID.");
        }

        [TestMethod]
        public void ReturnFieldVisitToEdit_ClearsVerifiedAndSetsReturnedToEdit()
        {
            var visit = BuildInProgressVisit();
            visit.IsFieldVisitVerified = true;
            visit.FieldVisitStatusID = (int)FieldVisitStatusEnum.Complete;

            visit.ReturnFieldVisitToEdit();

            Assert.IsFalse(visit.IsFieldVisitVerified);
            Assert.AreEqual((int)FieldVisitStatusEnum.ReturnedToEdit, visit.FieldVisitStatusID);
        }

        [TestMethod]
        public void MarkFieldVisitAsProvisionalIfNonManager_NonManager_ClearsVerifiedFlag()
        {
            var visit = BuildInProgressVisit();
            visit.IsFieldVisitVerified = true;
            visit.TreatmentBMP = new TreatmentBMP { StormwaterJurisdictionID = 7 };

            var nonManager = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionEditor,
                StormwaterJurisdictionPeople =
                {
                    new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 7 },
                },
            };

            visit.MarkFieldVisitAsProvisionalIfNonManager(nonManager);

            Assert.IsFalse(visit.IsFieldVisitVerified);
        }

        [TestMethod]
        public void MarkFieldVisitAsProvisionalIfNonManager_Manager_KeepsVerifiedFlag()
        {
            var visit = BuildInProgressVisit();
            visit.IsFieldVisitVerified = true;
            visit.TreatmentBMP = new TreatmentBMP { StormwaterJurisdictionID = 7 };

            var manager = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionManager,
                StormwaterJurisdictionPeople =
                {
                    new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 7 },
                },
            };

            visit.MarkFieldVisitAsProvisionalIfNonManager(manager);

            Assert.IsTrue(visit.IsFieldVisitVerified,
                "Managers assigned to the BMP's jurisdiction must not have their verification cleared.");
        }

        [TestMethod]
        public void MarkFieldVisitAsProvisionalIfNonManager_ManagerOfDifferentJurisdiction_ClearsVerifiedFlag()
        {
            var visit = BuildInProgressVisit();
            visit.IsFieldVisitVerified = true;
            visit.TreatmentBMP = new TreatmentBMP { StormwaterJurisdictionID = 7 };

            var managerOfOther = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionManager,
                StormwaterJurisdictionPeople =
                {
                    new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 99 },
                },
            };

            visit.MarkFieldVisitAsProvisionalIfNonManager(managerOfOther);

            Assert.IsFalse(visit.IsFieldVisitVerified);
        }
    }
}
