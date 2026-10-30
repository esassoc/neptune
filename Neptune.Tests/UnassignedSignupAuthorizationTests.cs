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
    /// (which grants Unassigned) on endpoints that returned every user's email, phone and web-service token,
    /// changed any BMP's type, and read any field visit, maintenance record, assessment or project by ID.
    /// These tests pin the fixes, and snapshot every action a fresh sign-up gets past at the attribute level
    /// so a new one needs a deliberate decision rather than slipping in.
    /// </summary>
    [TestClass]
    public class UnassignedSignupAuthorizationTests
    {
        // Controller.Action pairs an Unassigned caller passes at the attribute level. Each one either serves
        // the caller's own data, applies its own in-method check (self-or-admin, OCTA grant reviewer, viewable
        // jurisdictions), or is reference data. Add to this list only after checking the new action does too.
        private static readonly string[] ExpectedUnassignedReachableActions =
        {
            "FundingSourceController.Get",
            "FundingSourceController.ListTreatmentBMPFunding",
            "FieldVisitController.List",
            "MaintenanceRecordController.List",
            "OnlandVisualTrashAssessmentAreaController.DownloadGdb",
            "OrganizationController.Get",
            "OrganizationController.ListFundingSources",
            "ProjectController.Get",
            "ProjectController.GetBoundingBoxByProjectID",
            "ProjectController.ListAttachmentsByProjectID",
            "ProjectController.ListDelineations",
            "ProjectController.ListDelineationsByProjectID",
            "ProjectController.ListLoadReducingResultsForProject",
            "ProjectController.ListProjectNetworkSolveHistoriesForProject",
            "ProjectController.ListProjectsSharedWithOCTAM2Tier2GrantProgram",
            "ProjectController.ListTreatmentBMPHRUCharacteristicsForProject",
            "ProjectController.ListTreatmentBMPsAsUpsertDtosByProjectID",
            "ProjectController.DownloadProjectsSharedWithOCTAM2Tier2GrantProgram",
            "ProjectController.DownloadTreatmentBMPsForProjectsSharedWithOCTAM2Tier2GrantProgram",
            "RegionalSubbasinController.GetRegionalSubbasinGraphTraceAsFeatureCollectionFromPoint",
            "TreatmentBMPAssessmentController.List",
            "TreatmentBMPAssessmentController.ListLatestByBMP",
            "TreatmentBMPController.DownloadGdb",
            "TreatmentBMPController.GetDelineationErrors",
            "TreatmentBMPController.GetParameterizationErrors",
            "TreatmentBMPController.GetUpstreamestErrors",
            "TreatmentBMPController.GetUpstreamRSBCatchmentGeoJSONForTreatmentBMP",
            "TreatmentBMPController.ListForDelineationMap",
            "TreatmentBMPController.ListOtherTreatmentBMPsInRegionalSubbasin",
            "TreatmentBMPTypeController.ListBMPsByType",
            "UserClaimsController.GetByGlobalID",
            "UserClaimsController.PostUserClaims",
            "UserController.Create",
            "UserController.Get",
            "UserController.GetDetail",
            "UserController.GetNotifications",
            "WaterQualityManagementPlanController.ListHydrologicSubareas",
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
        public void RecordViewFeatures_RefuseUnassigned()
        {
            foreach (var feature in new BaseAuthorizationAttribute[] { new FieldVisitViewFeature(), new MaintenanceRecordViewFeature(), new TreatmentBMPAssessmentViewFeature() })
            {
                CollectionAssert.DoesNotContain(AuthorizationAttributeHelpers.GetGrantedRoles(feature), RoleEnum.Unassigned, feature.GetType().Name);
            }
        }

        [TestMethod]
        public void ProjectView_UnassignedNonReviewer_IsForbidden()
        {
            var unassigned = new Person { RoleID = (int)RoleEnum.Unassigned };
            Assert.IsFalse(ProjectViewFeature.CanView(unassigned, 7, sharedWithOCTAM2Tier2: true));
        }

        [TestMethod]
        public void ProjectView_OCTAReviewer_SeesOnlySharedProjects()
        {
            var reviewer = new Person { RoleID = (int)RoleEnum.Unassigned, IsOCTAGrantReviewer = true };
            Assert.IsTrue(ProjectViewFeature.CanView(reviewer, 7, sharedWithOCTAM2Tier2: true));
            Assert.IsFalse(ProjectViewFeature.CanView(reviewer, 7, sharedWithOCTAM2Tier2: false));
        }

        [TestMethod]
        public void ProjectView_Editor_SeesOnlyOwnJurisdiction()
        {
            var editor = new Person
            {
                RoleID = (int)RoleEnum.JurisdictionEditor,
                StormwaterJurisdictionPeople = { new StormwaterJurisdictionPerson { StormwaterJurisdictionID = 7 } },
            };
            Assert.IsTrue(ProjectViewFeature.CanView(editor, 7, sharedWithOCTAM2Tier2: false));
            Assert.IsFalse(ProjectViewFeature.CanView(editor, 99, sharedWithOCTAM2Tier2: false));
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
