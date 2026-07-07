using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1104: pins the authorization attributes on the TreatmentBMP mutation endpoints.
    /// A prior multi-endpoint change (b709e4d689, NPT-1001) accidentally left <c>basic-info</c>
    /// as <c>[AllowAnonymous]</c> and the other edits as <c>[UserViewFeature]</c> (which grants
    /// every signed-in role, including Unassigned, with no jurisdiction check). These reflection
    /// tests fail loudly if any of the five edits ever regresses off <c>[TreatmentBMPEditFeature]</c>.
    ///
    /// The jurisdiction-matrix tests document the per-BMP decision <c>TreatmentBMPEditFeature</c>
    /// makes via the Person helpers, mapping to the acceptance-criteria 401/403/200 outcomes.
    /// </summary>
    [TestClass]
    public class TreatmentBMPControllerAuthorizationTests
    {
        private static readonly string[] EditEndpointMethodNames =
        {
            nameof(TreatmentBMPController.UpdateBasicInfo),
            nameof(TreatmentBMPController.UpdateType),
            nameof(TreatmentBMPController.UpdateLocation),
            nameof(TreatmentBMPController.UpdateCustomAttributes),
            nameof(TreatmentBMPController.UpdateUpstreamBMP),
        };

        private static MethodInfo GetMethod(string name)
        {
            var method = typeof(TreatmentBMPController).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected method {name} on TreatmentBMPController.");
            return method;
        }

        [TestMethod]
        public void AllFiveEditEndpoints_AreGatedWithTreatmentBMPEditFeature()
        {
            foreach (var name in EditEndpointMethodNames)
            {
                var method = GetMethod(name);
                Assert.IsTrue(method.GetCustomAttributes(typeof(TreatmentBMPEditFeature), true).Any(),
                    $"{name} must be decorated with [TreatmentBMPEditFeature].");
            }
        }

        [TestMethod]
        public void AllFiveEditEndpoints_DoNotAllowAnonymousOrUserView()
        {
            foreach (var name in EditEndpointMethodNames)
            {
                var method = GetMethod(name);

                Assert.IsFalse(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any(),
                    $"{name} must not be [AllowAnonymous].");
                Assert.IsFalse(method.GetCustomAttributes(typeof(OptionalAuthAttribute), true).Any(),
                    $"{name} must not be [OptionalAuth] — it would suppress the 401 for unauthenticated callers.");
                Assert.IsFalse(method.GetCustomAttributes(typeof(UserViewFeature), true).Any(),
                    $"{name} must not be [UserViewFeature] — that grants every signed-in role with no jurisdiction check.");
            }
        }

        [TestMethod]
        public void DeleteEndpoint_RemainsGatedWithJurisdictionEditFeature()
        {
            var method = GetMethod(nameof(TreatmentBMPController.Delete));
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "Delete must remain [JurisdictionEditFeature].");
        }

        [TestMethod]
        public void JurisdictionEditor_AssignedToBMPJurisdiction_IsAuthorized()
        {
            var editor = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionEditor,
                StormwaterJurisdictionPeople =
                {
                    new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 7 },
                },
            };

            Assert.IsFalse(editor.IsAnonymousOrUnassigned());
            Assert.IsTrue(editor.IsAssignedToStormwaterJurisdiction(7),
                "A JurisdictionEditor assigned to the BMP's jurisdiction must pass the per-BMP check (expects 200).");
        }

        [TestMethod]
        public void JurisdictionEditor_OfDifferentJurisdiction_IsForbidden()
        {
            var editor = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionEditor,
                StormwaterJurisdictionPeople =
                {
                    new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 99 },
                },
            };

            Assert.IsFalse(editor.IsAssignedToStormwaterJurisdiction(7),
                "A JurisdictionEditor assigned to a different jurisdiction must fail the per-BMP check (expects 403).");
        }

        [TestMethod]
        public void Administrator_IsAssignedToAnyJurisdiction()
        {
            var admin = new Person { RoleID = (int)RoleEnum.Admin };

            Assert.IsTrue(admin.IsAssignedToStormwaterJurisdiction(7),
                "Administrators are assigned to every jurisdiction (expects 200).");
        }

        [TestMethod]
        public void UnassignedUser_IsAnonymousOrUnassigned()
        {
            var unassigned = new Person { RoleID = (int)RoleEnum.Unassigned };

            Assert.IsTrue(unassigned.IsAnonymousOrUnassigned(),
                "An Unassigned user is short-circuited to Forbid by TreatmentBMPEditFeature (expects 403).");
        }
    }
}
