namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Body of POST /api/tickets/{id}/escalate. Until Task 3 reads the actor from the token,
/// <see cref="EscalatedBy"/> is supplied by the caller.
/// </summary>
public sealed record EscalateTicketRequest(string Reason, string EscalatedBy);