using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Controllers;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1134: the Trash Analysis Area GDB download carries every Trash Analysis Areas grid column (plus the
    /// BMP / WQMP / OVTA IDs) and is limited to the jurisdictions the caller sees in the grid. The GDAL zip step is
    /// integration-only, so the content tests target <see cref="TrashGeneratingUnitGdbExport.ToFeatureCollection"/>.
    /// DB-backed tests read the local NeptuneDB (read-only) and are Inconclusive when the data isn't present.
    /// Set NEPTUNE_TEST_CONNECTION_STRING to run against a non-localhost DB (e.g. a devcontainer).
    /// </summary>
    [TestClass]
    public class TrashGeneratingUnitGdbExportTests
    {
        private static readonly string[] ExpectedAttributeNames =
        {
            "TrashAnalysisAreaID", "LandUseBlockID", "LandUseType", "OVTAAreaName", "OVTAAreaID", "OVTAAreaBaselineScore",
            "TreatmentBMPName", "TreatmentBMPID", "WQMPName", "WQMPID", "Jurisdiction", "AreaAcres",
            "BaselineTGR", "ProgressTGR", "CurrentNetLoadingRate", "TrashCaptureStatusBMP", "TrashCaptureStatusWQMP",
            "TrashCaptureEffectivenessBMP", "TrashCaptureEffectivenessWQMP", "MedianHouseholdIncomeResidential",
            "MedianHouseholdIncomeRetail", "PermitClass", "LandUseForTGR", "LandUseDefaultTGR", "LastUpdateDate",
        };

        private static NeptuneDbContext GetDbContext()
        {
            var connectionString = Environment.GetEnvironmentVariable("NEPTUNE_TEST_CONNECTION_STRING")
                ?? "Data Source=localhost;Initial Catalog=NeptuneDB;Persist Security Info=True;Integrated Security=true;Encrypt=False;";
            var ob = new DbContextOptionsBuilder<NeptuneDbContext>();
            ob.UseSqlServer(connectionString, x => { x.CommandTimeout(180); x.UseNetTopologySuite(); });
            return new NeptuneDbContext(ob.Options);
        }

        private static Polygon Square()
        {
            return new Polygon(new LinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(100, 0),
                new Coordinate(100, 100),
                new Coordinate(0, 100),
                new Coordinate(0, 0),
            }));
        }

        private static TrashGeneratingUnitGridDto FullyLinkedDto() => new()
        {
            TrashGeneratingUnitID = 11,
            LandUseBlockID = 22,
            LandUseType = "Commercial",
            PriorityLandUseTypeDisplayName = "Commercial",
            OnlandVisualTrashAssessmentAreaID = 33,
            OnlandVisualTrashAssessmentAreaName = "Creek Area",
            OnlandVisualTrashAssessmentAreaBaselineScore = "B",
            TreatmentBMPID = 44,
            TreatmentBMPName = "Inlet Screen 1",
            WaterQualityManagementPlanID = 55,
            WaterQualityManagementPlanName = "Mall WQMP",
            StormwaterJurisdictionID = 7,
            StormwaterJurisdictionName = "City of Test",
            Area = 1.25,
            BaselineLoadingRate = 32.5m,
            ProgressLoadingRate = 10m,
            CurrentLoadingRate = 2.5m,
            TrashCaptureStatusBMP = "Full",
            TrashCaptureStatusWQMP = "Partial",
            TrashCaptureEffectivenessBMP = 100,
            TrashCaptureEffectivenessWQMP = 60,
            MedianHouseholdIncomeResidential = 85000m,
            MedianHouseholdIncomeRetail = 62000m,
            PermitClass = "Phase I MS4",
            LandUseForTGR = "Commercial",
            TrashGenerationRate = 32.5m,
            LastUpdateDate = new DateTime(2026, 3, 9, 14, 30, 0),
        };

        [TestMethod]
        public void ToFeatureCollection_EmitsEveryMappedField_FromTheGridDto()
        {
            var geometry = Square();
            var fc = TrashGeneratingUnitGdbExport.ToFeatureCollection(new[] { (FullyLinkedDto(), (Geometry)geometry) });

            Assert.AreEqual(2, fc.Count, "Schema row + one Trash Analysis Area.");
            var feature = fc[1];
            Assert.AreSame(geometry, feature.Geometry);
            CollectionAssert.AreEquivalent(ExpectedAttributeNames, feature.Attributes.GetNames());

            var a = feature.Attributes;
            Assert.AreEqual(11, a["TrashAnalysisAreaID"]);
            Assert.AreEqual(22, a["LandUseBlockID"]);
            Assert.AreEqual("Commercial", a["LandUseType"]);
            Assert.AreEqual("Creek Area", a["OVTAAreaName"]);
            Assert.AreEqual(33, a["OVTAAreaID"]);
            Assert.AreEqual("B", a["OVTAAreaBaselineScore"]);
            Assert.AreEqual("Inlet Screen 1", a["TreatmentBMPName"]);
            Assert.AreEqual(44, a["TreatmentBMPID"]);
            Assert.AreEqual("Mall WQMP", a["WQMPName"]);
            Assert.AreEqual(55, a["WQMPID"]);
            Assert.AreEqual("City of Test", a["Jurisdiction"]);
            Assert.AreEqual(1.25, (double)a["AreaAcres"]!, 1e-9);
            Assert.AreEqual(32.5m, a["BaselineTGR"]);
            Assert.AreEqual(10m, a["ProgressTGR"]);
            Assert.AreEqual(2.5m, a["CurrentNetLoadingRate"]);
            Assert.AreEqual("Full", a["TrashCaptureStatusBMP"]);
            Assert.AreEqual("Partial", a["TrashCaptureStatusWQMP"]);
            Assert.AreEqual(100, a["TrashCaptureEffectivenessBMP"]);
            Assert.AreEqual(60, a["TrashCaptureEffectivenessWQMP"]);
            Assert.AreEqual(85000m, a["MedianHouseholdIncomeResidential"]);
            Assert.AreEqual(62000m, a["MedianHouseholdIncomeRetail"]);
            Assert.AreEqual("Phase I MS4", a["PermitClass"]);
            Assert.AreEqual("Commercial", a["LandUseForTGR"]);
            Assert.AreEqual(32.5m, a["LandUseDefaultTGR"]);
            Assert.AreEqual("2026-03-09", a["LastUpdateDate"]);
        }

        [TestMethod]
        public void ToFeatureCollection_NoRows_EmitsOnlyTheSchemaRow()
        {
            var fc = TrashGeneratingUnitGdbExport.ToFeatureCollection(Array.Empty<(TrashGeneratingUnitGridDto, Geometry)>());

            Assert.AreEqual(1, fc.Count);
            var feature = fc[0];
            Assert.IsNull(feature.Geometry);
            CollectionAssert.AreEquivalent(ExpectedAttributeNames, feature.Attributes.GetNames());
            Assert.AreEqual(TrashGeneratingUnitGdbExport.SchemaRowID, feature.Attributes["TrashAnalysisAreaID"]);
        }

        [TestMethod]
        public void SchemaRow_IsFirst_AndHasATypedValueForEveryField()
        {
            // OGR infers field types from the values, so the schema row must carry a non-null value of the
            // right JSON type in every field: whole numbers for Integer fields, fractions for Real fields.
            var fc = TrashGeneratingUnitGdbExport.ToFeatureCollection(new[] { (FullyLinkedDto(), (Geometry)Square()) });
            var schema = fc[0].Attributes;

            Assert.IsNull(fc[0].Geometry);
            Assert.IsTrue(schema.GetNames().All(name => schema[name] != null), "Every schema-row field needs a value so OGR can type it.");
            foreach (var name in new[] { "TrashAnalysisAreaID", "LandUseBlockID", "OVTAAreaID", "TreatmentBMPID", "WQMPID", "TrashCaptureEffectivenessBMP", "TrashCaptureEffectivenessWQMP" })
            {
                Assert.IsInstanceOfType(schema[name], typeof(int), $"{name} should be typed Integer.");
            }
            foreach (var name in new[] { "AreaAcres", "BaselineTGR", "ProgressTGR", "CurrentNetLoadingRate", "MedianHouseholdIncomeResidential", "MedianHouseholdIncomeRetail", "LandUseDefaultTGR" })
            {
                Assert.IsInstanceOfType(schema[name], typeof(double), $"{name} should be typed Real.");
                Assert.AreNotEqual(0, (double)schema[name] % 1, $"{name} needs a fractional value or OGR types it Integer.");
            }
            foreach (var name in new[] { "LandUseType", "OVTAAreaName", "OVTAAreaBaselineScore", "TreatmentBMPName", "WQMPName", "Jurisdiction", "TrashCaptureStatusBMP", "TrashCaptureStatusWQMP", "PermitClass", "LandUseForTGR" })
            {
                Assert.IsInstanceOfType(schema[name], typeof(string), $"{name} should be typed String.");
            }
            Assert.AreEqual("1900-01-01", schema["LastUpdateDate"]);
        }

        [TestMethod]
        public void SchemaRowFilter_DropsOnlyTheSchemaRow()
        {
            Assert.AreEqual("TrashAnalysisAreaID <> -1", TrashGeneratingUnitGdbExport.SchemaRowFilter);
            Assert.IsTrue(TrashGeneratingUnitGdbExport.SchemaRowID < 1, "The schema row ID must never collide with a real TrashGeneratingUnitID.");
        }

        [TestMethod]
        public void ToFeatureCollection_UnlinkedArea_HasNullLinkFields()
        {
            var dto = FullyLinkedDto();
            dto.OnlandVisualTrashAssessmentAreaID = null;
            dto.OnlandVisualTrashAssessmentAreaName = null;
            dto.OnlandVisualTrashAssessmentAreaBaselineScore = null;
            dto.TreatmentBMPID = null;
            dto.TreatmentBMPName = null;
            dto.WaterQualityManagementPlanID = null;
            dto.WaterQualityManagementPlanName = null;
            dto.LastUpdateDate = null;

            var a = TrashGeneratingUnitGdbExport.ToFeatureCollection(new[] { (dto, (Geometry)Square()) })[1].Attributes;

            foreach (var name in new[] { "OVTAAreaID", "OVTAAreaName", "OVTAAreaBaselineScore", "TreatmentBMPID", "TreatmentBMPName", "WQMPID", "WQMPName", "LastUpdateDate" })
            {
                Assert.IsNull(a[name], $"{name} should be null for an unlinked Trash Analysis Area.");
            }
        }

        [TestMethod]
        public void LastUpdateDate_IsThePacificDate_NotTheUtcDate()
        {
            // Stored UTC; the grid shows the browser's local (Pacific) date. 02:00 UTC on Mar 10 is Mar 9 in Orange County.
            var dto = FullyLinkedDto();
            dto.LastUpdateDate = new DateTime(2026, 3, 10, 2, 0, 0, DateTimeKind.Utc);

            var a = TrashGeneratingUnitGdbExport.ToFeatureCollection(new[] { (dto, (Geometry)Square()) })[1].Attributes;

            Assert.AreEqual("2026-03-09", a["LastUpdateDate"]);
        }

        [TestMethod]
        public async Task ExportRowsQuery_IsOneStatement_AndMatchesTheGridRows()
        {
            using var db = GetDbContext();
            var jurisdictionID = db.vTrashGeneratingUnitLoadStatistics.AsNoTracking()
                .GroupBy(x => x.StormwaterJurisdictionID).OrderBy(g => g.Count()).Select(g => (int?)g.Key).FirstOrDefault();
            if (jurisdictionID == null) { Assert.Inconclusive("No Trash Analysis Areas in the local DB."); return; }

            var query = TrashGeneratingUnitGdbExport.ExportRowsQuery(db, jurisdictionID.Value);
            var sql = query.ToQueryString();
            StringAssert.Contains(sql, "INNER JOIN [TrashGeneratingUnit]", $"Attributes and polygons must come from one statement (NPT-1134 Copilot review). SQL:\n{sql}");
            Assert.AreEqual(1, sql.Split("SELECT ").Length - 1, $"Expected a single SELECT. SQL:\n{sql}");

            var rows = await query.ToListAsync();
            var gridIDs = db.vTrashGeneratingUnitLoadStatistics.AsNoTracking()
                .Where(x => x.StormwaterJurisdictionID == jurisdictionID.Value).Select(x => x.TrashGeneratingUnitID).ToList();
            CollectionAssert.AreEquivalent(gridIDs, rows.Select(x => x.Dto.TrashGeneratingUnitID).ToList());
            Assert.IsTrue(rows.All(x => x.Geometry != null && x.Geometry.SRID == 2771), "Every row should carry its native (2771) polygon.");
        }

        [TestMethod]
        public void DownloadGdb_RequiresJurisdictionEditFeature()
        {
            var method = typeof(TrashGeneratingUnitController).GetMethod(nameof(TrashGeneratingUnitController.DownloadGdb), BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method, "Expected DownloadGdb on TrashGeneratingUnitController.");
            Assert.IsTrue(method.GetCustomAttributes(typeof(JurisdictionEditFeature), true).Any(),
                "DownloadGdb must carry [JurisdictionEditFeature] (Admin, SitkaAdmin, JM, JE), matching the grid endpoint.");
        }

        [TestMethod]
        public async Task CanDownloadJurisdiction_JurisdictionUser_OnlyTheirJurisdictions()
        {
            using var db = GetDbContext();
            var person = db.People.AsNoTracking().FirstOrDefault(p =>
                (p.RoleID == (int)RoleEnum.JurisdictionEditor || p.RoleID == (int)RoleEnum.JurisdictionManager)
                && p.StormwaterJurisdictionPeople.Any());
            if (person == null) { Assert.Inconclusive("No JE/JM with an assigned jurisdiction in the local DB."); return; }

            var assignedIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(db, person.PersonID);
            Assert.IsTrue(await TrashGeneratingUnitGdbExport.CanDownloadJurisdictionAsync(db, person.PersonID, assignedIDs.First()));

            var unassignedID = db.StormwaterJurisdictions.AsNoTracking()
                .Where(x => !assignedIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => (int?)x.StormwaterJurisdictionID).FirstOrDefault();
            if (unassignedID == null) { Assert.Inconclusive("The JE/JM is assigned to every jurisdiction."); return; }

            Assert.IsFalse(await TrashGeneratingUnitGdbExport.CanDownloadJurisdictionAsync(db, person.PersonID, unassignedID.Value),
                "A JE/JM must not be able to download a jurisdiction outside their Trash Analysis Areas grid.");
        }

        [TestMethod]
        public async Task CanDownloadJurisdiction_Admin_AnyJurisdiction()
        {
            using var db = GetDbContext();
            var admin = db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            var allIDs = db.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID).ToList();
            foreach (var id in allIDs)
            {
                Assert.IsTrue(await TrashGeneratingUnitGdbExport.CanDownloadJurisdictionAsync(db, admin.PersonID, id), $"Admin should be able to download jurisdiction {id}.");
            }
        }

        [TestMethod]
        public async Task CanDownloadJurisdiction_NonexistentJurisdiction_IsRejected()
        {
            using var db = GetDbContext();
            var admin = db.People.AsNoTracking().FirstOrDefault(p => p.RoleID == (int)RoleEnum.Admin || p.RoleID == (int)RoleEnum.SitkaAdmin);
            if (admin == null) { Assert.Inconclusive("No Admin/SitkaAdmin in the local DB."); return; }

            Assert.IsFalse(await TrashGeneratingUnitGdbExport.CanDownloadJurisdictionAsync(db, admin.PersonID, int.MaxValue));
        }
    }
}
