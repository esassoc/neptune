using Neptune.Jobs;

namespace Neptune.API.Services;

public class NeptuneConfiguration : NeptuneJobConfiguration
{
    public string DatabaseConnectionString { get; set; }
    public string AzureBlobStorageConnectionString { get; set; }
    public string OcStormwaterToolsBaseUrl { get; set; }
    public string NereidUrl { get; set; }

    public string GDALAPIBaseUrl { get; set; }
    public string QGISAPIBaseUrl { get; set; }
    public string OCGISBaseUrl { get; set; }

    public string AnthropicApiKey { get; set; }
    public string ClaudeModelId { get; set; } = "claude-sonnet-4-6";
    public string Auth0Domain { get; set; }
    public string Auth0ClientID { get; set; }
}