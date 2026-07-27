using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1114 — covers the data query behind the regenerated Trash Screen Field Visit upload
    /// template (DataHubController.DownloadTrashScreenUploadTemplate). The endpoint's Excel/blob
    /// writing is integration-only (it needs the base template blob), so these tests assert the
    /// query that feeds the pre-populated rows: TreatmentBMPs.ListByTypeAsGridDtoForJurisdictionsAsync
    /// scoped to the "Inlet and Trash Screen" type (35) returns only type-35, non-planning-module
    /// BMPs within the caller's viewable jurisdictions, with custom-attribute values keyed by
    /// CustomAttributeTypeID. Reads the local dev DB (read-only); Inconclusive when the dev DB has
    /// no trash-screen BMPs.
    /// </summary>
    [TestClass]
    public class DataHubTrashScreenTemplateTests
    {
        private const int InletAndTrashScreenTreatmentBMPTypeID = 35;

        private readonly NeptuneDbContext _dbContext = GetDbContext();

        private static NeptuneDbContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<NeptuneDbContext>();
            optionsBuilder.UseSqlServer(
                "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;", x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
                    x.UseNetTopologySuite();
                });
            return new NeptuneDbContext(optionsBuilder.Options);
        }

        [TestMethod]
        public async Task ListByType_TrashScreen_ReturnsOnlyType35NonPlanningBMPsInScope()
        {
            // All jurisdictions that actually have a trash-screen BMP — mirrors an admin's viewable set.
            var jurisdictionIDs = await _dbContext.TreatmentBMPs.AsNoTracking()
                .Where(x => x.ProjectID == null && x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID)
                .Select(x => x.StormwaterJurisdictionID)
                .Distinct()
                .ToListAsync();
            if (jurisdictionIDs.Count == 0)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }

            var rows = await TreatmentBMPs.ListByTypeAsGridDtoForJurisdictionsAsync(
                _dbContext, InletAndTrashScreenTreatmentBMPTypeID, jurisdictionIDs);

            Assert.IsTrue(rows.Count > 0, "Expected at least one trash-screen BMP row.");
            Assert.IsTrue(rows.All(r => jurisdictionIDs.Contains(r.StormwaterJurisdictionID)),
                "Every row must belong to one of the requested jurisdictions.");

            // Confirm the returned IDs are all genuinely type-35, ProjectID == null BMPs.
            var returnedIDs = rows.Select(r => r.TreatmentBMPID).ToList();
            var mismatches = await _dbContext.TreatmentBMPs.AsNoTracking()
                .Where(x => returnedIDs.Contains(x.TreatmentBMPID)
                            && (x.ProjectID != null || x.TreatmentBMPTypeID != InletAndTrashScreenTreatmentBMPTypeID))
                .CountAsync();
            Assert.AreEqual(0, mismatches, "All returned BMPs must be non-planning-module Inlet-And-Trash-Screen BMPs.");

            // The template reads the three trash-screen custom attributes by type ID, so the DTO
            // dict must be keyed by CustomAttributeTypeID and never null (null-safe TryGetValue).
            Assert.IsTrue(rows.All(r => r.CustomAttributeValues != null),
                "CustomAttributeValues must be initialized so the template lookup is null-safe.");
        }

        [TestMethod]
        public async Task ListByType_TrashScreen_ScopesToRequestedJurisdiction()
        {
            var firstJurisdictionWithTrashScreen = await _dbContext.TreatmentBMPs.AsNoTracking()
                .Where(x => x.ProjectID == null && x.TreatmentBMPTypeID == InletAndTrashScreenTreatmentBMPTypeID)
                .Select(x => x.StormwaterJurisdictionID)
                .FirstOrDefaultAsync();
            if (firstJurisdictionWithTrashScreen == 0)
            {
                Assert.Inconclusive("No Inlet-And-Trash-Screen BMPs in dev DB.");
                return;
            }

            var rows = await TreatmentBMPs.ListByTypeAsGridDtoForJurisdictionsAsync(
                _dbContext, InletAndTrashScreenTreatmentBMPTypeID, new[] { firstJurisdictionWithTrashScreen });

            Assert.IsTrue(rows.Count > 0, "Expected at least one trash-screen BMP row for the seeded jurisdiction.");
            Assert.IsTrue(rows.All(r => r.StormwaterJurisdictionID == firstJurisdictionWithTrashScreen),
                "Passing a single jurisdiction must not leak BMPs from other jurisdictions.");
        }
    }
}
