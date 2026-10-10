using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Read side of agent selection: one row per active agent, with the open-ticket count
/// and the category ids they specialize in.
/// </summary>
public interface IAgentSelectionQuery
{
    Task<IReadOnlyList<AgentSelectionCandidate>> GetCandidatesAsync(CancellationToken ct);
}