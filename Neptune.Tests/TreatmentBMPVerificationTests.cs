using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1117 — single-BMP inventory verify / mark-provisional. Entity + authorization + projection.
    /// DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive when the data isn't present.
    /// </summary>
    [TestClass]
    public class TreatmentBMPVerificationTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer("Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        // --- Entity behavior (DB-independent) ---

        [TestMethod]
        public void MarkAsVerified_SetsFlagDateAndVerifier()
        {
            var bmp = new TreatmentBMP();
            bmp.MarkAsVerified(new Person { PersonID = 4242 });

            Assert.IsTrue(bmp.InventoryIsVerified);
            Assert.IsNotNull(bmp.DateOfLastInventoryVerification);
            Assert.AreEqual(4242, bmp.InventoryVerifiedByPersonID);
        }

        [TestMethod]
        public void MarkAsProvisional_UnsetsFlag_PreservesAuditTrail()
        {
            var when = new DateTime(2026, 3, 14, 8, 0, 0, DateTimeKind.Utc);
            var bmp = new TreatmentBMP { InventoryIsVerified = true, DateOfLastInventoryVerification = when, InventoryVerifiedByPersonID = 99 };

            bmp.MarkAsProvisional();

            Assert.IsFalse(bmp.InventoryIsVerified);
            Assert.AreEqual(when, bmp.DateOfLastInventoryVerification, "Verification date must be preserved as the audit trail.");
            Assert.AreEqual(99, bmp.InventoryVerifiedByPersonID, "Verifier must be preserved as the audit trail.");
        }

        // --- CanManageJurisdiction authorization ---

        [TestMethod]
        public async Task CanManageJurisdiction_AdminAndSitkaAdmin_AlwaysTrue()
        {
            using var db = GetDbContext();
            Assert.IsTrue(await new PersonDto { PersonID = 1, RoleID = (int)RoleEnum.Admin }.CanManageJurisdiction(999999, db));
            Assert.IsTrue(await new PersonDto { PersonID = 1, RoleID = (int)RoleEnum.SitkaAdmin }.CanManageJurisdiction(999999, db));
        }

        [TestMethod]
        public async Task CanManageJurisdiction_EditorAndUnassigned_False()
        {
            using var db = GetDbContext();
            // An Editor can EDIT but not MANAGE (verify) — this is the intended contrast with CanEditJurisdiction.
            Assert.IsFalse(await new PersonDto { PersonID = 1, RoleID = (int)RoleEnum.JurisdictionEditor }.CanManageJurisdiction(999999, db));
            Assert.IsFalse(await new PersonDto { PersonID = 1, RoleID = (int)RoleEnum.Unassigned }.CanManageJurisdiction(999999, db));
        }

        [TestMethod]
        public async Task CanManageJurisdiction_JurisdictionManager_ScopedToAssignedJurisdiction()
        {
            using var db = GetDbContext();
            var jm = db.People.AsNoTracking()
                .FirstOrDefault(p => p.RoleID == (int)RoleEnum.JurisdictionManager && p.StormwaterJurisdictionPeople.Any());
            if (jm == null) { Assert.Inconclusive("No JurisdictionManager with an assigned jurisdiction in the local DB."); return; }

            var dto = new PersonDto { PersonID = jm.PersonID, RoleID = jm.RoleID };
            var assigned = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, jm.PersonID);
            if (!assigned.Any()) { Assert.Inconclusive("JM has no viewable jurisdictions."); return; }

            Assert.IsTrue(await dto.CanManageJurisdiction(assigned.First(), db), "JM manages an assigned jurisdiction.");

            var outJurisdiction = db.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID)
                .AsEnumerable().FirstOrDefault(id => !assigned.Contains(id));
            if (outJurisdiction == 0) { Assert.Inconclusive("No unassigned jurisdiction to test against."); return; }
            Assert.IsFalse(await dto.CanManageJurisdiction(outJurisdiction, db), "JM cannot manage an unassigned jurisdiction.");
        }

        // --- Projection populates the verifier display name ---

        [TestMethod]
        public async Task GetByIDAsDto_VerifiedBmp_PopulatesInventoryVerifiedByPersonName()
        {
            using var db = GetDbContext();
            var verifiedId = db.TreatmentBMPs.AsNoTracking()
                .Where(x => x.InventoryIsVerified && x.InventoryVerifiedByPersonID != null)
                .Select(x => x.TreatmentBMPID).FirstOrDefault();
            if (verifiedId == 0) { Assert.Inconclusive("No verified BMP with a verifier in the local DB."); return; }

            var dto = await TreatmentBMPs.GetByIDAsDtoAsync(db, verifiedId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(dto.InventoryVerifiedByPersonName),
                "InventoryVerifiedByPersonName should be populated for a verified BMP.");
        }
    }
}
