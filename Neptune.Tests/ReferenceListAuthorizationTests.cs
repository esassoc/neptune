using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Authorization;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1104 round 2: JE/JM-facing edit flows load reference lists on open (owner-organization
    /// dropdown in Edit Basic Info, funding sources in the funding-event modal, BMP create, project
    /// basics). Those list GETs were [AdminFeature], which 403'd JurisdictionEditors AND
    /// JurisdictionManagers the moment they opened the editor — the round-1 audit covered the write
    /// endpoints but missed the ancillary lookup loads. Pin the corrected gates so this regression
    /// class can't silently return.
    /// </summary>
    [TestClass]
    public class ReferenceListAuthorizationTests
    {
        private static MethodInfo GetListMethod(System.Type controllerType)
        {
            var method = controllerType.GetMethod("List", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, $"Expected List() on {controllerType.Name}.");
            return method!;
        }

        [TestMethod]
        public void OrganizationList_IsReachableByJurisdictionEditorsAndManagers()
        {
            var method = GetListMethod(typeof(OrganizationController));
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "GET /organizations must be [JurisdictionEditFeature] — the BMP Edit Basic Info owner-organization dropdown, BMP create, and project basics all load it for JE/JM users.");
            Assert.IsFalse(method.GetCustomAttributes(typeof(AdminFeature), true).Any(),
                "GET /organizations must not be [AdminFeature] — that 403'd JE/JM edit flows on open.");
        }

        [TestMethod]
        public void FundingSourceList_IsReachableByJurisdictionEditorsAndManagers()
        {
            var method = GetListMethod(typeof(FundingSourceController));
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "GET /funding-sources must be [JurisdictionEditFeature] — the funding-event modal loads it for JE/JM users.");
            Assert.IsFalse(method.GetCustomAttributes(typeof(AdminFeature), true).Any(),
                "GET /funding-sources must not be [AdminFeature] — that 403'd the JE/JM funding modal on open.");
        }
    }
}
