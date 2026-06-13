using Neptune.Common.Recaptcha;
using Neptune.Jobs;

namespace Neptune.API.Services;

public class NeptuneConfiguration : NeptuneJobConfiguration
{
    public string DatabaseConnectionString { get; set; }
    public string AzureBlobStorageConnectionString { get; set; }
    public string NereidUrl { get; set; }
    public GoogleRecaptchaV3Config GoogleRecaptchaV3Config { get; set; }

    public string GDALAPIBaseUrl { get; set; }
    public string QGISAPIBaseUrl { get; set; }
    public string OCGISBaseUrl { get; set; }

    public string AnthropicApiKey { get; set; }
    public string ClaudeModelId { get; set; } = "claude-sonnet-4-6";

    // NPT-998: Azure Blob paths for the bulk-upload .xlsx/.csv templates the user downloads
    // from the Data Hub upload pages. Same values as Neptune.WebMvc.Common.WebConfiguration so
    // both surfaces can serve the same template files during the MVC retirement transition.
    public string PathToBulkUploadWQMPTemplate { get; set; }
    public string PathToSimplifiedBMPTemplate { get; set; }
    public string PathToUploadWQMPBoundaryTemplate { get; set; }
    public string PathToFieldVisitUploadTemplate { get; set; }
    public string PathToOVTAUploadTemplate { get; set; }

    /// <summary>
    /// Upper bound on PDF size accepted by the WQMP upload + AI extraction pipeline.
    /// Defaults to 200 MB (Anthropic's Files API ceiling is 500 MB; 200 MB covers
    /// 99%+ of real-world scanned WQMPs with headroom). Override per environment if
    /// needed.
    /// </summary>
    public long MaxExtractablePdfSizeBytes { get; set; } = 200L * 1024 * 1024;
    public string Auth0Domain { get; set; }
    public string Auth0ClientID { get; set; }
}