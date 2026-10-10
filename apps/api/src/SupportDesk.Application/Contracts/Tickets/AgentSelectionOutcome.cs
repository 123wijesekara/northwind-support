namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Result of picking an agent for a ticket. When no agent fits, this is a normal
/// outcome (not an error): <see cref="SelectedAgentId"/> is null and
/// <see cref="Reason"/> explains why.
/// </summary>
public sealed record AgentSelectionOutcome(int? SelectedAgentId, string Reason)
{
    public bool Assigned => SelectedAgentId is not null;
}