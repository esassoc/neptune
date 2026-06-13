using Neptune.Common.Email;

namespace Neptune.Jobs
{
    public class NeptuneJobConfiguration : SendGridConfiguration
    {
        // Root of the single SPA app (https://{web}). Used by both the API and the in-process Hangfire
        // jobs to build user-facing links (emails, etc.). Append the Angular route path.
        public string OcStormwaterToolsBaseUrl { get; set; }

        // Legacy module-specific base URLs (carry a /planning or /trash suffix in the WebMvc configmap).
        // Only the retiring WebMvc navbar/DataHub still reads these; the API uses OcStormwaterToolsBaseUrl.
        public string PlanningModuleBaseUrl { get; set; }
        public string TrashModuleBaseUrl { get; set; }
        public string AzureBlobStorageConnectionString { get; set; }
    }
}
