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
    /// NPT-1123 rework — POST /treatment-bmps must reject a jurisdiction the caller isn't assigned to
    /// (admins may use any). DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive
    /// when the data isn't present. Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB (e.g. a devcontainer).
    /// </summary>
    [TestClass]
    public class TreatmentBMPCreateJurisdictionTests
    {
        private const string JurisdictionErrorType = "StormwaterJurisdictionID";

        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static TreatmentBMPCreateDto BuildCreateDto(NeptuneDbContext db, int stormwaterJurisdictionID) => new()
        {
            TreatmentBMPName = $"NPT-1123 test {Guid.NewGuid():N}",
            TreatmentBMPTypeID = db.TreatmentBMPTypes.AsNoTracking().Select(x => x.TreatmentBMPTypeID).First(),
            StormwaterJurisdictionID = stormwaterJurisdictionID,
        };

        [TestMethod]
        public async Task ValidateCreate_JurisdictionUser_UnassignedJurisdiction_IsRejected()
        {
            using var db = GetDbContext();
            var person = db.People.AsNoTracking().FirstOrDefault(p =>
                (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                && p.StormwaterJurisdictionPeople.Any());
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var assignedIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, person.PersonID);
            var unassignedID = db.StormwaterJurisdictions.AsNoTracking()
                .Where(x => !assignedIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            if (unassignedID == null) { Assert.Inconclusive("The JE/JM is assigned to every jurisdiction."); return; }

            var errors = await TreatmentBMPs.ValidateCreateAsync(db, BuildCreateDto(db, unassignedID.Value), person.PersonID);

            Assert.IsTrue(errors.Any(e => e.Type == JurisdictionErrorType), "A JE/JM must not be able to create a BMP in a jurisdiction they aren't assigned to.");
        }

        [TestMethod]
        public async Task ValidateCreate_JurisdictionUser_AssignedJurisdiction_IsAllowed()
        {
            using var db = GetDbContext();
            var person = db.People.AsNoTracking().FirstOrDefault(p =>
                (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                && p.StormwaterJurisdictionPeople.Any());
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var assignedID = db.StormwaterJurisdictionPeople.AsNoTracking()
                .Where(x => x.PersonID == person.PersonID).Select(x => x.StormwaterJurisdictionID).First();

            var errors = await TreatmentBMPs.ValidateCreateAsync(db, BuildCreateDto(db, assignedID), person.PersonID);

            Assert.IsFalse(errors.Any(e => e.Type == JurisdictionErrorType), "A JE/JM must be able to create a BMP in an assigned jurisdiction.");
        }

        [TestMethod]
        public async Task ValidateCreate_Admin_AnyJurisdiction_IsAllowed()
        {
            using var db = GetDbContext();
            var admin = db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            var anyJurisdictionID = db.StormwaterJurisdictions.AsNoTracking()
                .Where(x => !x.StormwaterJurisdictionPeople.Any(sjp => sjp.PersonID == admin.PersonID))
                .Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault()
                ?? db.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID).First();

            var errors = await TreatmentBMPs.ValidateCreateAsync(db, BuildCreateDto(db, anyJurisdictionID), admin.PersonID);

            Assert.IsFalse(errors.Any(e => e.Type == JurisdictionErrorType), "Admins may create BMPs in any jurisdiction.");
        }
    }
}
