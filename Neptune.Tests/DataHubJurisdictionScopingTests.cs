using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.Helpers;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1135: the Data Hub / Delineation uploads and downloads take the jurisdiction in the body or form and
    /// return 403 unless the caller is assigned to it (Admin/SitkaAdmin: any), on top of their unchanged role gates.
    /// DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive when the data isn't present.
    /// Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB (e.g. a devcontainer).
    /// </summary>
    [TestClass]
    public class DataHubJurisdictionScopingTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        // Every endpoint behind the nine pages, with the role attribute it had before NPT-1135. The fix adds a
        // jurisdiction check; it must not change these (req 3).
        private static readonly (Type Controller, string Method, Type RoleAttribute)[] ScopedEndpoints =
        {
            (typeof(LandUseBlockController), nameof(LandUseBlockController.DownloadGdb), typeof(JurisdictionEditFeature)),
            (typeof(LandUseBlockController), nameof(LandUseBlockController.UploadGdb), typeof(JurisdictionEditFeature)),
            (typeof(LandUseBlockController), nameof(LandUseBlockController.ApproveStaging), typeof(JurisdictionEditFeature)),
            (typeof(OnlandVisualTrashAssessmentAreaController), nameof(OnlandVisualTrashAssessmentAreaController.DownloadGdb), typeof(UserViewFeature)),
            (typeof(OnlandVisualTrashAssessmentAreaController), nameof(OnlandVisualTrashAssessmentAreaController.GdbUpload), typeof(JurisdictionEditFeature)),
            (typeof(OnlandVisualTrashAssessmentAreaController), nameof(OnlandVisualTrashAssessmentAreaController.GdbApprove), typeof(JurisdictionEditFeature)),
            (typeof(DelineationGeometryController), nameof(DelineationGeometryController.Download), typeof(JurisdictionEditFeature)),
            (typeof(DelineationGeometryController), nameof(DelineationGeometryController.Upload), typeof(JurisdictionEditFeature)),
            (typeof(DelineationGeometryController), nameof(DelineationGeometryController.Approve), typeof(JurisdictionEditFeature)),
            (typeof(WaterQualityManagementPlanController), nameof(WaterQualityManagementPlanController.BulkUploadXlsx), typeof(JurisdictionEditFeature)),
            (typeof(WaterQualityManagementPlanController), nameof(WaterQualityManagementPlanController.UploadSimplifiedBMPs), typeof(JurisdictionEditFeature)),
            (typeof(WaterQualityManagementPlanController), nameof(WaterQualityManagementPlanController.UploadBoundaryFromAPNs), typeof(JurisdictionEditFeature)),
        };

        [TestMethod]
        public void ScopedEndpoints_KeepTheirRoleGates()
        {
            foreach (var (controller, methodName, roleAttribute) in ScopedEndpoints)
            {
                var method = controller.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(method, $"Expected {controller.Name}.{methodName}.");
                Assert.IsTrue(method!.GetCustomAttributes(roleAttribute, true).Any(),
                    $"{controller.Name}.{methodName} must keep [{roleAttribute.Name}] — NPT-1135 adds a jurisdiction check, it doesn't change roles.");
                Assert.IsFalse(method.GetCustomAttributes(typeof(JurisdictionManageFeature), true).Any(),
                    $"{controller.Name}.{methodName} must not be tightened to [JurisdictionManageFeature].");
            }
        }

        [TestMethod]
        public void Downloads_CanReturnForbid()
        {
            // FileResult can't carry a 403; the downloads return IActionResult so they can Forbid().
            foreach (var (controller, methodName) in new[]
                     {
                         (typeof(LandUseBlockController), nameof(LandUseBlockController.DownloadGdb)),
                         (typeof(OnlandVisualTrashAssessmentAreaController), nameof(OnlandVisualTrashAssessmentAreaController.DownloadGdb)),
                         (typeof(DelineationGeometryController), nameof(DelineationGeometryController.Download)),
                     })
            {
                var method = controller.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                Assert.AreEqual(typeof(Task<IActionResult>), method!.ReturnType, $"{controller.Name}.{methodName} should return Task<IActionResult>.");
            }
        }

        [TestMethod]
        public async Task IsPersonAssignedToJurisdiction_JurisdictionUser_OnlyTheirJurisdictions()
        {
            using var db = GetDbContext();
            var personID = db.People.AsNoTracking()
                .Where(p => (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager) && p.StormwaterJurisdictionPeople.Any())
                .Select(p => (int?)p.PersonID).FirstOrDefault();
            if (personID == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }
            var person = await People.GetByIDAsDtoAsync(db, personID.Value);

            var assignedIDs = db.StormwaterJurisdictionPeople.AsNoTracking().Where(x => x.PersonID == personID).Select(x => x.StormwaterJurisdictionID).ToList();
            foreach (var id in assignedIDs)
            {
                Assert.IsTrue(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(db, person, id), $"JE/JM should pass for assigned jurisdiction {id}.");
            }

            var unassignedID = db.StormwaterJurisdictions.AsNoTracking()
                .Where(x => !assignedIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            if (unassignedID == null) { Assert.Inconclusive("The JE/JM is assigned to every jurisdiction."); return; }

            Assert.IsFalse(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(db, person, unassignedID.Value),
                "A JE/JM must not pass for a jurisdiction they aren't assigned to.");
            Assert.IsTrue(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionsAsync(db, person, assignedIDs));
            Assert.IsFalse(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionsAsync(db, person, assignedIDs.Append(unassignedID.Value)),
                "One foreign jurisdiction in the set must fail the whole set (approve steps).");
        }

        [TestMethod]
        public async Task IsPersonAssignedToJurisdiction_Admin_AnyExistingJurisdiction()
        {
            using var db = GetDbContext();
            var adminID = db.People.AsNoTracking()
                .Where(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin)
                .Select(p => (int?)p.PersonID).FirstOrDefault();
            if (adminID == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }
            var admin = await People.GetByIDAsDtoAsync(db, adminID.Value);

            var allIDs = db.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID).ToList();
            foreach (var id in allIDs)
            {
                Assert.IsTrue(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(db, admin, id), $"Admin should pass for jurisdiction {id}.");
            }
            Assert.IsTrue(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionsAsync(db, admin, allIDs));
            Assert.IsFalse(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(db, admin, int.MaxValue),
                "A nonexistent jurisdiction is rejected even for Admin.");
        }

        [TestMethod]
        public async Task IsPersonAssignedToJurisdiction_Unassigned_RejectedForPublicJurisdictions()
        {
            // The BMP-viewable helper hands Unassigned users the public VerifiedOnly jurisdictions; this check
            // must not, or an Unassigned caller could use the UserView-gated OVTA Area download.
            using var db = GetDbContext();
            var personID = db.People.AsNoTracking()
                .Where(p => p.RoleID == (int)RoleEnum.Unassigned && !p.StormwaterJurisdictionPeople.Any())
                .Select(p => (int?)p.PersonID).FirstOrDefault();
            if (personID == null) { Assert.Inconclusive("No Unassigned person without jurisdictions in the local DB."); return; }
            var person = await People.GetByIDAsDtoAsync(db, personID.Value);

            var publicIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonDtoForBMPsAsync(db, person);
            if (publicIDs.Count == 0) { Assert.Inconclusive("No public VerifiedOnly jurisdictions in the local DB."); return; }

            foreach (var id in publicIDs)
            {
                Assert.IsFalse(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionAsync(db, person, id),
                    $"An Unassigned user must not pass for public jurisdiction {id}.");
            }
        }

        // Controller-level: the actions themselves return 403 for a foreign jurisdiction. The blob and GDAL services
        // are null, so a rejected request that reached UploadToBlobStorage or the export would throw instead (AC 12).
        private static T WithCaller<T>(T controller, string? globalID) where T : ControllerBase
        {
            var claims = globalID == null ? Array.Empty<Claim>() : new[] { new Claim(ClaimsConstants.Sub, globalID) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, globalID == null ? null : "test")),
                    RequestServices = new ServiceCollection().BuildServiceProvider(),
                },
            };
            return controller;
        }

        private static IFormFile FakeZip() => new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "File", "upload.zip");

        private static (string GlobalID, int ForeignJurisdictionID)? FindEditorAndForeignJurisdiction(NeptuneDbContext db)
        {
            var editor = db.People.AsNoTracking()
                .Where(p => (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                            && p.GlobalID != null && p.StormwaterJurisdictionPeople.Any())
                .Select(p => new { p.GlobalID, AssignedIDs = p.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToList() })
                .FirstOrDefault();
            if (editor == null) { return null; }
            var foreignID = db.StormwaterJurisdictions.AsNoTracking()
                .Where(x => !editor.AssignedIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            return foreignID == null ? null : (editor.GlobalID!, foreignID.Value);
        }

        private static LandUseBlockController NewLandUseBlockController(NeptuneDbContext db, string? globalID) =>
            WithCaller(new LandUseBlockController(db, NullLogger<LandUseBlockController>.Instance, Options.Create(new NeptuneConfiguration()), null!, null!), globalID);

        private static OnlandVisualTrashAssessmentAreaController NewOvtaAreaController(NeptuneDbContext db, string? globalID) =>
            WithCaller(new OnlandVisualTrashAssessmentAreaController(db, NullLogger<OnlandVisualTrashAssessmentAreaController>.Instance, Options.Create(new NeptuneConfiguration()), null!, null!), globalID);

        private static DelineationGeometryController NewDelineationController(NeptuneDbContext db, string? globalID) =>
            WithCaller(new DelineationGeometryController(db, NullLogger<DelineationGeometryController>.Instance, Options.Create(new NeptuneConfiguration()), null!, null!), globalID);

        [TestMethod]
        public async Task Controllers_ForeignJurisdiction_Return403WithoutStaging()
        {
            using var db = GetDbContext();
            var found = FindEditorAndForeignJurisdiction(db);
            if (found == null) { Assert.Inconclusive("No JE/JM with a GlobalID and an unassigned jurisdiction in the local DB."); return; }
            var (globalID, foreignID) = found.Value;

            Assert.IsInstanceOfType<ForbidResult>(
                await NewLandUseBlockController(db, globalID).DownloadGdb(new LandUseBlockGdbDownloadRequestDto { StormwaterJurisdictionID = foreignID }));
            Assert.IsInstanceOfType<ForbidResult>(
                await NewOvtaAreaController(db, globalID).DownloadGdb(new OvtaAreaGdbDownloadRequestDto { StormwaterJurisdictionID = foreignID }));
            Assert.IsInstanceOfType<ForbidResult>(
                await NewDelineationController(db, globalID).Download(new DelineationGdbDownloadRequestDto { StormwaterJurisdictionID = foreignID }));

            Assert.IsInstanceOfType<ForbidResult>(
                (await NewLandUseBlockController(db, globalID).UploadGdb(new LandUseBlockGdbUploadFormDto { File = FakeZip(), StormwaterJurisdictionID = foreignID })).Result);
            Assert.IsInstanceOfType<ForbidResult>(
                (await NewOvtaAreaController(db, globalID).GdbUpload(new OvtaAreaGdbUploadFormDto { File = FakeZip(), StormwaterJurisdictionID = foreignID })).Result);
            Assert.IsInstanceOfType<ForbidResult>(
                (await NewDelineationController(db, globalID).Upload(new DelineationGdbUploadFormDto { File = FakeZip(), StormwaterJurisdictionID = foreignID, TreatmentBMPNameField = "Name" })).Result);
        }

        [TestMethod]
        public async Task OvtaAreaDownload_Anonymous_Returns403()
        {
            // The OVTA Area download is UserView-gated; a caller with no assignments gets 403 for every jurisdiction.
            using var db = GetDbContext();
            var jurisdictionID = db.StormwaterJurisdictions.AsNoTracking().Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            if (jurisdictionID == null) { Assert.Inconclusive("No jurisdictions in the local DB."); return; }

            Assert.IsInstanceOfType<ForbidResult>(
                await NewOvtaAreaController(db, null).DownloadGdb(new OvtaAreaGdbDownloadRequestDto { StormwaterJurisdictionID = jurisdictionID.Value }));
        }

        [TestMethod]
        public async Task IsPersonAssignedToJurisdictions_EmptySet_Passes()
        {
            // An approve with nothing staged is rejected later by its own validation, not as a 403.
            using var db = GetDbContext();
            var person = new PersonDto { PersonID = -1, RoleID = (int)RoleEnum.JurisdictionEditor };
            Assert.IsTrue(await StormwaterJurisdictionPeople.IsPersonAssignedToJurisdictionsAsync(db, person, Array.Empty<int>()));
        }
    }
}
