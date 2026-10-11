using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

/// <summary>
/// Escalates a ticket one priority step (BR-7). The actor (who did it) comes from the
/// caller's identity, not the request body.
/// </summary>
public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    ITicketEscalationRepository escalations,
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentSelectionQuery agentSelectionQuery,
    AgentSelector agentSelector,
    SlaWindowRules slaWindowRules,
    IUnitOfWork unitOfWork,
    IClock clock,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<TicketDetailDto> HandleAsync(
        int ticketId,
        EscalateTicketRequest request,
        string escalatedBy,
        CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(ticketId, ct)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            throw new ConflictException(
                $"Ticket {ticket.Reference} is {ticket.Status} and cannot be escalated.");
        }

        if (ticket.Priority == TicketPriority.Critical)
        {
            throw new ConflictException(
                $"Ticket {ticket.Reference} is already Critical and cannot be escalated.");
        }

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);

        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);

        var newPriority = (TicketPriority)((int)ticket.Priority + 1);
        var now = clock.UtcNow;

        var newDueAtUtc = SlaCalculator.CalculateDueAt(
            newPriority,
            customer.Tier,
            createdAtUtc: now,
            rules: slaWindowRules);

        var newAgentId = await ResolveOwnerAsync(ticket, category, ct);

        var history = ticket.Escalate(
            newPriority: newPriority,
            newAgentId: newAgentId,
            newDueAtUtc: newDueAtUtc,
            reason: request.Reason,
            escalatedBy: escalatedBy,
            nowUtc: now);

        await escalations.AddAsync(history, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {From} to {To} by {Who}.",
            ticket.Reference, history.FromPriority, history.ToPriority, escalatedBy);

        return await getTicket.HandleAsync(ticket.Id, ct);
    }

    private async Task<int?> ResolveOwnerAsync(Ticket ticket, Category category, CancellationToken ct)
    {
        var candidates = await agentSelectionQuery.GetCandidatesAsync(ct);

        if (ticket.AssignedAgentId is { } currentId)
        {
            var current = candidates.FirstOrDefault(c => c.AgentId == currentId);
            if (current is not null && IsEligible(current, category))
            {
                return currentId;
            }
        }

        var selection = agentSelector.Select(candidates, category.Id, category.RequiresSpecialist);
        return selection.SelectedAgentId;
    }

    private static bool IsEligible(AgentSelectionCandidate candidate, Category category)
    {
        if (candidate.OpenTicketCount >= candidate.MaxOpenTickets)
        {
            return false;
        }

        if (category.RequiresSpecialist &&
            !candidate.SpecializationCategoryIds.Contains(category.Id))
        {
            return false;
        }

        return true;
    }
}