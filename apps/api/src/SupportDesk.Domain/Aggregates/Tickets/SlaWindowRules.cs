namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// The tunable numbers that define an SLA response window: base hours per priority,
/// the multiplier applied to Premium customers, the floor, and the At-Risk threshold.
/// These come from configuration — no rule may hard-code them.
/// </summary>
public sealed record SlaWindowRules(
    IReadOnlyDictionary<TicketPriority, int> BaseWindowHours,
    double PremiumCustomerMultiplier,
    int MinimumWindowHours,
    int AtRiskThresholdPercent)
{
    /// <summary>The rules the app currently ships with — used only by tests and defaults.</summary>
    public static SlaWindowRules Default { get; } = new(
        BaseWindowHours: new Dictionary<TicketPriority, int>
        {
            [TicketPriority.Low] = 72,
            [TicketPriority.Medium] = 24,
            [TicketPriority.High] = 8,
            [TicketPriority.Critical] = 4
        },
        PremiumCustomerMultiplier: 0.5,
        MinimumWindowHours: 1,
        AtRiskThresholdPercent: 25);

    public int BaseHoursFor(TicketPriority priority) =>
        BaseWindowHours.TryGetValue(priority, out var hours)
            ? hours
            : throw new InvalidOperationException(
                $"No base SLA window configured for priority '{priority}'.");
}