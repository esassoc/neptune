namespace Neptune.EFModels.Entities;

/// <summary>
/// NPT-1075 round 2 — per-row validation for the OVTA Area GDB upload. Replaces the
/// silent <c>.Where(x =&gt; x.Geometry is { IsValid: true, Area: &gt; 0 })</c> filter and the
/// generic "file may be corrupted or invalid" catch-all with actionable per-feature
/// error messages so users can find and fix the offending features in their GIS tooling.
/// </summary>
public static class OnlandVisualTrashAssessmentAreaGdbValidator
{
    /// <summary>
    /// Hard limit on the Description column, matching <c>varchar(500)</c> on
    /// <c>dbo.OnlandVisualTrashAssessmentArea.AssessmentAreaDescription</c>.
    /// </summary>
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Walks the deserialized staging rows and returns one error per problem found.
    /// Empty list means every row is valid and the caller can proceed to insert. The
    /// caller is responsible for the whole-file-rejection semantics — pure function,
    /// no DbContext, no side effects.
    /// </summary>
    public static List<string> Validate(IReadOnlyList<OnlandVisualTrashAssessmentAreaStaging> stagings)
    {
        var errors = new List<string>();
        for (var i = 0; i < stagings.Count; i++)
        {
            var rowNumber = i + 1;
            var staging = stagings[i];

            if (string.IsNullOrWhiteSpace(staging.AreaName))
            {
                errors.Add($"Feature {rowNumber}: OVTA Area Name is missing.");
                continue;
            }
            if (staging.Geometry == null)
            {
                errors.Add($"Feature {rowNumber} ({staging.AreaName}): geometry is missing.");
                continue;
            }
            if (!staging.Geometry.IsValid)
            {
                errors.Add($"Feature {rowNumber} ({staging.AreaName}): geometry is invalid (NTS reports IsValid = false).");
                continue;
            }
            if (staging.Geometry.Area <= 0)
            {
                errors.Add($"Feature {rowNumber} ({staging.AreaName}): polygon has zero or negative area.");
                continue;
            }
            if (!string.IsNullOrEmpty(staging.Description) && staging.Description.Length > MaxDescriptionLength)
            {
                errors.Add($"Feature {rowNumber} ({staging.AreaName}): Description exceeds the {MaxDescriptionLength}-character limit ({staging.Description.Length} characters provided).");
            }
        }
        return errors;
    }
}
