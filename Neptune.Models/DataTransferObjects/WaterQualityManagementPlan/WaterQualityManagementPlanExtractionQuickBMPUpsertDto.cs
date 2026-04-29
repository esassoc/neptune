using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// Per-BMP action emitted by the WQMP AI review workflow's BMP cards in
/// Step 3 (NPT-1020). On accept/edit the BMP is upserted into the WQMP's
/// QuickBMPs list (matched by name); on reject any previously-persisted
/// row for this card index is removed. Per-card status is recorded under
/// the BMP index in DraftOverlayJson.
/// </summary>
public class WaterQualityManagementPlanExtractionQuickBMPUpsertDto
{
    /// <summary>
    /// Card index from the SPA's <c>__BMP__-{i}</c> key. Used to identify
    /// which BMP card the reviewer acted on so the status entry in
    /// DraftOverlayJson is keyed consistently across sessions.
    /// </summary>
    [Required]
    public int? BmpIndex { get; set; }

    /// <summary>
    /// The BMP record to upsert. Required on accept/edit; null on reject.
    /// </summary>
    public QuickBMPUpsertDto? QuickBMPUpsert { get; set; }

    /// <summary>
    /// One of <c>"accept"</c>, <c>"edit"</c>, <c>"reject"</c>.
    /// </summary>
    [Required]
    public string? Action { get; set; }
}
