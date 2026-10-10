namespace SupportDesk.Application.Options;

/// <summary>
/// Bound to the "Sla" section of configuration. Numbers only - the domain works in terms
/// of <c>SlaWindowRules</c>, and a mapping method converts between the two.
/// </summary>
public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    /// <summary>Base response window in hours, per priority.</summary>
    public Dictionary<string, int> BaseWindowHours { get; set; } = new()
    {
        ["Low"] = 72,
        ["Medium"] = 24,
        ["High"] = 8,
        ["Critical"] = 4
    };

    /// <summary>Multiplier applied to Premium customers (0.5 halves the window).</summary>
    public double PremiumCustomerMultiplier { get; set; } = 0.5;

    /// <summary>Never let the window drop below this many hours.</summary>
    public int MinimumWindowHours { get; set; } = 1;

    /// <summary>When the remaining fraction of the window is at or below this, the ticket is At risk.</summary>
    public int AtRiskThresholdPercent { get; set; } = 25;
}