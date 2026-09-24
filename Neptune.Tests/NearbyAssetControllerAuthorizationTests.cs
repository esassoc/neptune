using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1123: the homepage Field Actions panel is shown to every signed-in role except Unassigned.
    /// <c>[JurisdictionEditFeature]</c> is role-only and grants exactly Admin, SitkaAdmin, JurisdictionManager
    /// and JurisdictionEditor — pin it so the nearby-assets lookup never drifts to anonymous or Unassigned access.
    /// </summary>
    [TestClass]
    public class NearbyAssetControllerAuthorizationTests
    {
        private static MethodInfo GetListMethod()
        {
            var method = typeof(NearbyAssetController).GetMethod(nameof(NearbyAssetController.List), BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, "Expected List on NearbyAssetController.");
            return method;
        }

        [TestMethod]
        public void List_RequiresJurisdictionEditFeature()
        {
            Assert.IsTrue(GetListMethod().GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "NearbyAssetController.List must carry [JurisdictionEditFeature].");
        }

        [TestMethod]
        public void List_IsNotAnonymous()
        {
            var method = GetListMethod();
            Assert.IsFalse(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any(), "List must not be [AllowAnonymous].");
            Assert.IsFalse(method.GetCustomAttributes(typeof(OptionalAuthAttribute), true).Any(), "List must not be [OptionalAuth].");
        }
    }
}
