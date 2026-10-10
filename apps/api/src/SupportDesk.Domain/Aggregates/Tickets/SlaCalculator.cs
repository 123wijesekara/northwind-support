using SupportDesk.Domain.Aggregates.Customers;

namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Works out when a ticket's response window ends. Pure: given the same inputs it always
/// produces the same due date. No clock, no database, no I/O.
/// </summary>
/// <remarks>
/// BR-2: due = creation + base window for the priority.
/// BR-3: Premium customers get half the window, but never shorter than the floor.
/// </remarks>
public static class SlaCalculator
{
    public static DateTime CalculateDueAt(
        TicketPriority priority,
        CustomerTier tier,
        DateTime createdAtUtc,
        SlaWindowRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var baseHours = rules.BaseHoursFor(priority);
        var hours = tier == CustomerTier.Premium
            ? baseHours * rules.PremiumCustomerMultiplier
            : baseHours;

        var floored = Math.Max(hours, rules.MinimumWindowHours);

        // Round to whole minutes: the DB stores datetime2(3), tests compare cleanly,
        // and second-level drift is not meaningful for an SLA window.
        var minutes = (int)Math.Round(floored * 60, MidpointRounding.AwayFromZero);

        return createdAtUtc.AddMinutes(minutes);
    }
}