using System;
using System.Collections.Generic;
using System.Globalization;

namespace Neptune.EFModels.Entities.AI;

/// <summary>
/// NPT-1020: maps the WQMP AI review workflow's per-field actions onto the
/// <see cref="WaterQualityManagementPlan"/> entity. Each FieldKey emitted by the SPA
/// (matching <c>ExtractedField.key</c> in <c>wqmp-review.component.ts</c>) has a
/// type-specific parser + setter so the controller endpoint stays thin and any new
/// field can be wired up by adding one row to the dictionary below.
///
/// On <c>reject</c> we set the property to null (for nullable fields) or throw a
/// <see cref="FieldNotRejectableException"/> for required FKs the workflow shouldn't
/// be able to blank.
/// </summary>
public static class WqmpExtractionFieldApplier
{
    public sealed class UnknownFieldKeyException(string fieldKey) : Exception($"Unknown FieldKey '{fieldKey}'.");

    public sealed class FieldNotRejectableException(string fieldKey) : Exception($"Field '{fieldKey}' is required and cannot be cleared via the AI workflow.");

    public sealed class InvalidFieldValueException(string fieldKey, string raw) : Exception($"Field '{fieldKey}' could not parse value '{raw}'.");

    private delegate void Applier(WaterQualityManagementPlan wqmp, string? rawValue, bool isReject);

    private static readonly Dictionary<string, Applier> FieldAppliers = new(StringComparer.Ordinal)
    {
        // Lookup IDs — values arrive as numeric strings (the SPA already resolved labels
        // to IDs via lookupFieldConfig). Reject is a hard error since these FKs are
        // required on the entity.
        ["Jurisdiction"] = (wqmp, raw, isReject) =>
        {
            if (isReject) throw new FieldNotRejectableException("Jurisdiction");
            wqmp.StormwaterJurisdictionID = ParseRequiredInt("Jurisdiction", raw);
        },
        ["TrashCaptureStatusType"] = (wqmp, raw, isReject) =>
        {
            if (isReject) throw new FieldNotRejectableException("TrashCaptureStatusType");
            wqmp.TrashCaptureStatusTypeID = ParseRequiredInt("TrashCaptureStatusType", raw);
        },

        // Lookup IDs that are nullable — null on reject is fine.
        ["HydrologicSubarea"] = (wqmp, raw, isReject) => wqmp.HydrologicSubareaID = NullableIntFor("HydrologicSubarea", raw, isReject),
        ["WaterQualityManagementPlanPriority"] = (wqmp, raw, isReject) => wqmp.WaterQualityManagementPlanPriorityID = NullableIntFor("WaterQualityManagementPlanPriority", raw, isReject),
        ["WaterQualityManagementPlanDevelopmentType"] = (wqmp, raw, isReject) => wqmp.WaterQualityManagementPlanDevelopmentTypeID = NullableIntFor("WaterQualityManagementPlanDevelopmentType", raw, isReject),
        ["WaterQualityManagementPlanLandUse"] = (wqmp, raw, isReject) => wqmp.WaterQualityManagementPlanLandUseID = NullableIntFor("WaterQualityManagementPlanLandUse", raw, isReject),
        ["WaterQualityManagementPlanPermitTerm"] = (wqmp, raw, isReject) => wqmp.WaterQualityManagementPlanPermitTermID = NullableIntFor("WaterQualityManagementPlanPermitTerm", raw, isReject),
        ["HydromodificationAppliesType"] = (wqmp, raw, isReject) => wqmp.HydromodificationAppliesTypeID = NullableIntFor("HydromodificationAppliesType", raw, isReject),

        // Text fields.
        ["WaterQualityManagementPlanName"] = (wqmp, raw, isReject) => wqmp.WaterQualityManagementPlanName = isReject ? null : Trimmed(raw),
        ["RecordNumber"] = (wqmp, raw, isReject) => wqmp.RecordNumber = isReject ? null : Trimmed(raw),
        ["MaintenanceContactName"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactName = isReject ? null : Trimmed(raw),
        ["MaintenanceContactOrganization"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactOrganization = isReject ? null : Trimmed(raw),
        ["MaintenanceContactPhone"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactPhone = isReject ? null : Trimmed(raw),
        ["MaintenanceContactAddress1"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactAddress1 = isReject ? null : Trimmed(raw),
        ["MaintenanceContactAddress2"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactAddress2 = isReject ? null : Trimmed(raw),
        ["MaintenanceContactCity"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactCity = isReject ? null : Trimmed(raw),
        ["MaintenanceContactState"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactState = isReject ? null : Trimmed(raw),
        ["MaintenanceContactZip"] = (wqmp, raw, isReject) => wqmp.MaintenanceContactZip = isReject ? null : Trimmed(raw),

        // Number.
        ["RecordedWQMPAreaInAcres"] = (wqmp, raw, isReject) => wqmp.RecordedWQMPAreaInAcres = NullableDecimalFor("RecordedWQMPAreaInAcres", raw, isReject),

        // Dates — accept ISO yyyy-MM-dd which is what the SPA's date input emits.
        ["ApprovalDate"] = (wqmp, raw, isReject) => wqmp.ApprovalDate = NullableDateFor("ApprovalDate", raw, isReject),
        ["DateOfConstruction"] = (wqmp, raw, isReject) => wqmp.DateOfConstruction = NullableDateFor("DateOfConstruction", raw, isReject),
    };

    /// <summary>
    /// Applies a single per-field action to <paramref name="wqmp"/> in place. Caller
    /// owns <c>SaveChangesAsync</c>.
    /// </summary>
    public static void Apply(WaterQualityManagementPlan wqmp, string fieldKey, string? rawValue, string action)
    {
        if (!FieldAppliers.TryGetValue(fieldKey, out var applier))
        {
            throw new UnknownFieldKeyException(fieldKey);
        }

        var isReject = string.Equals(action, "reject", StringComparison.OrdinalIgnoreCase);
        applier(wqmp, rawValue, isReject);
    }

    /// <summary>True when this FieldKey is mapped — cheap precheck the controller can use.</summary>
    public static bool IsKnownFieldKey(string fieldKey) => FieldAppliers.ContainsKey(fieldKey);

    private static string? Trimmed(string? raw) => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();

    private static int ParseRequiredInt(string fieldKey, string? raw)
    {
        var trimmed = Trimmed(raw);
        if (trimmed == null || !int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidFieldValueException(fieldKey, raw ?? "");
        }
        return value;
    }

    private static int? NullableIntFor(string fieldKey, string? raw, bool isReject)
    {
        if (isReject) return null;
        var trimmed = Trimmed(raw);
        if (trimmed == null) return null;
        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidFieldValueException(fieldKey, raw ?? "");
        }
        return value;
    }

    private static decimal? NullableDecimalFor(string fieldKey, string? raw, bool isReject)
    {
        if (isReject) return null;
        var trimmed = Trimmed(raw);
        if (trimmed == null) return null;
        if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidFieldValueException(fieldKey, raw ?? "");
        }
        return value;
    }

    private static DateTime? NullableDateFor(string fieldKey, string? raw, bool isReject)
    {
        if (isReject) return null;
        var trimmed = Trimmed(raw);
        if (trimmed == null) return null;
        // The SPA's <input type="date"> emits yyyy-MM-dd; AI-extracted dates may be
        // free-form. Try the strict ISO parse first, fall back to invariant parse.
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var iso))
        {
            return DateTime.SpecifyKind(iso, DateTimeKind.Utc);
        }
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var loose))
        {
            return loose;
        }
        throw new InvalidFieldValueException(fieldKey, raw ?? "");
    }
}
