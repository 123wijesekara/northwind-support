using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Queries;

/// <summary>
/// Loads active agents and their open-ticket counts in one round trip.
/// </summary>
public sealed class AgentSelectionQuery(SupportDbContext db) : IAgentSelectionQuery
{
    public async Task<IReadOnlyList<AgentSelectionCandidate>> GetCandidatesAsync(CancellationToken ct)
    {
        var openStatuses = new[] { TicketStatus.New, TicketStatus.Open, TicketStatus.InProgress };

        var rows = await db.Set<Agent>()
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new
            {
                a.Id,
                a.FullName,
                a.IsActive,
                a.MaxOpenTickets,
                OpenTicketCount = db.Set<Ticket>()
                    .Count(t => t.AssignedAgentId == a.Id && openStatuses.Contains(t.Status)),
                CategoryIds = a.Specializations.Select(s => s.CategoryId).ToList()
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new AgentSelectionCandidate(
                r.Id,
                r.FullName,
                r.IsActive,
                r.MaxOpenTickets,
                r.OpenTicketCount,
                r.CategoryIds))
            .ToList();
    }
}