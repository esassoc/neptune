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
    /// NPT-1137 AC 12-14: a simple BMP appears on the Simple BMPs tab only if its WQMP appears on the WQMPs tab.
    /// For each kind of caller, the Simple BMP grid must hold exactly the QuickBMPs of the WQMPs that
    /// ListViewableByPersonDtoAsync returns. DB-backed: reads the local NeptuneDB (read-only) and is Inconclusive
    /// when the data isn't present. Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB.
    /// </summary>
    [TestClass]
    public class QuickBMPGridScopingTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static async Task AssertGridMatchesViewableWqmps(NeptuneDbContext db, PersonDto personDto)
        {
            var viewableWqmpIDs = (await vWaterQualityManagementPlanDetaileds.ListViewableByPersonDtoAsync(db, personDto))
                .Select(x => x.WaterQualityManagementPlanID).ToHashSet();
            var grid = await QuickBMPs.ListViewableByPersonAsGridDtoAsync(db, personDto);

            var expectedQuickBMPIDs = db.QuickBMPs.AsNoTracking()
                .Where(x => viewableWqmpIDs.Contains(x.WaterQualityManagementPlanID))
                .Select(x => x.QuickBMPID).ToList();
            CollectionAssert.AreEquivalent(expectedQuickBMPIDs, grid.Select(x => x.QuickBMPID).ToList(),
                "The grid must hold exactly the simple BMPs of the WQMPs on the WQMPs tab.");
            Assert.IsTrue(grid.All(x => viewableWqmpIDs.Contains(x.WaterQualityManagementPlanID)),
                "A simple BMP leaked from a WQMP the caller can't view.");
        }

        [TestMethod]
        public async Task JurisdictionUser_SeesOnlySimpleBMPsOfViewableWqmps()
        {
            using var db = GetDbContext();
            var person = db.People.AsNoTracking().FirstOrDefault(p =>
                (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                && p.StormwaterJurisdictionPeople.Any());
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var personDto = new PersonDto { PersonID = person.PersonID, RoleID = person.RoleID };
            await AssertGridMatchesViewableWqmps(db, personDto);

            var assignedJurisdictionIDs = db.StormwaterJurisdictionPeople.AsNoTracking()
                .Where(x => x.PersonID == person.PersonID).Select(x => x.StormwaterJurisdictionID).ToList();
            var grid = await QuickBMPs.ListViewableByPersonAsGridDtoAsync(db, personDto);
            Assert.IsTrue(grid.All(x => assignedJurisdictionIDs.Contains(x.StormwaterJurisdictionID)),
                "A jurisdiction user must only see simple BMPs from their assigned jurisdictions.");
        }

        [TestMethod]
        public async Task Admin_SeesSimpleBMPsOfViewableWqmps()
        {
            using var db = GetDbContext();
            var admin = db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            await AssertGridMatchesViewableWqmps(db, new PersonDto { PersonID = admin.PersonID, RoleID = admin.RoleID });
        }

        [TestMethod]
        public async Task Anonymous_SeesSimpleBMPsOfPublicWqmpsOnly()
        {
            using var db = GetDbContext();
            await AssertGridMatchesViewableWqmps(db, null);
        }
    }
}
