namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// NPT-1068: raw Nereid request / response JSON for the BMP's most recent run. Surfaced by
/// SitkaAdmin-only "Latest Nereid Request" / "Latest Nereid Response" download links on the
/// Treatment BMP detail page, matching the legacy MVC <c>ModeledPerformance.cshtml</c> layout.
/// </summary>
public class TreatmentBMPNereidLogContentDto
{
    public int NereidLogID { get; set; }
    public string NereidRequest { get; set; } = string.Empty;
    public string? NereidResponse { get; set; }
}
