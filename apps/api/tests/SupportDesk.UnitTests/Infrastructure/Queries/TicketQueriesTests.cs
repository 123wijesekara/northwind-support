using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Common;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

/// <summary>
/// Query-level tests. These run against SQL Server because the SLA filter relies on
/// date arithmetic that SQLite cannot translate. The test creates and drops its own
/// database so it does not affect the development one.
/// </summary>
public sealed class TicketQueriesTests : IAsyncLifetime
{
    private readonly string _dbName = $"SupportDeskTests_{Guid.NewGuid():N}";
    private SupportDbContext _db = null!;

    private static string BuildConnectionString(string dbName) =>
        $"Server=localhost;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlServer(BuildConnectionString(_dbName))
            .Options;

        _db = new SupportDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _db.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Best effort.
        }
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByPriority_ExactMatch()
    {
        Seed(
            SeedTicket("TCK-0001", priority: TicketPriority.High),
            SeedTicket("TCK-0002", priority: TicketPriority.Medium),
            SeedTicket("TCK-0003", priority: TicketPriority.High));

        var result = await Query(new TicketQuery { Priority = TicketPriority.High });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(TicketPriority.High, t.Priority));
    }

    [Fact]
    public async Task GetPagedAsync_CombinesFilters_WithAnd()
    {
        Seed(
            SeedTicket("TCK-0001", priority: TicketPriority.High, status: TicketStatus.Open),
            SeedTicket("TCK-0002", priority: TicketPriority.High, status: TicketStatus.Closed),
            SeedTicket("TCK-0003", priority: TicketPriority.Medium, status: TicketStatus.Open));

        var result = await Query(new TicketQuery
        {
            Priority = TicketPriority.High,
            Status = TicketStatus.Open
        });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0001", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SearchMatchesCustomerName()
    {
        Seed(
            SeedTicket("TCK-0001", customerName: "Contoso Ltd"),
            SeedTicket("TCK-0002", customerName: "Fabrikam Inc"),
            SeedTicket("TCK-0003", customerName: "Adventure Works"));

        var result = await Query(new TicketQuery { Search = "Contoso" });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0001", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SearchMatchesReference()
    {
        Seed(SeedTicket("TCK-0001"), SeedTicket("TCK-0002"));

        var result = await Query(new TicketQuery { Search = "TCK-0002" });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0002", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SearchMatchesTitle()
    {
        Seed(
            SeedTicket("TCK-0001", title: "Payment failure"),
            SeedTicket("TCK-0002", title: "Outage in production"));

        var result = await Query(new TicketQuery { Search = "outage" });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0002", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_UnassignedOnly_ReturnsOnlyUnassigned()
    {
        Seed(
            SeedTicket("TCK-0001", assignedAgentId: 1),
            SeedTicket("TCK-0002", assignedAgentId: null),
            SeedTicket("TCK-0003", assignedAgentId: null));

        var result = await Query(new TicketQuery { UnassignedOnly = true });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Null(t.AssignedAgent));
    }

    [Fact]
    public async Task GetPagedAsync_BlankSearch_IsIgnored()
    {
        Seed(SeedTicket("TCK-0001"), SeedTicket("TCK-0002"));

        var result = await Query(new TicketQuery { Search = "   " });

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_SlaStatusNotApplicable_ReturnsOnlyTicketsWithoutDueDate()
    {
        Seed(
            new TicketSeed("TCK-0001", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(4)),
            new TicketSeed("TCK-0002", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow, null));

        var result = await Query(new TicketQuery { SlaStatus = SlaStatus.NotApplicable });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0002", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SlaStatusBreached_ReturnsOpenPastDueTickets()
    {
        Seed(
            new TicketSeed("TCK-0001", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow.AddHours(-5), FixedClock.DefaultNow.AddHours(-1)),
            new TicketSeed("TCK-0002", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(10)));

        var result = await Query(new TicketQuery { SlaStatus = SlaStatus.Breached });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0001", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SlaStatusAtRisk_ReturnsOpenNearDueTickets()
    {
        Seed(
            // Created 3h ago, due in 1h → 25% remaining → AtRisk.
            new TicketSeed("TCK-0001", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow.AddHours(-3), FixedClock.DefaultNow.AddHours(1)),
            // Created 1h ago, due in 3h → 75% remaining → WithinSla.
            new TicketSeed("TCK-0002", TicketPriority.Medium, TicketStatus.Open, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow.AddHours(-1), FixedClock.DefaultNow.AddHours(3)));

        var result = await Query(new TicketQuery { SlaStatus = SlaStatus.AtRisk });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0001", result.Items.Single().Reference);
    }

    [Fact]
    public async Task GetPagedAsync_SlaStatusMet_ReturnsResolvedOnTimeTickets()
    {
        Seed(
            new TicketSeed("TCK-0001", TicketPriority.Medium, TicketStatus.Resolved, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(4),
                FixedClock.DefaultNow.AddHours(2)),
            new TicketSeed("TCK-0002", TicketPriority.Medium, TicketStatus.Resolved, null, "Contoso Ltd",
                "Something is not working", FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(4),
                FixedClock.DefaultNow.AddHours(6)));

        var result = await Query(new TicketQuery { SlaStatus = SlaStatus.Met });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0001", result.Items.Single().Reference);
    }

    // ---------- Helpers ----------

    private async Task<PagedResult<TicketListItemDto>> Query(TicketQuery query)
    {
        var sut = new TicketQueries(_db, new FixedClock(), SlaWindowRules.Default);
        return await sut.GetPagedAsync(query.Normalized(), CancellationToken.None);
    }

    private void Seed(params TicketSeed[] seeds)
    {
        var category = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        _db.Set<Category>().Add(category);
        _db.SaveChanges();

        var customerNames = seeds.Select(s => s.CustomerName).Distinct().ToList();
        foreach (var name in customerNames)
        {
            _db.Set<Customer>().Add(new Customer(
                name,
                $"{name.Replace(" ", "").ToLowerInvariant()}@example.com",
                null,
                CustomerTier.Standard,
                FixedClock.DefaultNow));
        }

        var agentIds = seeds.Where(s => s.AssignedAgentId is not null)
                            .Select(s => s.AssignedAgentId!.Value)
                            .Distinct()
                            .ToList();
        foreach (var id in agentIds)
        {
            _db.Set<Agent>().Add(new Agent(
                $"Agent {id}",
                $"agent{id}@example.com",
                maxOpenTickets: 10,
                FixedClock.DefaultNow));
        }

        _db.SaveChanges();

        foreach (var seed in seeds)
        {
            var customerId = _db.Set<Customer>().First(c => c.Name == seed.CustomerName).Id;

            var ticket = Ticket.Raise(
                seed.Reference,
                seed.Title,
                "Description long enough to be valid.",
                customerId,
                category.Id,
                seed.Priority,
                seed.CreatedAtUtc);

            if (seed.AssignedAgentId is { } agentId)
            {
                ticket.AssignTo(agentId, seed.CreatedAtUtc);
            }

            if (seed.Status != TicketStatus.New)
            {
                ticket.ChangeStatus(seed.Status, seed.ResolvedAtUtc ?? FixedClock.DefaultNow);
            }

            _db.Set<Ticket>().Add(ticket);
            _db.SaveChanges();

            // Set the fields no domain method exposes for test purposes.
            _db.Entry(ticket).Property(t => t.DueAtUtc).CurrentValue = seed.DueAtUtc;
            _db.Entry(ticket).Property(t => t.CreatedAtUtc).CurrentValue = seed.CreatedAtUtc;
            _db.SaveChanges();
        }
    }

    private static TicketSeed SeedTicket(
        string reference,
        TicketPriority priority = TicketPriority.Medium,
        TicketStatus status = TicketStatus.New,
        int? assignedAgentId = null,
        string customerName = "Contoso Ltd",
        string title = "Something is not working") =>
        new(reference, priority, status, assignedAgentId, customerName, title,
            FixedClock.DefaultNow, FixedClock.DefaultNow.AddHours(24));

    private sealed record TicketSeed(
        string Reference,
        TicketPriority Priority,
        TicketStatus Status,
        int? AssignedAgentId,
        string CustomerName,
        string Title,
        DateTime CreatedAtUtc,
        DateTime? DueAtUtc,
        DateTime? ResolvedAtUtc = null);
}