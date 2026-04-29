using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// Per-field action emitted by the WQMP AI review workflow's field cards
/// (NPT-1020). Every accept / edit / reject click writes through to both the
/// draft overlay JSON and the live WQMP in a single transaction so the
/// detail page reflects the change immediately.
/// </summary>
public class WaterQualityManagementPlanExtractionFieldUpsertDto
{
    /// <summary>
    /// Stable identifier matching the SPA's ExtractedField.key (e.g.
    /// <c>"Jurisdiction"</c>, <c>"WaterQualityManagementPlanName"</c>,
    /// <c>"ApprovalDate"</c>). Backend resolves the key to a WQMP property
    /// and value parser via <c>WqmpExtractionFieldApplier</c>.
    /// </summary>
    [Required]
    public string? FieldKey { get; set; }

    /// <summary>
    /// Raw string the SPA captured for this field. For lookup fields this is
    /// the resolved ID as a string (the SPA already mapped extracted text to
    /// an ID via its lookupFieldConfig). Null when <see cref="Action"/> is
    /// <c>"reject"</c>.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// One of <c>"accept"</c>, <c>"edit"</c>, <c>"reject"</c>. Drives the
    /// state recorded in DraftOverlayJson and (for reject) whether the WQMP
    /// property is cleared.
    /// </summary>
    [Required]
    public string? Action { get; set; }
}
