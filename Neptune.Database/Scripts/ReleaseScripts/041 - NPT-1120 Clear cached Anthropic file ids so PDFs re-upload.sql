-- NPT-1120: clear the cached Anthropic Files API ids on WQMP documents so every document
-- re-uploads through the new PDF-header normalization in AnthropicFileService.
--
-- Some source PDFs carry junk ahead of the %PDF signature (the reported case: a single stray
-- 0x01 byte). Anthropic requires the signature at byte 0 and otherwise sniffs the document as
-- application/octet-stream, failing extraction with "Unsupported document file format" — an
-- error that names the wrong problem. The fix trims those leading bytes at upload time, but it
-- only runs on a cache miss: AnthropicFileID short-circuits the upload entirely, so any document
-- already uploaded with the bad header keeps failing until its cached id is cleared.
--
-- Clearing the id is safe and cheap: the next extraction or chat call re-uploads the PDF and
-- re-caches a fresh id. The only cost is one repeat upload per document. Idempotent — rows with
-- a NULL id are untouched, so re-running is a no-op.
--
-- Note: this orphans the previously-uploaded files on the Anthropic account. They are unreferenced
-- after this runs and count toward the 100GB org storage limit; delete them via the Files API if
-- that ever matters.
SET NOCOUNT ON;

UPDATE dbo.WaterQualityManagementPlanDocument
SET AnthropicFileID = NULL,
    AnthropicFileUploadedDate = NULL
WHERE AnthropicFileID IS NOT NULL;
