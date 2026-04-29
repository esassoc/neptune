using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanExtractionResults
{
    public static async Task<WaterQualityManagementPlanExtractionResult?> GetByWqmpIDAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.WaterQualityManagementPlanExtractionResults
            .SingleOrDefaultAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
    }

    public static async Task<WaterQualityManagementPlanExtractionResultDto?> GetByWqmpIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.WaterQualityManagementPlanExtractionResults
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(WaterQualityManagementPlanExtractionResultProjections.AsDto)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// NPT-1020: per-field status writer. Reads <c>DraftOverlayJson</c>, mutates the entry
    /// for <paramref name="fieldKey"/>, writes back. Caller composes this with the live
    /// WQMP write inside the same DbContext transaction so the draft state and the WQMP
    /// stay in lockstep.
    /// </summary>
    /// <param name="state">One of <c>"accepted"</c>, <c>"edited"</c>, <c>"rejected"</c>.</param>
    /// <param name="value">Reviewer-current value as a string. Null on reject.</param>
    public static async Task SetFieldStatusAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, string fieldKey, string state, string? value, int personID)
    {
        var entity = await GetByWqmpIDAsync(dbContext, waterQualityManagementPlanID)
            ?? throw new InvalidOperationException($"No extraction result exists for WQMP {waterQualityManagementPlanID}.");

        if (entity.ApprovedDate.HasValue)
        {
            throw new InvalidOperationException("Cannot modify a field on an extraction result that has already been approved.");
        }

        // Parse the existing overlay (or start fresh) and upsert the field's entry.
        // Uses System.Text.Json.Nodes for tolerant in-place mutation — overlay shape:
        //   { "Jurisdiction": { "state": "accepted", "value": "37" }, ... }
        var overlay = string.IsNullOrWhiteSpace(entity.DraftOverlayJson)
            ? new JsonObject()
            : JsonNode.Parse(entity.DraftOverlayJson) as JsonObject ?? new JsonObject();

        var entry = new JsonObject { ["state"] = state };
        if (state != "rejected")
        {
            entry["value"] = value;
        }
        overlay[fieldKey] = entry;

        entity.ApplyDraftOverlay(overlay.ToJsonString(), personID, DateTime.UtcNow);
    }

    // Used when a reviewer re-runs the AI extraction — the draft overlay lives on the existing
    // result row, so deleting the row clears the draft too. Caller confirms data loss in the UI.
    public static async Task DeleteByWqmpIDAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        await dbContext.WaterQualityManagementPlanExtractionResults
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .ExecuteDeleteAsync();
    }

    public static async Task MarkApprovedAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, int personID)
    {
        var entity = await GetByWqmpIDAsync(dbContext, waterQualityManagementPlanID)
            ?? throw new InvalidOperationException($"No extraction result exists for WQMP {waterQualityManagementPlanID}.");

        entity.Approve(personID, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
    }
}
