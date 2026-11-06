using System;
using System.Linq;
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
using Neptune.Common;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1135 rework (KE 10/30): the Land Use Blocks grid shows Admin/SitkaAdmin every block and everyone else only their
    /// assigned jurisdictions' blocks, and both the grid and the GDB export stop loading every Trash Generating Unit entity.
    /// DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive when the data isn't present.
    /// Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB (e.g. a devcontainer).
    /// </summary>
    [TestClass]
    public class LandUseBlockGridAndExportTests
    {
        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(600); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        // The jurisdiction with the fewest blocks keeps the in-memory comparisons fast.
        private static int? SmallestJurisdictionWithBlocks(NeptuneDbContext db) =>
            db.LandUseBlocks.AsNoTracking().GroupBy(x => x.StormwaterJurisdictionID)
                .OrderBy(g => g.Count()).Select(g => (int?)g.Key).FirstOrDefault();

        [TestMethod]
        public async Task Grid_JurisdictionUser_OnlyTheirJurisdictions()
        {
            using var db = GetDbContext();
            var personID = db.People.AsNoTracking()
                .Where(p => (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                            && p.StormwaterJurisdictionPeople.Any(sjp => db.LandUseBlocks.Any(l => l.StormwaterJurisdictionID == sjp.StormwaterJurisdictionID)))
                .Select(p => (int?)p.PersonID).FirstOrDefault();
            if (personID == null) { Assert.Inconclusive("No JE/JM assigned to a jurisdiction with Land Use Blocks in the local DB."); return; }
            var person = await People.GetByIDAsDtoAsync(db, personID.Value);
            var assignedIDs = db.StormwaterJurisdictionPeople.AsNoTracking().Where(x => x.PersonID == personID).Select(x => x.StormwaterJurisdictionID).ToList();

            var rows = await LandUseBlocks.ListForPersonAsync(db, person);

            Assert.IsTrue(rows.Count > 0);
            Assert.IsTrue(rows.All(x => assignedIDs.Contains(x.StormwaterJurisdictionID)), "A JE/JM must only see blocks in their assigned jurisdictions.");
            Assert.AreEqual(db.LandUseBlocks.Count(x => assignedIDs.Contains(x.StormwaterJurisdictionID)), rows.Count, "...and every block in them.");
        }

        [TestMethod]
        public async Task Grid_Admin_EveryBlock()
        {
            using var db = GetDbContext();
            var admin = new PersonDto { PersonID = -1, RoleID = (int)RoleEnum.Admin };
            var total = db.LandUseBlocks.Count();
            if (total == 0) { Assert.Inconclusive("No Land Use Blocks in the local DB."); return; }

            var rows = await LandUseBlocks.ListForPersonAsync(db, admin);

            Assert.AreEqual(total, rows.Count);
        }

        [TestMethod]
        public async Task Grid_UnassignedAndAnonymous_SeeNothing()
        {
            using var db = GetDbContext();
            var unassignedID = db.People.AsNoTracking()
                .Where(p => p.RoleID == (int)RoleEnum.Unassigned && !p.StormwaterJurisdictionPeople.Any())
                .Select(p => (int?)p.PersonID).FirstOrDefault();
            if (unassignedID != null)
            {
                Assert.AreEqual(0, (await LandUseBlocks.ListForPersonAsync(db, await People.GetByIDAsDtoAsync(db, unassignedID.Value))).Count);
            }

            // The [AllowAnonymous] endpoint: no claims resolves to the anonymous Unassigned person.
            var controller = new LandUseBlockController(db, NullLogger<LandUseBlockController>.Instance, Options.Create(new NeptuneConfiguration()), null!, null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()), RequestServices = new ServiceCollection().BuildServiceProvider() },
                },
            };
            var result = (OkObjectResult)(await controller.List()).Result!;
            Assert.AreEqual(0, ((System.Collections.Generic.List<LandUseBlockGridDto>)result.Value!).Count, "An anonymous caller must get an empty grid.");
        }

        [TestMethod]
        public async Task Grid_ValuesMatchTheInMemoryCalculation()
        {
            // The grid now computes areas with SQL STArea; it must agree with the old NTS calculation well past the
            // 2 decimal places the grid shows.
            using var db = GetDbContext();
            var jurisdictionID = SmallestJurisdictionWithBlocks(db);
            if (jurisdictionID == null) { Assert.Inconclusive("No Land Use Blocks in the local DB."); return; }

            var expected = db.LandUseBlocks.AsNoTracking()
                .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
                .Include(x => x.TrashGeneratingUnits)
                .Where(x => x.StormwaterJurisdictionID == jurisdictionID)
                .ToDictionary(x => x.LandUseBlockID);
            var admin = new PersonDto { PersonID = -1, RoleID = (int)RoleEnum.Admin };
            var actual = (await LandUseBlocks.ListForPersonAsync(db, admin)).Where(x => x.StormwaterJurisdictionID == jurisdictionID).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var dto in actual)
            {
                var block = expected[dto.LandUseBlockID];
                AssertClose(block.LandUseBlockGeometry.Area * Constants.SquareMetersToAcres, dto.Area, $"Area of block {dto.LandUseBlockID}");
                AssertClose(block.TrashGeneratingUnits.Sum(y => y.TrashGeneratingUnitGeometry.Area) * Constants.SquareMetersToAcres, dto.TrashGeneratingArea,
                    $"TrashGeneratingArea of block {dto.LandUseBlockID}");
                Assert.AreEqual(block.StormwaterJurisdiction.GetOrganizationDisplayName(), dto.StormwaterJurisdictionName);
                Assert.AreEqual(block.PriorityLandUseTypeID.HasValue ? PriorityLandUseType.AllLookupDictionary[block.PriorityLandUseTypeID.Value].PriorityLandUseTypeDisplayName : null,
                    dto.PriorityLandUseTypeName);
                Assert.AreEqual(PermitType.AllLookupDictionary[block.PermitTypeID].PermitTypeDisplayName, dto.PermitTypeName);
            }
        }

        private static void AssertClose(double expected, double actual, string what)
        {
            // Relative 1e-4 or absolute 1e-6 acres, whichever is looser.
            Assert.IsTrue(Math.Abs(expected - actual) <= Math.Max(1e-6, Math.Abs(expected) * 1e-4), $"{what}: expected {expected}, got {actual}.");
        }

        [TestMethod]
        public async Task Export_RowsMatchTheOldEntityCalculationExactly()
        {
            // AC 5: the export keeps the same values. TrashResultsArea is still summed in .NET (not STArea), so it's exact.
            using var db = GetDbContext();
            var jurisdictionID = SmallestJurisdictionWithBlocks(db);
            if (jurisdictionID == null) { Assert.Inconclusive("No Land Use Blocks in the local DB."); return; }

            var expected = db.LandUseBlocks.AsNoTracking().Include(x => x.TrashGeneratingUnits)
                .Where(x => x.StormwaterJurisdictionID == jurisdictionID).ToDictionary(x => x.LandUseBlockID);
            var rows = await LandUseBlockGdbExport.ListExportRowsAsync(db, jurisdictionID.Value);

            Assert.AreEqual(expected.Count, rows.Count);
            foreach (var row in rows)
            {
                var block = expected[row.LandUseBlockID];
                Assert.AreEqual(block.TrashGeneratingUnits.Sum(y => y.TrashGeneratingUnitGeometry.Area), row.TrashResultsArea, $"TrashResultsArea of block {row.LandUseBlockID}");
                Assert.IsTrue(block.LandUseBlockGeometry.EqualsExact(row.Geometry), $"Geometry of block {row.LandUseBlockID}");
                Assert.AreEqual(block.PermitTypeID, row.PermitTypeID);
                Assert.AreEqual(block.PriorityLandUseTypeID, row.PriorityLandUseTypeID);
            }
        }

        private static LandUseBlockGdbExport.ExportRow Row(int? priorityLandUseTypeID) => new()
        {
            LandUseBlockID = 1,
            PriorityLandUseTypeID = priorityLandUseTypeID,
            PermitTypeID = PermitType.All.First().PermitTypeID,
            Geometry = new GeometryFactory().CreatePolygon(new[] { new Coordinate(0, 0), new Coordinate(0, 100), new Coordinate(100, 100), new Coordinate(100, 0), new Coordinate(0, 0) }),
            TrashResultsArea = 5000,
        };

        [TestMethod]
        public void Export_FeatureAttributes_KeepNamesAndOrder()
        {
            var feature = LandUseBlockGdbExport.ToFeatureCollection(new[] { Row(PriorityLandUseType.All.First().PriorityLandUseTypeID) }, "City of Somewhere").Single();

            CollectionAssert.AreEqual(LandUseBlockGdbExport.AttributeNames, feature.Attributes.GetNames());
            Assert.AreEqual(10000 * Constants.SquareMetersToAcres, (double)feature.Attributes["BlockArea"], 1e-12);
            Assert.AreEqual(5000 * Constants.SquareMetersToAcres, (double)feature.Attributes["TrashResultsArea"], 1e-12);
            Assert.AreEqual("City of Somewhere", feature.Attributes["StormwaterJurisdiction"]);
        }

        [TestMethod]
        public void Export_NullPriorityLandUseType_WritesNull()
        {
            // The column is nullable; the old (int) cast threw on a null and failed the whole download.
            var feature = LandUseBlockGdbExport.ToFeatureCollection(new[] { Row(null) }, "City of Somewhere").Single();
            Assert.IsNull(feature.Attributes["PriorityLandUseType"]);
        }

        [TestMethod]
        public void Export_NoBlocks_WritesOneSchemaRow()
        {
            var feature = LandUseBlockGdbExport.ToFeatureCollection(Array.Empty<LandUseBlockGdbExport.ExportRow>(), "City of Somewhere").Single();
            Assert.IsNull(feature.Geometry);
            CollectionAssert.AreEqual(LandUseBlockGdbExport.AttributeNames, feature.Attributes.GetNames());
        }
    }
}
