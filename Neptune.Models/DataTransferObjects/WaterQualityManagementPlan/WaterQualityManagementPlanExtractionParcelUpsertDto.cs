using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// Per-parcel action emitted by the WQMP AI review workflow's parcel cards
/// in Step 1 (NPT-1020). Each accept / edit / reject click adds or removes
/// the parcel from the WQMP's parcel list and records the per-card status
/// in the draft overlay JSON.
/// </summary>
public class WaterQualityManagementPlanExtractionParcelUpsertDto
{
    /// <summary>
    /// Stable per-card key in the SPA's <c>__Parcel__-{i}</c> shape. Used to
    /// index the entry in DraftOverlayJson so the same card can be re-edited.
    /// </summary>
    [Required]
    public string? ParcelKey { get; set; }

    /// <summary>
    /// APN as the reviewer accepted/edited it. Null when
    /// <see cref="Action"/> is <c>"reject"</c>. Server resolves the APN to
    /// a ParcelID; an unresolvable APN returns 400.
    /// </summary>
    public string? ParcelNumber { get; set; }

    /// <summary>
    /// One of <c>"accept"</c>, <c>"edit"</c>, <c>"reject"</c>.
    /// </summary>
    [Required]
    public string? Action { get; set; }
}
