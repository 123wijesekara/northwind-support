using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

/// <summary>
/// Picks the best agent for a new ticket, following BR-4 (specialists) and BR-5
/// (fewest open, deterministic tie-break, strictly below the limit).
/// Pure: it only sees the candidate data it is given, so it is trivially testable.
/// </summary>
public sealed class AgentSelector
{
    public AgentSelectionOutcome Select(
        IReadOnlyList<AgentSelectionCandidate> candidates,
        int categoryId,
        bool categoryRequiresSpecialist)
    {
        if (candidates.Count == 0)
        {
            return new AgentSelectionOutcome(null, "No active agents are available.");
        }

        var eligible = candidates.AsEnumerable();

        if (categoryRequiresSpecialist)
        {
            eligible = eligible.Where(c => c.SpecializationCategoryIds.Contains(categoryId));

            if (!eligible.Any())
            {
                return new AgentSelectionOutcome(
                    null,
                    "No active agent specializes in this category.");
            }
        }

        var belowLimit = eligible.Where(c => c.OpenTicketCount < c.MaxOpenTickets);

        if (!belowLimit.Any())
        {
            return new AgentSelectionOutcome(
                null,
                categoryRequiresSpecialist
                    ? "All eligible specialists are at their open-ticket limit."
                    : "All eligible agents are at their open-ticket limit.");
        }

        var winner = belowLimit
            .OrderBy(c => c.OpenTicketCount)
            .ThenBy(c => c.AgentId)
            .First();

        return new AgentSelectionOutcome(
            winner.AgentId,
            $"Assigned to {winner.FullName} ({winner.OpenTicketCount} open of {winner.MaxOpenTickets}).");
    }
}