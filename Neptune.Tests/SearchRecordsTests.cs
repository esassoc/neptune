using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.Search;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1125 — the header record search index. DB-backed tests read the local NeptuneDB (read-only) and are
    /// Inconclusive when the data isn't present.
    /// </summary>
    [TestClass]
    public class SearchRecordsTests
    {
        private static readonly string[] Scopes =
        [
            SearchRecordDto.TreatmentBMPScope,
            SearchRecordDto.WaterQualityManagementPlanScope,
            SearchRecordDto.OnlandVisualTrashAssessmentScope,
            SearchRecordDto.ProjectScope
        ];

        private static NeptuneDbContext GetDbContext()
        {
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer("Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;",
                x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static Person GetAdmin(NeptuneDbContext db)
        {
            return db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin && p.IsActive);
        }

        private static async Task<List<SearchRecordDto>> ListForPersonAsync(NeptuneDbContext db, Person person)
        {
            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, person.PersonID);
            return await SearchRecords.ListForJurisdictionsAsync(db, jurisdictionIDs);
        }

        [TestMethod]
        public async Task AdminIndex_CoversEveryJurisdictionWithRecords()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) Assert.Inconclusive("No active Admin in the local DB.");

            var records = await ListForPersonAsync(db, admin);
            if (records.Count == 0) Assert.Inconclusive("No searchable records in the local DB.");

            var jurisdictionsWithInventoriedBMPs = await db.TreatmentBMPs.AsNoTracking()
                .Where(x => x.ProjectID == null && x.TreatmentBMPName != null && x.TreatmentBMPName.Trim() != "")
                .Select(x => x.StormwaterJurisdictionID).Distinct().CountAsync();
            var bmpCount = await db.TreatmentBMPs.AsNoTracking()
                .CountAsync(x => x.ProjectID == null && x.TreatmentBMPName != null && x.TreatmentBMPName.Trim() != "");

            Assert.IsTrue(jurisdictionsWithInventoriedBMPs > 1, "Expected inventoried BMPs in more than one jurisdiction.");
            Assert.AreEqual(bmpCount, records.Count(x => x.Scope == SearchRecordDto.TreatmentBMPScope),
                "An Admin's index should hold every named inventoried BMP across all jurisdictions.");
        }

        [TestMethod]
        public async Task JurisdictionManagerIndex_OnlyHoldsAssignedJurisdictions()
        {
            using var db = GetDbContext();
            var jm = db.People.AsNoTracking().Include(p => p.StormwaterJurisdictionPeople)
                .FirstOrDefault(p => p.RoleID == (int)RoleEnum.JurisdictionManager && p.IsActive && p.StormwaterJurisdictionPeople.Any());
            if (jm == null) Assert.Inconclusive("No active JurisdictionManager with an assigned jurisdiction in the local DB.");

            var assigned = jm.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToHashSet();
            var records = await ListForPersonAsync(db, jm);
            if (records.Count == 0) Assert.Inconclusive("The JurisdictionManager's jurisdictions hold no searchable records.");

            var bmpIDs = records.Where(x => x.Scope == SearchRecordDto.TreatmentBMPScope).Select(x => x.ID).ToList();
            var outside = await db.TreatmentBMPs.AsNoTracking()
                .CountAsync(x => bmpIDs.Contains(x.TreatmentBMPID) && !assigned.Contains(x.StormwaterJurisdictionID));
            Assert.AreEqual(0, outside, "A JurisdictionManager's index must not contain BMPs from unassigned jurisdictions.");

            var wqmpIDs = records.Where(x => x.Scope == SearchRecordDto.WaterQualityManagementPlanScope).Select(x => x.ID).ToList();
            var outsideWqmps = await db.WaterQualityManagementPlans.AsNoTracking()
                .CountAsync(x => wqmpIDs.Contains(x.WaterQualityManagementPlanID) && !assigned.Contains(x.StormwaterJurisdictionID));
            Assert.AreEqual(0, outsideWqmps, "A JurisdictionManager's index must not contain WQMPs from unassigned jurisdictions.");
        }

        [TestMethod]
        public async Task Index_ExcludesPlanningProjectBMPs()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) Assert.Inconclusive("No active Admin in the local DB.");

            var planningBMPIDs = await db.TreatmentBMPs.AsNoTracking().Where(x => x.ProjectID != null).Select(x => x.TreatmentBMPID).ToListAsync();
            if (planningBMPIDs.Count == 0) Assert.Inconclusive("No planning-project BMPs in the local DB.");

            var records = await ListForPersonAsync(db, admin);
            var planning = planningBMPIDs.ToHashSet();
            Assert.IsFalse(records.Any(x => x.Scope == SearchRecordDto.TreatmentBMPScope && planning.Contains(x.ID)),
                "Planning-project BMPs are reached through their Project, not listed as inventoried BMPs.");
        }

        [TestMethod]
        public async Task Index_RowsAreWellFormed()
        {
            using var db = GetDbContext();
            var admin = GetAdmin(db);
            if (admin == null) Assert.Inconclusive("No active Admin in the local DB.");

            var records = await ListForPersonAsync(db, admin);
            if (records.Count == 0) Assert.Inconclusive("No searchable records in the local DB.");

            Assert.IsFalse(records.Any(x => string.IsNullOrWhiteSpace(x.Title)), "Every row needs a title to match against.");
            CollectionAssert.IsSubsetOf(records.Select(x => x.Scope).Distinct().ToList(), Scopes, "Unexpected scope value.");
            Assert.AreEqual(records.Count, records.Select(x => (x.Scope, x.ID)).Distinct().Count(), "Each record should appear once.");
        }

        [TestMethod]
        public async Task EmptyJurisdictionList_ReturnsNothing()
        {
            using var db = GetDbContext();
            var records = await SearchRecords.ListForJurisdictionsAsync(db, []);
            Assert.AreEqual(0, records.Count);
        }
    }
}
