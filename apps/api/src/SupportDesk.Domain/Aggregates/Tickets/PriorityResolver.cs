namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>
/// Decides the priority a ticket is actually handled at. Pure: category flags and the
/// requested priority in, the effective priority out.
/// </summary>
/// <remarks>
/// BR-1: use the requested priority; default to Medium. A category flagged
/// <c>ForcesCriticalPriority</c> always wins, whatever the reporter asked for.
/// </remarks>
public static class PriorityResolver
{
    public static TicketPriority Resolve(
        TicketPriority? requestedPriority,
        bool categoryForcesCritical)
    {
        if (categoryForcesCritical)
        {
            return TicketPriority.Critical;
        }

        return requestedPriority ?? TicketPriority.Medium;
    }
}