namespace Neptune.EFModels.Entities;

public abstract partial class DryWeatherFlowOverride
{
    /// <summary>
    /// Resolves a user-supplied value to a <see cref="DryWeatherFlowOverride"/>. Accepts either
    /// the full display name ("Yes - DWF Effectively Eliminated") or the shorthand name
    /// ("Yes" / "No"). Case-insensitive and trims whitespace — NPT-1073 KE round 2 ask, so
    /// XLSX uploaders don't have to type the long display name verbatim.
    /// </summary>
    public static DryWeatherFlowOverride? GetByDisplayNameOrShorthand(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var trimmed = input.Trim();
        return All.SingleOrDefault(x =>
            string.Equals(x.DryWeatherFlowOverrideDisplayName, trimmed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.DryWeatherFlowOverrideName, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
