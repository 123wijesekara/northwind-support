using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Options;

/// <summary>
/// Turns the bound <see cref="SlaOptions"/> into the domain's <see cref="SlaWindowRules"/>.
/// Kept in one place so both create and escalate see the same numbers.
/// </summary>
public static class SlaWindowRulesFactory
{
    public static SlaWindowRules FromOptions(SlaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var baseHours = new Dictionary<TicketPriority, int>();
        foreach (var (name, hours) in options.BaseWindowHours)
        {
            if (Enum.TryParse<TicketPriority>(name, ignoreCase: true, out var priority))
            {
                baseHours[priority] = hours;
            }
        }

        return new SlaWindowRules(
            BaseWindowHours: baseHours,
            PremiumCustomerMultiplier: options.PremiumCustomerMultiplier,
            MinimumWindowHours: options.MinimumWindowHours,
            AtRiskThresholdPercent: options.AtRiskThresholdPercent);
    }
}