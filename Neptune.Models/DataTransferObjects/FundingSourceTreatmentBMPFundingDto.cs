namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// NPT-999: one row in the SPA Funding Source detail page's Treatment BMP Funding panel.
/// Mirrors the legacy MVC FundingSource/Detail "Treatment BMP Funding" table — links a
/// TreatmentBMP to the amount this FundingSource has contributed via FundingEvents.
/// Multiple FundingEvents against the same BMP are summed server-side.
/// </summary>
public class FundingSourceTreatmentBMPFundingDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Drives whether the BMP name should render as a link on the public/anonymous
    /// view — verified BMPs are visible to everyone, unverified are admin-only.</summary>
    public bool InventoryIsVerified { get; set; }
}
