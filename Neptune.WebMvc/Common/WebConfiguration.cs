using Neptune.Common.Recaptcha;
using Neptune.Jobs;

namespace Neptune.WebMvc.Common;

public class WebConfiguration : NeptuneJobConfiguration
{
    public string DatabaseConnectionString { get; set; }
    public string MapServiceUrl { get; set; }

    public string NereidUrl { get; set; }
    public string GDALAPIBaseUrl { get; set; }
    public string QGISAPIBaseUrl { get; set; }
    public string OCGISBaseUrl { get; set; }

    public string PathToFieldVisitUploadTemplate { get; set; }
    public string PathToOVTAUploadTemplate { get; set; }
    public string PathToSimplifiedBMPTemplate { get; set; }
    public string PathToBulkUploadWQMPTemplate { get; set; }
    public string PathToUploadWQMPBoundaryTemplate { get; set; }
    public string AzureBlobStorageConnectionString { get; set; }

    public GoogleRecaptchaV3Config GoogleRecaptchaV3Config { get; set; }
    public string Auth0Domain { get; set; }
    public string Auth0ClientID { get; set; }
}