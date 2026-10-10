namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// What the agent selector needs to know about one agent, in one row.
/// </summary>
public sealed record AgentSelectionCandidate(
    int AgentId,
    string FullName,
    bool IsActive,
    int MaxOpenTickets,
    int OpenTicketCount,
    IReadOnlyCollection<int> SpecializationCategoryIds);