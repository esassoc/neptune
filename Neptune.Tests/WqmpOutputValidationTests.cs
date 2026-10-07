using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Services.AI;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1132: the WQMP extraction tool can't be strict (all four strict tools exceed the API's
    /// schema-complexity limit), so <see cref="WqmpExtractionService.ValidateWqmpOutput"/> checks
    /// its output instead. Anything malformed is emptied and reported, never passed to the wizard.
    /// </summary>
    [TestClass]
    public class WqmpOutputValidationTests
    {
        private static readonly string[] FieldNames =
        [
            "WaterQualityManagementPlanName", "Jurisdiction", "WaterQualityManagementPlanLandUse", "WaterQualityManagementPlanPriority",
            "WaterQualityManagementPlanStatus", "WaterQualityManagementPlanDevelopmentType", "ApprovalDate", "MaintenanceContactName",
            "MaintenanceContactOrganization", "MaintenanceContactPhone", "MaintenanceContactAddress1", "MaintenanceContactAddress2",
            "MaintenanceContactCity", "MaintenanceContactState", "MaintenanceContactZip", "WaterQualityManagementPlanPermitTerm",
            "DateOfConstruction", "HydrologicSubarea", "RecordNumber", "RecordedWQMPAreaInAcres", "TrashCaptureStatusType",
            "HydromodificationAppliesType",
        ];

        private static JsonObject ValidOutput()
        {
            var root = new JsonObject();
            foreach (var name in FieldNames)
            {
                root[name] = new JsonObject
                {
                    ["Value"] = "x", ["ExtractionEvidence"] = "evidence", ["DocumentSource"] = "Page 1",
                    ["BoundingBox"] = new JsonObject { ["PageNumber"] = 1, ["X"] = 0.1, ["Y"] = 0.2, ["Width"] = 0.3, ["Height"] = 0.04 },
                };
            }
            return root;
        }

        private static (JsonObject Output, List<string> Problems) Validate(JsonObject input)
        {
            var problems = new List<string>();
            var output = (JsonObject)JsonNode.Parse(WqmpExtractionService.ValidateWqmpOutput(input.ToJsonString(), problems))!;
            return (output, problems);
        }

        [TestMethod]
        public void ValidOutput_PassesUnchangedWithNoProblems()
        {
            var (output, problems) = Validate(ValidOutput());
            Assert.AreEqual(0, problems.Count);
            Assert.AreEqual("x", output["ApprovalDate"]!["Value"]!.GetValue<string>());
        }

        [TestMethod]
        public void NotFoundNulls_AreValid()
        {
            var input = ValidOutput();
            input["ApprovalDate"] = new JsonObject { ["Value"] = null, ["ExtractionEvidence"] = null, ["DocumentSource"] = null, ["BoundingBox"] = null };
            Assert.AreEqual(0, Validate(input).Problems.Count);
        }

        [TestMethod]
        public void FieldThatIsNotAnObject_IsEmptiedAndReported()
        {
            var input = ValidOutput();
            input["ApprovalDate"] = "June 5, 2019";
            var (output, problems) = Validate(input);
            Assert.IsNull(output["ApprovalDate"]);
            CollectionAssert.Contains(problems, "WQMP: ApprovalDate malformed, left empty");
        }

        [TestMethod]
        public void NonStringValue_IsEmptiedAndReported()
        {
            var input = ValidOutput();
            input["RecordedWQMPAreaInAcres"]!["Value"] = 1.23;
            var (output, problems) = Validate(input);
            Assert.IsNull(output["RecordedWQMPAreaInAcres"]);
            Assert.AreEqual(1, problems.Count);
        }

        [TestMethod]
        public void MalformedBoundingBox_DropsOnlyTheBox()
        {
            var input = ValidOutput();
            input["MaintenanceContactCity"]!["BoundingBox"] = new JsonObject { ["PageNumber"] = 1 };
            var (output, problems) = Validate(input);
            Assert.IsNull(output["MaintenanceContactCity"]!["BoundingBox"]);
            Assert.AreEqual("x", output["MaintenanceContactCity"]!["Value"]!.GetValue<string>());
            CollectionAssert.Contains(problems, "WQMP: MaintenanceContactCity bounding box malformed, dropped");
        }

        [TestMethod]
        public void MissingAndUnexpectedFields_AreReported()
        {
            var input = ValidOutput();
            input.Remove("RecordNumber");
            input["Surprise"] = "x";
            var (output, problems) = Validate(input);
            Assert.IsFalse(output.ContainsKey("Surprise"));
            CollectionAssert.Contains(problems, "WQMP: RecordNumber missing");
            CollectionAssert.Contains(problems, "WQMP: unexpected property Surprise dropped");
        }

        [TestMethod]
        public void EmptyFallback_IsNotReportedAgain()
        {
            var problems = new List<string>();
            Assert.AreEqual("{}", WqmpExtractionService.ValidateWqmpOutput("{}", problems));
            Assert.AreEqual(0, problems.Count);
        }

        [TestMethod]
        public void NotAnObject_IsReported()
        {
            var problems = new List<string>();
            Assert.AreEqual("{}", WqmpExtractionService.ValidateWqmpOutput("[1,2]", problems));
            Assert.AreEqual(1, problems.Count);
        }

        [TestMethod]
        public void FieldList_MatchesTheSchema()
        {
            // Every schema field must be present in a valid output; this catches a schema change
            // that the test's field list (and the validator's expectations) didn't follow.
            var input = ValidOutput();
            Assert.AreEqual(0, Validate(input).Problems.Count(p => p.EndsWith("missing")));
        }
    }
}
