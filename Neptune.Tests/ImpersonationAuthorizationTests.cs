using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1104 rework: authorization gates evaluate the EFFECTIVE (impersonated) user so that
    /// impersonation faithfully exercises authorization — previously the UI wore the impersonated
    /// identity while every gate passed as the authenticated admin, which is exactly how the
    /// JurisdictionEditor 403 regression slipped past impersonation-based testing. These tests pin
    /// the carve-outs and the environment short-circuits so the behavior can't silently drift.
    /// </summary>
    [TestClass]
    public class ImpersonationAuthorizationTests
    {
        private static bool GetEvaluateAuthenticatedUserOnly(BaseAuthorizationAttribute attribute)
        {
            var property = typeof(BaseAuthorizationAttribute).GetProperty("EvaluateAuthenticatedUserOnly",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property, "EvaluateAuthenticatedUserOnly property must exist on BaseAuthorizationAttribute.");
            return (bool)property!.GetValue(attribute)!;
        }

        [TestMethod]
        public void ImpersonationStartAndStopFeatures_EvaluateTheAuthenticatedUser()
        {
            // Start/stop must authorize the REAL admin even while wearing a low-privilege identity —
            // otherwise an admin impersonating a Viewer could never stop impersonating.
            Assert.IsTrue(GetEvaluateAuthenticatedUserOnly(new ImpersonateUserFeature()),
                "ImpersonateUserFeature must evaluate the authenticated user.");
            Assert.IsTrue(GetEvaluateAuthenticatedUserOnly(new StopImpersonationFeature()),
                "StopImpersonationFeature must evaluate the authenticated user.");
        }

        [TestMethod]
        public void RegularFeatures_EvaluateTheEffectiveUser()
        {
            // Representative sample of ordinary gates: all must run against the effective
            // (impersonated) user so impersonation testing is faithful.
            Assert.IsFalse(GetEvaluateAuthenticatedUserOnly(new TreatmentBMPEditFeature()),
                "TreatmentBMPEditFeature must evaluate the effective (impersonated) user.");
            Assert.IsFalse(GetEvaluateAuthenticatedUserOnly(new AdminFeature()),
                "AdminFeature must evaluate the effective (impersonated) user.");
            Assert.IsFalse(GetEvaluateAuthenticatedUserOnly(new UserViewFeature()),
                "UserViewFeature must evaluate the effective (impersonated) user.");
        }

        [TestMethod]
        public void GetEffectivePerson_InProduction_ReturnsAuthenticatedUser_WithoutTouchingTheDatabase()
        {
            var service = new ImpersonationService(new FakeWebHostEnvironment("Production"));
            var authenticatedAdmin = new Person { PersonID = 1, RoleID = 2, ImpersonatedPersonID = 42 };

            // dbContext is deliberately null: the production path must short-circuit before any DB access.
            var effective = service.GetEffectivePerson(null!, authenticatedAdmin);

            Assert.AreSame(authenticatedAdmin, effective, "Impersonation must no-op in production.");
        }

        [TestMethod]
        public void GetEffectivePerson_NotImpersonating_ReturnsAuthenticatedUser_WithoutTouchingTheDatabase()
        {
            var service = new ImpersonationService(new FakeWebHostEnvironment("Development"));
            var authenticatedAdmin = new Person { PersonID = 1, RoleID = 2, ImpersonatedPersonID = null };

            var effective = service.GetEffectivePerson(null!, authenticatedAdmin);

            Assert.AreSame(authenticatedAdmin, effective, "Without an ImpersonatedPersonID the authenticated user is the effective user.");
        }

        [TestMethod]
        public void GetEffectivePerson_NullAuthenticatedUser_ReturnsNull()
        {
            var service = new ImpersonationService(new FakeWebHostEnvironment("Development"));
            Assert.IsNull(service.GetEffectivePerson(null!, null));
        }

        // The impersonating (non-prod, ImpersonatedPersonID set) path loads the impersonated Person —
        // with StormwaterJurisdictionPeople includes — via People.GetByID; exercised end-to-end in
        // local verification (impersonated JE saving an out-of-jurisdiction BMP now 403s).

        private sealed class FakeWebHostEnvironment(string environmentName) : IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = environmentName;
            public string ApplicationName { get; set; } = "Neptune.Tests";
            public string WebRootPath { get; set; } = string.Empty;
            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
