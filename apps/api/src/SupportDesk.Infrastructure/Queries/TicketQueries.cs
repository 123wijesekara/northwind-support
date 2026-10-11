using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Queries;

/// <summary>
/// Ticket reads. Projected in the database and never tracked.
/// </summary>
public sealed class TicketQueries(
    SupportDbContext db,
    IClock clock,
    SlaWindowRules slaWindowRules) : ITicketQueries
{
    public async Task<PagedResult<TicketListItemDto>> GetPagedAsync(TicketQuery query, CancellationToken ct)
    {
        var tickets = ApplyFilters(TicketsWithLabels(), query);

        var totalCount = await tickets.CountAsync(ct);

        var now = clock.UtcNow;

        var items = await ApplySort(tickets, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new TicketListItemDto
            {
                Id = x.Ticket.Id,
                Reference = x.Ticket.Reference,
                Title = x.Ticket.Title,
                Status = x.Ticket.Status,
                Priority = x.Ticket.Priority,
                Customer = new CustomerSummaryDto(x.Customer.Id, x.Customer.Name, x.Customer.Tier),
                Category = new CategorySummaryDto(x.Category.Id, x.Category.Name),
                AssignedAgent = x.Agent == null ? null : new AgentSummaryDto(x.Agent.Id, x.Agent.FullName),
                CreatedAtUtc = x.Ticket.CreatedAtUtc,
                UpdatedAtUtc = x.Ticket.UpdatedAtUtc,
                DueAtUtc = x.Ticket.DueAtUtc,
                ResolvedAtUtc = x.Ticket.ResolvedAtUtc,
                SlaStatus = SlaEvaluator.Evaluate(
                    x.Ticket.DueAtUtc,
                    x.Ticket.ResolvedAtUtc,
                    x.Ticket.CreatedAtUtc,
                    now,
                    slaWindowRules.AtRiskThresholdPercent)
            })
            .ToListAsync(ct);

        return new PagedResult<TicketListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<TicketDetailDto?> GetDetailAsync(int id, CancellationToken ct)
    {
        var now = clock.UtcNow;

        return TicketsWithLabels()
            .Where(x => x.Ticket.Id == id)
            .Select(x => new TicketDetailDto
            {
                Id = x.Ticket.Id,
                Reference = x.Ticket.Reference,
                Title = x.Ticket.Title,
                Description = x.Ticket.Description,
                Status = x.Ticket.Status,
                Priority = x.Ticket.Priority,
                Customer = new CustomerContactDto(
                    x.Customer.Id, x.Customer.Name, x.Customer.Email, x.Customer.Phone, x.Customer.Tier),
                Category = new CategoryDto(x.Category.Id, x.Category.Name, x.Category.RequiresSpecialist),
                AssignedAgent = x.Agent == null ? null : new AgentSummaryDto(x.Agent.Id, x.Agent.FullName),
                CreatedAtUtc = x.Ticket.CreatedAtUtc,
                UpdatedAtUtc = x.Ticket.UpdatedAtUtc,
                DueAtUtc = x.Ticket.DueAtUtc,
                ResolvedAtUtc = x.Ticket.ResolvedAtUtc,
                SlaStatus = SlaEvaluator.Evaluate(
                    x.Ticket.DueAtUtc,
                    x.Ticket.ResolvedAtUtc,
                    x.Ticket.CreatedAtUtc,
                    now,
                    slaWindowRules.AtRiskThresholdPercent)
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<TicketListItemDto>> GetForCustomerAsync(int customerId, CancellationToken ct)
    {
        var now = clock.UtcNow;

        return await TicketsWithLabels()
            .Where(x => x.Ticket.CustomerId == customerId)
            .OrderByDescending(x => x.Ticket.CreatedAtUtc)
            .ThenByDescending(x => x.Ticket.Id)
            .Select(x => new TicketListItemDto
            {
                Id = x.Ticket.Id,
                Reference = x.Ticket.Reference,
                Title = x.Ticket.Title,
                Status = x.Ticket.Status,
                Priority = x.Ticket.Priority,
                Customer = new CustomerSummaryDto(x.Customer.Id, x.Customer.Name, x.Customer.Tier),
                Category = new CategorySummaryDto(x.Category.Id, x.Category.Name),
                AssignedAgent = x.Agent == null ? null : new AgentSummaryDto(x.Agent.Id, x.Agent.FullName),
                CreatedAtUtc = x.Ticket.CreatedAtUtc,
                UpdatedAtUtc = x.Ticket.UpdatedAtUtc,
                DueAtUtc = x.Ticket.DueAtUtc,
                ResolvedAtUtc = x.Ticket.ResolvedAtUtc,
                SlaStatus = SlaEvaluator.Evaluate(
                    x.Ticket.DueAtUtc,
                    x.Ticket.ResolvedAtUtc,
                    x.Ticket.CreatedAtUtc,
                    now,
                    slaWindowRules.AtRiskThresholdPercent)
            })
            .ToListAsync(ct);
    }

    private IQueryable<TicketWithLabels> TicketsWithLabels() =>
        from ticket in db.Set<Ticket>().AsNoTracking()
        join customer in db.Set<Customer>() on ticket.CustomerId equals customer.Id
        join category in db.Set<Category>() on ticket.CategoryId equals category.Id
        from agent in db.Set<Agent>().Where(a => a.Id == ticket.AssignedAgentId).DefaultIfEmpty()
        select new TicketWithLabels { Ticket = ticket, Customer = customer, Category = category, Agent = agent };

    /// <summary>
    /// Applies each supplied filter in SQL. Adding a new filter means adding one
    /// <c>Where</c> here — no restructuring.
    /// </summary>
    private IQueryable<TicketWithLabels> ApplyFilters(IQueryable<TicketWithLabels> source, TicketQuery query)
    {
        if (query.Status is { } status)
        {
            source = source.Where(x => x.Ticket.Status == status);
        }

        if (query.Priority is { } priority)
        {
            source = source.Where(x => x.Ticket.Priority == priority);
        }

        if (query.CategoryId is { } categoryId)
        {
            source = source.Where(x => x.Ticket.CategoryId == categoryId);
        }

        if (query.CustomerId is { } customerId)
        {
            source = source.Where(x => x.Ticket.CustomerId == customerId);
        }

        if (query.AssignedAgentId is { } agentId)
        {
            source = source.Where(x => x.Ticket.AssignedAgentId == agentId);
        }

        if (query.UnassignedOnly)
        {
            source = source.Where(x => x.Ticket.AssignedAgentId == null);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            var pattern = $"%{term}%";

            source = source.Where(x =>
                EF.Functions.Like(x.Ticket.Title, pattern) ||
                EF.Functions.Like(x.Ticket.Reference, pattern) ||
                EF.Functions.Like(x.Customer.Name, pattern));
        }

        // BR-8 + FL-6: SLA status filter — one more Where, no restructuring.
        // Status is derived, so its definition is translated into SQL here. This mirrors
        // SlaEvaluator.Evaluate; keep the two in sync.
        if (query.SlaStatus is { } slaStatus)
        {
            var now = clock.UtcNow;
            var threshold = slaWindowRules.AtRiskThresholdPercent;

            source = slaStatus switch
            {
                SlaStatus.NotApplicable => source.Where(x => x.Ticket.DueAtUtc == null),

                SlaStatus.Met => source.Where(x =>
                    x.Ticket.ResolvedAtUtc != null &&
                    x.Ticket.DueAtUtc != null &&
                    x.Ticket.ResolvedAtUtc <= x.Ticket.DueAtUtc),

                SlaStatus.Breached => source.Where(x =>
                    (x.Ticket.ResolvedAtUtc != null &&
                     x.Ticket.DueAtUtc != null &&
                     x.Ticket.ResolvedAtUtc > x.Ticket.DueAtUtc) ||
                    (x.Ticket.ResolvedAtUtc == null &&
                     x.Ticket.DueAtUtc != null &&
                     now > x.Ticket.DueAtUtc)),

                               SlaStatus.AtRisk => source.Where(x =>
                    x.Ticket.ResolvedAtUtc == null &&
                    x.Ticket.DueAtUtc != null &&
                    now <= x.Ticket.DueAtUtc &&
                    EF.Functions.DateDiffMillisecond(now, x.Ticket.DueAtUtc.Value) * 100
                        <= EF.Functions.DateDiffMillisecond(x.Ticket.CreatedAtUtc, x.Ticket.DueAtUtc.Value) * threshold),

                SlaStatus.WithinSla => source.Where(x =>
                    x.Ticket.ResolvedAtUtc == null &&
                    x.Ticket.DueAtUtc != null &&
                    now <= x.Ticket.DueAtUtc &&
                    EF.Functions.DateDiffMillisecond(now, x.Ticket.DueAtUtc.Value) * 100
                        > EF.Functions.DateDiffMillisecond(x.Ticket.CreatedAtUtc, x.Ticket.DueAtUtc.Value) * threshold),

                _ => source
            };
        }

        return source;
    }

    private static IQueryable<TicketWithLabels> ApplySort(IQueryable<TicketWithLabels> source, TicketQuery query)
    {
        var descending = !string.Equals(query.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "updatedatutc" => descending
                ? source.OrderByDescending(x => x.Ticket.UpdatedAtUtc)
                : source.OrderBy(x => x.Ticket.UpdatedAtUtc),
            "dueatutc" => descending
                ? source.OrderByDescending(x => x.Ticket.DueAtUtc)
                : source.OrderBy(x => x.Ticket.DueAtUtc),
            "priority" => descending
                ? source.OrderByDescending(x => x.Ticket.Priority)
                : source.OrderBy(x => x.Ticket.Priority),
            "status" => descending
                ? source.OrderByDescending(x => x.Ticket.Status)
                : source.OrderBy(x => x.Ticket.Status),
            _ => descending
                ? source.OrderByDescending(x => x.Ticket.CreatedAtUtc)
                : source.OrderBy(x => x.Ticket.CreatedAtUtc)
        };

        return sorted.ThenByDescending(x => x.Ticket.Id);
    }

    private sealed class TicketWithLabels
    {
        public required Ticket Ticket { get; init; }
        public required Customer Customer { get; init; }
        public required Category Category { get; init; }
        public Agent? Agent { get; init; }
    }
}