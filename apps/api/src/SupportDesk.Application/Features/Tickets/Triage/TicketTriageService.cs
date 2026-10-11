using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

/// <summary>
/// Applies the create-time triage rules (BR-1..BR-5) and returns the decisions plus
/// a human-readable reason for each one.
/// </summary>
public sealed class TicketTriageService(
    IAgentSelectionQuery agentSelectionQuery,
    AgentSelector agentSelector,
    SlaWindowRules slaWindowRules,
    IClock clock)
{
    public async Task<TriageResult> TriageAsync(
        Category category,
        Customer customer,
        TicketPriority? requestedPriority,
        CancellationToken ct)
    {
        // BR-1: effective priority.
        var effectivePriority = PriorityResolver.Resolve(
            requestedPriority,
            category.ForcesCriticalPriority);

        var priorityReason = category.ForcesCriticalPriority
            ? $"Category '{category.Name}' forces Critical."
            : requestedPriority is null
                ? "No priority requested; defaulted to Medium."
                : $"Using the requested priority '{requestedPriority}'.";

        // BR-2, BR-3: due date.
        var createdAtUtc = clock.UtcNow;
        var dueAtUtc = SlaCalculator.CalculateDueAt(
            effectivePriority,
            customer.Tier,
            createdAtUtc,
            slaWindowRules);

        var baseHours = slaWindowRules.BaseHoursFor(effectivePriority);
        var windowHours = (dueAtUtc - createdAtUtc).TotalHours;

        var slaWindowReason = customer.Tier == CustomerTier.Premium
            ? $"{effectivePriority} base {baseHours}h, halved for Premium customer ({windowHours:0.##}h)."
            : $"{effectivePriority} base {baseHours}h ({windowHours:0.##}h).";

        // BR-4, BR-5: agent selection.
        var candidates = await agentSelectionQuery.GetCandidatesAsync(ct);
        var selection = agentSelector.Select(candidates, category.Id, category.RequiresSpecialist);

        AgentSummaryDto? assignedAgent = null;
        if (selection.SelectedAgentId is { } agentId)
        {
            var picked = candidates.First(c => c.AgentId == agentId);
            assignedAgent = new AgentSummaryDto(picked.AgentId, picked.FullName);
        }

        return new TriageResult
        {
            Priority = effectivePriority,
            PriorityReason = priorityReason,
            DueAtUtc = dueAtUtc,
            SlaWindowReason = slaWindowReason,
            AssignedAgent = assignedAgent,
            AssignmentReason = selection.Reason
        };
    }
}