using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.API.Services.AI;

namespace Neptune.Tests
{
    /// <summary>
    /// NPT-1131: which extraction failures count as an Anthropic account problem (generic user
    /// message + the Datadog monitor's log marker) rather than a per-document one.
    ///
    /// These cover the Files API upload path, which bypasses the SDK and so arrives as
    /// AnthropicFileUploadException with a raw status code. On QA (2026-10-07) the upload 404'd with
    /// "Not found" and the user saw exactly that text, because only the SDK's typed exceptions were
    /// classified.
    /// </summary>
    [TestClass]
    public class AnthropicAccountIssueTests
    {
        private const string NotFoundBody = "{\"type\":\"error\",\"error\":{\"type\":\"not_found_error\",\"message\":\"Not found\"}}";
        private const string CreditBody = "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":\"Your credit balance is too low to access the Anthropic API. Please go to Plans & Billing to upgrade or purchase credits.\"}}";
        private const string BadPdfBody = "{\"type\":\"error\",\"error\":{\"type\":\"invalid_request_error\",\"message\":\"Could not process PDF\"}}";

        [DataTestMethod]
        [DataRow(401)]
        [DataRow(403)]
        [DataRow(404)]
        public void UploadRejectedForKeyOrAccess_IsAccountIssue(int statusCode)
        {
            var ex = new AnthropicFileService.AnthropicFileUploadException(statusCode, NotFoundBody);
            Assert.IsTrue(AnthropicAccountIssue.IsAccountIssue(ex));
        }

        [TestMethod]
        public void UploadRejectedForCreditBalance_IsAccountIssue()
        {
            var ex = new AnthropicFileService.AnthropicFileUploadException(400, CreditBody);
            Assert.IsTrue(AnthropicAccountIssue.IsAccountIssue(ex));
        }

        [DataTestMethod]
        [DataRow(400)]
        [DataRow(413)]
        [DataRow(500)]
        public void UploadRejectedForTheDocument_IsNotAccountIssue(int statusCode)
        {
            var ex = new AnthropicFileService.AnthropicFileUploadException(statusCode, BadPdfBody);
            Assert.IsFalse(AnthropicAccountIssue.IsAccountIssue(ex));
        }

        [TestMethod]
        public void OurOwnPreCheckFailure_IsNotAccountIssue()
        {
            Assert.IsFalse(AnthropicAccountIssue.IsAccountIssue(new InvalidOperationException("Not found")));
        }

        [TestMethod]
        public void UploadExceptionMessage_KeepsBodyForReadableMessageExtraction()
        {
            // The controller pulls error.message out of everything after the first '{'.
            var ex = new AnthropicFileService.AnthropicFileUploadException(404, NotFoundBody);
            StringAssert.StartsWith(ex.Message, "Anthropic Files API upload returned 404: {");
            StringAssert.EndsWith(ex.Message, NotFoundBody);
        }
    }
}
