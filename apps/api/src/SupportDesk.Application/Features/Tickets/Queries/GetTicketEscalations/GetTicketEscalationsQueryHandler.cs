using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Queries.GetTicketEscalations;

/// <summary>
/// Returns a ticket's escalation history, newest first.
/// </summary>
public sealed class GetTicketEscalationsQueryHandler(
    ITicketRepository tickets,
    ITicketEscalationRepository escalations,
    IAgentRepository agents)
{
    public async Task<IReadOnlyList<TicketEscalationDto>> HandleAsync(int ticketId, CancellationToken ct)
    {
        _ = await tickets.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        var rows = await escalations.GetForTicketAsync(ticketId, ct);

        if (rows.Count == 0)
        {
            return Array.Empty<TicketEscalationDto>();
        }

        // Load the agent labels in one pass.
        var agentIds = rows
            .SelectMany(r => new[] { r.FromAgentId, r.ToAgentId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var agentsById = new Dictionary<int, string>();
        foreach (var id in agentIds)
        {
            var agent = await agents.GetByIdAsync(id, ct);
            if (agent is not null)
            {
                agentsById[id] = agent.FullName;
            }
        }

        return rows.Select(r => new TicketEscalationDto
        {
            Id = r.Id,
            TicketId = r.TicketId,
            FromPriority = r.FromPriority,
            ToPriority = r.ToPriority,
            FromAgent = r.FromAgentId is { } f ? new AgentSummaryDto(f, agentsById[f]) : null,
            ToAgent = r.ToAgentId is { } t ? new AgentSummaryDto(t, agentsById[t]) : null,
            FromDueAtUtc = r.FromDueAtUtc,
            ToDueAtUtc = r.ToDueAtUtc,
            Reason = r.Reason,
            EscalatedBy = r.EscalatedBy,
            EscalatedAtUtc = r.EscalatedAtUtc
        }).ToList();
    }
}