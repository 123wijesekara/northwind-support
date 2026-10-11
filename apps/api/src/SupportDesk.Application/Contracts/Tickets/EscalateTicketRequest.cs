namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Body of POST /api/tickets/{id}/escalate. The actor is taken from the caller's token,
/// not from the request body.
/// </summary>
public sealed record EscalateTicketRequest(string Reason);