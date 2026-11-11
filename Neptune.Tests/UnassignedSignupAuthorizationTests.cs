using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.Tests.Helpers;

namespace Neptune.Tests
{
    /// <summary>
    /// Anyone can sign up, and a sign-up lands on Unassigned. On 29 Oct 2026 an audit found UserViewFeature
    /// granted Unassigned, so a sign-up could get every user's email, phone and web-service token, change any
    /// BMP's type, and read BMP, field-visit and project data by ID. UserViewFeature is now staff-only.
    /// These tests pin the fixes, and snapshot every action a fresh sign-up gets past at the attribute level
    /// so a new one needs a deliberate decision rather than slipping in.
    /// </summary>
    [TestClass]
    public class UnassignedSignupAuthorizationTests
    {
        // Controller.Action pairs an Unassigned caller passes at the attribute level, grouped by what then
        // stops them reading someone else's data. Add to this list only after the new action fits a group.
        private static readonly string[] ExpectedUnassignedReachableActions =
        {
            // Sign-up and the caller's own record: self-or-admin check in the action, or the caller's own claims.
            "UserClaimsController.GetByGlobalID",
            "UserClaimsController.PostUserClaims",
            "UserController.Create",
            "UserController.Get",
            "UserController.GetDetail",
            "UserController.GetNotifications",

            // OCTA grant review (the reviewer flag is independent of role): IsOCTAGrantReviewer check in the action.
            "ProjectController.ListProjectsSharedWithOCTAM2Tier2GrantProgram",
            "ProjectController.ListDelineations",
            "ProjectController.DownloadProjectsSharedWithOCTAM2Tier2GrantProgram",
            "ProjectController.DownloadTreatmentBMPsForProjectsSharedWithOCTAM2Tier2GrantProgram",

            // Project-routed reads: ProjectViewFeature lets Unassigned through only as an OCTA reviewer on a shared project.
            "ProjectController.Get",
            "ProjectController.GetBoundingBoxByProjectID",
            "ProjectController.ListAttachmentsByProjectID",
            "ProjectController.ListDelineationsByProjectID",
            "ProjectController.ListLoadReducingResultsForProject",
            "ProjectController.ListProjectNetworkSolveHistoriesForProject",
            "ProjectController.ListTreatmentBMPHRUCharacteristicsForProject",
            "ProjectController.ListTreatmentBMPsAsUpsertDtosByProjectID",

            // Reference data the public pages link to (not jurisdiction-scoped): the owner organization on a
            // BMP page and its funding sources (contact as PersonDisplayDto, no email or phone), and the
            // drainage-area trace on public maps.
            "OrganizationController.Get",
            "OrganizationController.ListFundingSources",
            "RegionalSubbasinController.GetRegionalSubbasinGraphTraceAsFeatureCollectionFromPoint",
        };

        [TestMethod]
        public void ActionsAnUnassignedSignupCanPass_MatchTheReviewedList()
        {
            var actual = ListActionsUnassignedCanPass().OrderBy(x => x).ToList();
            var expected = ExpectedUnassignedReachableActions.OrderBy(x => x).ToList();

            var added = actual.Except(expected).ToList();
            var removed = expected.Except(actual).ToList();
            Assert.IsTrue(added.Count == 0 && removed.Count == 0,
                "The actions a fresh Unassigned sign-up can reach changed. Review each one, then update the list.\n" +
                $"Newly reachable: {string.Join(", ", added)}\nNo longer reachable: {string.Join(", ", removed)}");
        }

        [TestMethod]
        public void UserList_IsStaffOnly()
        {
            var method = typeof(UserController).GetMethod(nameof(UserController.List))!;
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any());
            Assert.IsFalse(method.GetCustomAttributes(typeof(UserViewFeature), true).Any());
        }

        [TestMethod]
        public void OrganizationUsers_IsStaffOnly()
        {
            var method = typeof(OrganizationController).GetMethod(nameof(OrganizationController.ListUsers))!;
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any());
        }

        [TestMethod]
        public void PersonSimpleDto_DoesNotCarryTheWebServiceToken()
        {
            // PersonSimpleDto goes out in user lists and inside ProjectDto; the token signs in to the external API.
            Assert.IsNull(typeof(PersonSimpleDto).GetProperty("WebServiceAccessToken"));
        }

        [TestMethod]
        public void BMPTypeChangeAndLandUseRefresh_AreJurisdictionScopedEdits()
        {
            foreach (var name in new[] { nameof(TreatmentBMPController.ChangeTreatmentBMPType), nameof(TreatmentBMPController.QueueRefreshLandUse) })
            {
                var method = typeof(TreatmentBMPController).GetMethod(name)!;
                Assert.IsTrue(method.GetCustomAttributes(typeof(TreatmentBMPEditFeature), true).Any(), $"{name} must be [TreatmentBMPEditFeature].");
            }
        }

        [TestMethod]
        public void UserViewFeature_DoesNotGrantUnassigned()
        {
            // UserViewFeature gates ~30 staff endpoints; letting Unassigned back in reopens every one to a sign-up.
            CollectionAssert.DoesNotContain(AuthorizationAttributeHelpers.GetGrantedRoles(new UserViewFeature()), RoleEnum.Unassigned);
        }

        [TestMethod]
        public void WebServices_AreForActiveStaffOnly()
        {
            // The ExternalAPI token handler refuses any owner this returns false for.
            Assert.IsTrue(new Person { RoleID = (int)RoleEnum.JurisdictionEditor, IsActive = true }.CanUseWebServices());
            Assert.IsTrue(new Person { RoleID = (int)RoleEnum.JurisdictionManager, IsActive = true }.CanUseWebServices());
            Assert.IsTrue(new Person { RoleID = (int)RoleEnum.Admin, IsActive = true }.CanUseWebServices());
            Assert.IsTrue(new Person { RoleID = (int)RoleEnum.SitkaAdmin, IsActive = true }.CanUseWebServices());
            Assert.IsFalse(new Person { RoleID = (int)RoleEnum.Unassigned, IsActive = true }.CanUseWebServices());
            Assert.IsFalse(new Person { RoleID = (int)RoleEnum.Unassigned, IsActive = true, IsOCTAGrantReviewer = true }.CanUseWebServices());
            Assert.IsFalse(new Person { RoleID = (int)RoleEnum.Admin, IsActive = false }.CanUseWebServices());
        }

        [TestMethod]
        public void TokenGeneration_IsStaffOnly()
        {
            var method = typeof(UserController).GetMethod(nameof(UserController.GenerateWebServiceToken))!;
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any());
        }

        private static IEnumerable<string> ListActionsUnassignedCanPass()
        {
            var controllerTypes = typeof(UserController).Assembly.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var controller in controllerTypes)
            {
                var controllerAttributes = controller.GetCustomAttributes(true);
                foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!action.GetCustomAttributes(typeof(HttpMethodAttribute), true).Any())
                    {
                        continue;
                    }

                    var attributes = controllerAttributes.Concat(action.GetCustomAttributes(true)).ToList();
                    if (attributes.OfType<AllowAnonymousAttribute>().Any())
                    {
                        continue; // anonymous surface is reviewed separately
                    }

                    var gates = attributes.OfType<BaseAuthorizationAttribute>().ToList();
                    var unassignedPassesEveryGate = gates.All(gate =>
                    {
                        var roles = AuthorizationAttributeHelpers.GetGrantedRoles(gate);
                        return roles.Count == 0 || roles.Contains(RoleEnum.Unassigned);
                    });

                    if (unassignedPassesEveryGate)
                    {
                        yield return $"{controller.Name}.{action.Name}";
                    }
                }
            }
        }
    }
}
