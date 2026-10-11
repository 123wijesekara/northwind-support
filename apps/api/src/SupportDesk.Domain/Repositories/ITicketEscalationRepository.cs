using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Persists and reads <see cref="TicketEscalation"/> history rows.
/// </summary>
public interface ITicketEscalationRepository
{
    Task AddAsync(TicketEscalation escalation, CancellationToken ct);

    /// <summary>History rows for a ticket, newest first.</summary>
    Task<IReadOnlyList<TicketEscalation>> GetForTicketAsync(int ticketId, CancellationToken ct);
}