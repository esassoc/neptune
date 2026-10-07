using Neptune.Models.DataTransferObjects;

namespace Neptune.Eval;

/// <summary>
/// Anthropic API list prices, USD per million tokens (as of 2026-09). Cache writes use the
/// 5-minute ephemeral rate (1.25x input); cache reads use each model's published read price.
/// Update when prices change; unknown models report $0 and are flagged in the scorecard.
/// </summary>
public static class Pricing
{
    private sealed record Rate(decimal Input, decimal Output, decimal CacheRead);

    private static readonly Dictionary<string, Rate> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-sonnet-4-6"] = new(3.00m, 15.00m, 0.30m),
        ["claude-sonnet-5"] = new(2.00m, 10.00m, 0.20m),
        ["claude-sonnet-5-5"] = new(2.00m, 10.00m, 0.20m),
        ["claude-opus-5"] = new(5.00m, 25.00m, 0.50m),
        ["claude-opus-5-5"] = new(4.00m, 20.00m, 0.20m),
        ["claude-haiku-4-5"] = new(1.00m, 5.00m, 0.10m),
    };

    public static bool IsKnown(string model) => Rates.ContainsKey(model);

    public static decimal Cost(string model, IEnumerable<WqmpExtractionCallUsageDto> calls)
    {
        if (!Rates.TryGetValue(model, out var r)) return 0m;
        decimal total = 0m;
        foreach (var c in calls)
        {
            total += c.InputTokens * r.Input
                     + c.CacheCreationInputTokens * r.Input * 1.25m
                     + c.CacheReadInputTokens * r.CacheRead
                     + c.OutputTokens * r.Output;
        }
        return total / 1_000_000m;
    }
}
