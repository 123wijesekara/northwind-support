using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// The decisions the create-time triage made, with a human-readable reason for each.
/// Returned by POST /api/tickets so the client can show why a ticket was prioritised
/// and assigned the way it was.
/// </summary>
public sealed class TriageResult
{
    public TicketPriority Priority { get; init; }

    public string PriorityReason { get; init; } = string.Empty;

    public DateTime? DueAtUtc { get; init; }

    public string SlaWindowReason { get; init; } = string.Empty;

    /// <summary>Null when nobody was eligible; see <see cref="AssignmentReason"/>.</summary>
    public AgentSummaryDto? AssignedAgent { get; init; }

    public string AssignmentReason { get; init; } = string.Empty;
}