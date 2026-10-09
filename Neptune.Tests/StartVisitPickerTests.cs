using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1122 — jurisdiction-scoped BMP / WQMP picker lists behind the "Start Field Visit" (Field Records) and
    /// "Start O&amp;M Visit" (WQMP O&amp;M Verifications) modals. Mirrors the controller wiring: resolve the caller's
    /// viewable jurisdictions, then list. DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive
    /// when the data isn't present. Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB (e.g. a devcontainer).
    /// </summary>
    [TestClass]
    public class StartVisitPickerTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static Person FindJurisdictionUser(NeptuneDbContext db) =>
            db.People.AsNoTracking().FirstOrDefault(p =>
                (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                && p.StormwaterJurisdictionPeople.Any());

        private static Person FindAdmin(NeptuneDbContext db) =>
            db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin);

        // --- BMP picker ---

        [TestMethod]
        public async Task BMPPicker_JurisdictionUser_OnlyAssignedJurisdictions()
        {
            using var db = GetDbContext();
            var person = FindJurisdictionUser(db);
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, person.PersonID);
            var picker = await TreatmentBMPs.ListAsMinimalDtoForJurisdictionsAsync(db, jurisdictionIDs);

            var expectedIDs = db.TreatmentBMPs.AsNoTracking()
                .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => x.TreatmentBMPID).ToHashSet();
            CollectionAssert.AreEquivalent(expectedIDs.ToList(), picker.Select(x => x.TreatmentBMPID).ToList(),
                "Picker must contain exactly the BMPs in the user's assigned jurisdictions.");

            var outOfScopeBMPID = db.TreatmentBMPs.AsNoTracking()
                .Where(x => !jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.TreatmentBMPID).FirstOrDefault();
            if (outOfScopeBMPID != null)
            {
                Assert.IsFalse(picker.Any(x => x.TreatmentBMPID == outOfScopeBMPID), "Out-of-jurisdiction BMP leaked into the picker.");
            }
        }

        [TestMethod]
        public async Task BMPPicker_Admin_SeesAllBMPs()
        {
            using var db = GetDbContext();
            var admin = FindAdmin(db);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, admin.PersonID);
            var picker = await TreatmentBMPs.ListAsMinimalDtoForJurisdictionsAsync(db, jurisdictionIDs);

            Assert.AreEqual(db.TreatmentBMPs.Count(), picker.Count, "Admin/SitkaAdmin must see BMPs across all jurisdictions.");
        }

        [TestMethod]
        public async Task BMPPicker_OrderedByNameWithTypeName()
        {
            using var db = GetDbContext();
            var admin = FindAdmin(db);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, admin.PersonID);
            var picker = await TreatmentBMPs.ListAsMinimalDtoForJurisdictionsAsync(db, jurisdictionIDs);
            if (!picker.Any()) { Assert.Inconclusive("No BMPs in the local DB."); return; }

            // Compare against the DB's own ordering (collation), not .NET's string comparer.
            var dbOrdered = db.TreatmentBMPs.AsNoTracking().OrderBy(x => x.TreatmentBMPName).Select(x => x.TreatmentBMPName).ToList();
            CollectionAssert.AreEqual(dbOrdered, picker.Select(x => x.TreatmentBMPName).ToList());
            Assert.IsTrue(picker.All(x => !string.IsNullOrEmpty(x.TreatmentBMPTypeName)), "Picker rows must carry the BMP type name.");
        }

        // --- WQMP picker ---

        [TestMethod]
        public async Task WQMPPicker_JurisdictionUser_OnlyAssignedJurisdictions()
        {
            using var db = GetDbContext();
            var person = FindJurisdictionUser(db);
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, person.PersonID);
            var picker = await WaterQualityManagementPlans.ListAsDisplayDtoForJurisdictionsAsync(db, jurisdictionIDs);

            var expectedIDs = db.WaterQualityManagementPlans.AsNoTracking()
                .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => x.WaterQualityManagementPlanID).ToList();
            CollectionAssert.AreEquivalent(expectedIDs, picker.Select(x => x.WaterQualityManagementPlanID).ToList(),
                "Picker must contain exactly the WQMPs in the user's assigned jurisdictions.");

            var outOfScopeWQMPID = db.WaterQualityManagementPlans.AsNoTracking()
                .Where(x => !jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.WaterQualityManagementPlanID).FirstOrDefault();
            if (outOfScopeWQMPID != null)
            {
                Assert.IsFalse(picker.Any(x => x.WaterQualityManagementPlanID == outOfScopeWQMPID), "Out-of-jurisdiction WQMP leaked into the picker.");
            }
        }

        [TestMethod]
        public async Task WQMPPicker_Admin_SeesAllWQMPsOrderedByName()
        {
            using var db = GetDbContext();
            var admin = FindAdmin(db);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, admin.PersonID);
            var picker = await WaterQualityManagementPlans.ListAsDisplayDtoForJurisdictionsAsync(db, jurisdictionIDs);

            Assert.AreEqual(db.WaterQualityManagementPlans.Count(), picker.Count, "Admin/SitkaAdmin must see WQMPs across all jurisdictions.");
            var dbOrdered = db.WaterQualityManagementPlans.AsNoTracking().OrderBy(x => x.WaterQualityManagementPlanName)
                .Select(x => x.WaterQualityManagementPlanName).ToList();
            CollectionAssert.AreEqual(dbOrdered, picker.Select(x => x.WaterQualityManagementPlanName).ToList());
        }
    }
}
