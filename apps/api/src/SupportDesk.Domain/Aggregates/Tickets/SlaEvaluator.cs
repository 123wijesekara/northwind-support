namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out where a ticket stands against its due date. SLA status is always derived from
/// the ticket's dates and the current time - never stored.
/// </summary>
public static class SlaEvaluator
{
    /// <summary>
    /// Old 3-argument shape kept for callers that don't have the At-Risk threshold.
    /// Reports AtRisk never; use the 5-argument overload if you need that distinction.
    /// </summary>
    public static SlaStatus Evaluate(DateTime? dueAtUtc, DateTime? resolvedAtUtc, DateTime nowUtc) =>
        Evaluate(dueAtUtc, resolvedAtUtc, createdAtUtc: nowUtc, nowUtc, atRiskThresholdPercent: 0);

    /// <summary>
    /// BR-8. Derived: nothing is stored. Uses <paramref name="createdAtUtc"/> to know the
    /// window size, and <paramref name="atRiskThresholdPercent"/> to decide when an open
    /// ticket is close enough to its due date to be "at risk".
    /// </summary>
    /// <remarks>
    /// 0 for <paramref name="atRiskThresholdPercent"/> disables the At-Risk state entirely
    /// (every open, unbreached ticket is WithinSla), which is what older callers expect.
    /// </remarks>
    public static SlaStatus Evaluate(
        DateTime? dueAtUtc,
        DateTime? resolvedAtUtc,
        DateTime createdAtUtc,
        DateTime nowUtc,
        int atRiskThresholdPercent)
    {
        if (dueAtUtc is null)
        {
            return SlaStatus.NotApplicable;
        }

        var due = dueAtUtc.Value;

        if (resolvedAtUtc is not null)
        {
            return resolvedAtUtc.Value <= due ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > due)
        {
            return SlaStatus.Breached;
        }

        if (atRiskThresholdPercent <= 0)
        {
            return SlaStatus.WithinSla;
        }

        var totalSeconds = (due - createdAtUtc).TotalSeconds;
        if (totalSeconds <= 0)
        {
            // Defensive: a zero-width window is at risk by definition.
            return SlaStatus.AtRisk;
        }

        var remainingSeconds = (due - nowUtc).TotalSeconds;
        var remainingPercent = remainingSeconds / totalSeconds * 100d;

        return remainingPercent <= atRiskThresholdPercent
            ? SlaStatus.AtRisk
            : SlaStatus.WithinSla;
    }
}