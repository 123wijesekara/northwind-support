using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public class EscalateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ITicketEscalationRepository> _escalations = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IAgentSelectionQuery> _selectionQuery = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ITicketQueries> _queries = new();
    private readonly FixedClock _clock = new();

    public EscalateTicketCommandHandlerTests()
    {
        _customers.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Contoso", "c@example.com", null, CustomerTier.Standard, _clock.UtcNow));

        _categories.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category("General", requiresSpecialist: false, forcesCriticalPriority: false));

        _selectionQuery.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AgentSelectionCandidate>());

        _queries.Setup(q => q.GetDetailAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketDetailDto { Id = 1, Reference = "TCK-0001" });

        _escalations
            .Setup(e => e.AddAsync(It.IsAny<TicketEscalation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private EscalateTicketCommandHandler Sut()
    {
        var getTicket = new GetTicketQueryHandler(_queries.Object);

        return new EscalateTicketCommandHandler(
            _tickets.Object,
            _escalations.Object,
            _customers.Object,
            _categories.Object,
            _selectionQuery.Object,
            new AgentSelector(),
            SlaWindowRules.Default,
            _uow.Object,
            _clock,
            getTicket,
            NullLogger<EscalateTicketCommandHandler>.Instance);
    }

    private Ticket ExistingTicket(
        TicketPriority priority = TicketPriority.Medium,
        TicketStatus status = TicketStatus.Open)
    {
        var ticket = Ticket.Raise(
            "TCK-0001",
            "Title long enough",
            "Description long enough.",
            customerId: 1,
            categoryId: 1,
            priority,
            _clock.UtcNow);

        if (status != TicketStatus.New)
        {
            ticket.ChangeStatus(status, _clock.UtcNow);
        }

        _tickets.Setup(t => t.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        return ticket;
    }

    [Fact]
    public async Task Escalate_RaisesPriorityOneStep()
    {
        var ticket = ExistingTicket(TicketPriority.Medium);
        var request = new EscalateTicketRequest("Urgent for the customer.");

        await Sut().HandleAsync(1, request, "alex", CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
    }

    [Fact]
    public async Task Escalate_RecomputesDueDateFromNow()
    {
        var ticket = ExistingTicket(TicketPriority.Medium);

        _clock.Advance(TimeSpan.FromHours(1));

        var request = new EscalateTicketRequest("Escalating due to impact.");
        await Sut().HandleAsync(1, request, "alex", CancellationToken.None);

        Assert.Equal(_clock.UtcNow.AddHours(8), ticket.DueAtUtc);
    }

    [Fact]
    public async Task Escalate_CriticalTicket_Throws409()
    {
        ExistingTicket(TicketPriority.Critical);
        var request = new EscalateTicketRequest("Escalate anyway please.");

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut().HandleAsync(1, request, "alex", CancellationToken.None));

        Assert.Contains("Critical", ex.Message);
    }

    [Fact]
    public async Task Escalate_ResolvedTicket_Throws409()
    {
        ExistingTicket(TicketPriority.Medium, TicketStatus.Resolved);
        var request = new EscalateTicketRequest("Escalate anyway please.");

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut().HandleAsync(1, request, "alex", CancellationToken.None));

        Assert.Contains("Resolved", ex.Message);
    }

    [Fact]
    public async Task Escalate_ClosedTicket_Throws409()
    {
        ExistingTicket(TicketPriority.Medium, TicketStatus.Closed);
        var request = new EscalateTicketRequest("Escalate anyway please.");

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => Sut().HandleAsync(1, request, "alex", CancellationToken.None));

        Assert.Contains("Closed", ex.Message);
    }

    [Fact]
    public async Task Escalate_WritesHistoryRow_WithActorFromCaller()
    {
        var ticket = ExistingTicket(TicketPriority.Medium);
        ticket.AssignTo(99, _clock.UtcNow);

        var request = new EscalateTicketRequest("Escalating due to impact.");
        await Sut().HandleAsync(1, request, "alex@example.com", CancellationToken.None);

        _escalations.Verify(
            e => e.AddAsync(
                It.Is<TicketEscalation>(h =>
                    h.FromPriority == TicketPriority.Medium &&
                    h.ToPriority == TicketPriority.High &&
                    h.Reason == "Escalating due to impact." &&
                    h.EscalatedBy == "alex@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Escalate_KeepsCurrentAgent_WhenStillEligible()
    {
        _selectionQuery.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new AgentSelectionCandidate(5, "Sara", true, 10, 3, Array.Empty<int>())
            });

        var ticket = ExistingTicket(TicketPriority.Medium);
        ticket.AssignTo(5, _clock.UtcNow);

        var request = new EscalateTicketRequest("Keeping the current owner.");
        await Sut().HandleAsync(1, request, "alex", CancellationToken.None);

        Assert.Equal(5, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task Escalate_Reassigns_WhenCurrentAgentAtLimit()
    {
        _selectionQuery.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new AgentSelectionCandidate(5, "Sara", true, 10, 10, Array.Empty<int>()),
                new AgentSelectionCandidate(7, "Alex", true, 10, 2, Array.Empty<int>())
            });

        var ticket = ExistingTicket(TicketPriority.Medium);
        ticket.AssignTo(5, _clock.UtcNow);

        var request = new EscalateTicketRequest("Reassigning to someone free.");
        await Sut().HandleAsync(1, request, "alex", CancellationToken.None);

        Assert.Equal(7, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task Escalate_UnknownTicket_Throws404()
    {
        _tickets.Setup(t => t.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ticket?)null);

        var request = new EscalateTicketRequest("Does not matter here.");

        await Assert.ThrowsAsync<NotFoundException>(
            () => Sut().HandleAsync(999, request, "alex", CancellationToken.None));
    }
}