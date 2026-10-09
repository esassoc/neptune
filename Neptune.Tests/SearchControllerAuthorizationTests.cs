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
    /// NPT-1125: header record search is available to every signed-in role except Unassigned.
    /// <c>[JurisdictionEditFeature]</c> is role-only and grants exactly Admin, SitkaAdmin, JurisdictionManager
    /// and JurisdictionEditor — pin it so the search index never drifts to anonymous or Unassigned access.
    /// </summary>
    [TestClass]
    public class SearchControllerAuthorizationTests
    {
        private static MethodInfo GetListIndexMethod()
        {
            var method = typeof(SearchController).GetMethod(nameof(SearchController.ListIndex), BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, "Expected ListIndex on SearchController.");
            return method;
        }

        [TestMethod]
        public void ListIndex_RequiresJurisdictionEditFeature()
        {
            Assert.IsTrue(GetListIndexMethod().GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "SearchController.ListIndex must carry [JurisdictionEditFeature].");
        }

        [TestMethod]
        public void ListIndex_IsNotAnonymous()
        {
            var method = GetListIndexMethod();
            Assert.IsFalse(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any(), "ListIndex must not be [AllowAnonymous].");
            Assert.IsFalse(method.GetCustomAttributes(typeof(OptionalAuthAttribute), true).Any(), "ListIndex must not be [OptionalAuth].");
        }
    }
}
