using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Services.AI;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1106 round 3: strict tool use caps union-typed schema parameters at 16 per request,
    /// so the extraction schemas express "not found" as sentinels (empty strings, all-zero
    /// BoundingBox) instead of nulls. NormalizeNotFoundSentinels must restore the null-based
    /// contract the SPA review wizard and approve endpoint consume — pin that translation.
    /// </summary>
    [TestClass]
    public class WqmpExtractionSentinelTests
    {
        [TestMethod]
        public void NotFoundSentinels_BecomeNulls()
        {
            const string input = """
                {"WaterQualityManagementPlanName":{"Value":"","ExtractionEvidence":"","DocumentSource":"","BoundingBox":{"PageNumber":0,"X":0,"Y":0,"Width":0,"Height":0}}}
                """;

            var normalized = WqmpExtractionService.NormalizeNotFoundSentinels(input);
            using var doc = JsonDocument.Parse(normalized);
            var field = doc.RootElement.GetProperty("WaterQualityManagementPlanName");

            Assert.AreEqual(JsonValueKind.Null, field.GetProperty("Value").ValueKind);
            Assert.AreEqual(JsonValueKind.Null, field.GetProperty("ExtractionEvidence").ValueKind);
            Assert.AreEqual(JsonValueKind.Null, field.GetProperty("DocumentSource").ValueKind);
            Assert.AreEqual(JsonValueKind.Null, field.GetProperty("BoundingBox").ValueKind);
        }

        [TestMethod]
        public void FoundValues_PassThroughUnchanged()
        {
            const string input = """
                {"Jurisdiction":{"Value":"City of Brea","ExtractionEvidence":"the City of Brea shall...","DocumentSource":"Page 3","BoundingBox":{"PageNumber":3,"X":0.1,"Y":0.2,"Width":0.5,"Height":0.03}}}
                """;

            var normalized = WqmpExtractionService.NormalizeNotFoundSentinels(input);
            using var doc = JsonDocument.Parse(normalized);
            var field = doc.RootElement.GetProperty("Jurisdiction");

            Assert.AreEqual("City of Brea", field.GetProperty("Value").GetString());
            Assert.AreEqual("Page 3", field.GetProperty("DocumentSource").GetString());
            Assert.AreEqual(3, field.GetProperty("BoundingBox").GetProperty("PageNumber").GetInt32());
        }

        [TestMethod]
        public void MixedFields_NormalizeIndependently_IncludingInsideArrays()
        {
            // Array-category shape: { "items": [ { <ExtractedValue fields> }, ... ] } — the
            // SourceControlBMPs "No with no note" case must keep IsPresent and null the note.
            const string input = """
                {"items":[{"SourceControlBMPAttribute":{"Value":"Street Trees","ExtractionEvidence":"checklist row","DocumentSource":"Page 9","BoundingBox":{"PageNumber":9,"X":0.1,"Y":0.5,"Width":0.3,"Height":0.02}},"IsPresent":{"Value":"No","ExtractionEvidence":"[x] No","DocumentSource":"Page 9","BoundingBox":{"PageNumber":9,"X":0.6,"Y":0.5,"Width":0.1,"Height":0.02}},"SourceControlBMPNote":{"Value":"","ExtractionEvidence":"","DocumentSource":"","BoundingBox":{"PageNumber":0,"X":0,"Y":0,"Width":0,"Height":0}}}]}
                """;

            var normalized = WqmpExtractionService.NormalizeNotFoundSentinels(input);
            using var doc = JsonDocument.Parse(normalized);
            var item = doc.RootElement.GetProperty("items")[0];

            Assert.AreEqual("No", item.GetProperty("IsPresent").GetProperty("Value").GetString(),
                "an explicit 'No' answer must survive normalization untouched");
            Assert.AreEqual(JsonValueKind.Null, item.GetProperty("SourceControlBMPNote").GetProperty("Value").ValueKind);
            Assert.AreEqual(JsonValueKind.Null, item.GetProperty("SourceControlBMPNote").GetProperty("BoundingBox").ValueKind);
            Assert.AreEqual(9, item.GetProperty("IsPresent").GetProperty("BoundingBox").GetProperty("PageNumber").GetInt32());
        }

        [TestMethod]
        public void NonExtractedValueObjects_AreLeftAlone()
        {
            const string input = """{"SchemaVersion":"3","Notes":"","Count":0}""";
            var normalized = WqmpExtractionService.NormalizeNotFoundSentinels(input);
            using var doc = JsonDocument.Parse(normalized);

            Assert.AreEqual("", doc.RootElement.GetProperty("Notes").GetString(),
                "empty strings outside the ExtractedValue shape must not be nulled");
        }

        // The Anthropic strict-schema compiler enforces two hard caps this suite pins the
        // schemas against: max 16 union-typed parameters per request (type arrays / anyOf),
        // and a total compiled-grammar size that inlined ExtractedValue objects exceeded —
        // the array-category schemas must use a single $defs/ExtractedValue definition with
        // $ref fields (verified compilable against the live API; inlined equivalents 400).
        private static string BuildSchema(string builderName)
        {
            var method = typeof(WqmpExtractionService).GetMethod(builderName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(method, $"Expected private static {builderName}() on WqmpExtractionService.");
            return (string)method!.Invoke(null, null)!;
        }

        [DataTestMethod]
        [DataRow("BuildParcelSchemaJson")]
        [DataRow("BuildQuickBmpSchemaJson")]
        [DataRow("BuildSourceControlBmpSchemaJson")]
        public void ArrayCategorySchemas_UseSharedExtractedValueDefinition(string builderName)
        {
            var json = BuildSchema(builderName);
            using var doc = JsonDocument.Parse(json);

            Assert.IsTrue(doc.RootElement.GetProperty("$defs").TryGetProperty("ExtractedValue", out _),
                $"{builderName}: array-category schemas must define ExtractedValue once in $defs — inlining it per field exceeds the strict grammar-size cap.");
            Assert.IsTrue(json.Contains("\"$ref\":\"#/$defs/ExtractedValue\""),
                $"{builderName}: item fields must $ref the shared definition.");
        }

        [DataTestMethod]
        [DataRow("BuildWqmpSchemaJson")]
        [DataRow("BuildParcelSchemaJson")]
        [DataRow("BuildQuickBmpSchemaJson")]
        [DataRow("BuildSourceControlBmpSchemaJson")]
        public void CategorySchemas_ContainNoUnionTypes(string builderName)
        {
            var json = BuildSchema(builderName);

            Assert.IsFalse(json.Contains("\"type\":["),
                $"{builderName}: union types (type arrays) are capped at 16 per strict request — 'not found' must use sentinels, not nullables.");
            Assert.IsFalse(json.Contains("anyOf"),
                $"{builderName}: anyOf counts against the same strict union cap.");
        }
    }
}
