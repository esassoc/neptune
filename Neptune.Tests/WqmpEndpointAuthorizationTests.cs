using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1109: JurisdictionEditors were blocked from WQMP document CRUD and the AI
    /// extraction workflow — six endpoints carried [JurisdictionManageFeature], which is both
    /// Manager-only and jurisdiction-blind. The five WQMP-routed endpoints moved to the
    /// entity-scoped [WaterQualityManagementPlanEditFeature] (role check + jurisdiction match
    /// against the routed WQMP, mirroring TreatmentBMPEditFeature); the route-less upload
    /// endpoint moved to [JurisdictionEditFeature] (its in-body jurisdiction check scopes it).
    /// Pin the corrected gates so this regression class can't silently return — same rationale
    /// as ReferenceListAuthorizationTests.
    /// </summary>
    [TestClass]
    public class WqmpEndpointAuthorizationTests
    {
        private static MethodInfo GetControllerMethod(string methodName)
        {
            var method = typeof(WaterQualityManagementPlanController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected {methodName}() on WaterQualityManagementPlanController.");
            return method!;
        }

        private static void AssertWqmpEditGated(string methodName, string reason)
        {
            var method = GetControllerMethod(methodName);
            Assert.IsTrue(method.GetCustomAttributes(typeof(WaterQualityManagementPlanEditFeature), true).Any(),
                $"{methodName} must be [WaterQualityManagementPlanEditFeature] — {reason}");
            Assert.IsFalse(method.GetCustomAttributes(typeof(JurisdictionManageFeature), true).Any(),
                $"{methodName} must not be [JurisdictionManageFeature] — that excluded JurisdictionEditors and was jurisdiction-blind.");
        }

        [TestMethod]
        public void CreateDocument_IsWqmpEditGated() =>
            AssertWqmpEditGated("CreateDocument", "JEs upload documents on WQMPs in their jurisdiction (NPT-1109 AC 1-2).");

        [TestMethod]
        public void UpdateDocument_IsWqmpEditGated() =>
            AssertWqmpEditGated("UpdateDocument", "JEs edit document metadata on WQMPs in their jurisdiction (NPT-1109 AC 3).");

        [TestMethod]
        public void DeleteDocument_IsWqmpEditGated() =>
            AssertWqmpEditGated("DeleteDocument", "JEs delete documents on WQMPs in their jurisdiction (NPT-1109 AC 4).");

        [TestMethod]
        public void RunExtraction_IsWqmpEditGated() =>
            AssertWqmpEditGated("RunExtraction", "JEs run AI extraction on WQMPs in their jurisdiction (NPT-1109 Bug 2).");

        [TestMethod]
        public void GetExtractionResult_IsWqmpEditGated() =>
            AssertWqmpEditGated("GetExtractionResult", "the review wizard loads this on mount; JEs need it (NPT-1109 Bug 2).");

        [TestMethod]
        public void UploadDocument_IsJurisdictionEditGated()
        {
            var method = GetControllerMethod("UploadDocument");
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "POST /upload must be [JurisdictionEditFeature] — JEs create WQMPs from PDF (NPT-1109 Bug 2); the endpoint has no WQMP route id, and its in-body check scopes the requested jurisdiction to the caller's assigned set.");
            Assert.IsFalse(method.GetCustomAttributes(typeof(JurisdictionManageFeature), true).Any(),
                "POST /upload must not be [JurisdictionManageFeature] — that excluded JurisdictionEditors from the AI wizard entry point.");
        }

        [TestMethod]
        public void WaterQualityManagementPlanEditFeature_GrantsJurisdictionEditor()
        {
            // grantedRoles is a primary-constructor parameter captured on BaseAuthorizationAttribute;
            // locate its compiler-generated backing field by type rather than by (unstable) name.
            var attribute = new WaterQualityManagementPlanEditFeature();
            var rolesField = typeof(BaseAuthorizationAttribute)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(f => typeof(IEnumerable<RoleEnum>).IsAssignableFrom(f.FieldType));
            var grantedRoles = ((IEnumerable<RoleEnum>)rolesField.GetValue(attribute)!).ToList();

            CollectionAssert.AreEquivalent(
                new List<RoleEnum> { RoleEnum.SitkaAdmin, RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor },
                grantedRoles,
                "WaterQualityManagementPlanEditFeature must grant exactly Editor-and-up — JurisdictionEditor inclusion is the point of NPT-1109.");
        }
    }
}
