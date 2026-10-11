using Moq;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Triage;

public class TicketTriageServiceTests
{
    private readonly Mock<IAgentSelectionQuery> _query = new();
    private readonly FixedClock _clock = new();

    private TicketTriageService Sut() => new(
        _query.Object,
        new AgentSelector(),
        SlaWindowRules.Default,
        _clock);

    private static Category GeneralCategory() =>
        new("General", requiresSpecialist: false, forcesCriticalPriority: false);

    private static Category OutageCategory() =>
        new("Outage", requiresSpecialist: true, forcesCriticalPriority: true);

    private static Customer Standard() =>
        new("Contoso", "c@example.com", null, CustomerTier.Standard, FixedClock.DefaultNow);

    private static Customer Premium() =>
        new("Contoso", "c@example.com", null, CustomerTier.Premium, FixedClock.DefaultNow);

    [Fact]
    public async Task NoRequestedPriority_DefaultsToMedium()
    {
        _query.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<AgentSelectionCandidate>());

        var result = await Sut().TriageAsync(GeneralCategory(), Standard(), requestedPriority: null, CancellationToken.None);

        Assert.Equal(TicketPriority.Medium, result.Priority);
        Assert.Contains("Medium", result.PriorityReason);
    }

    [Fact]
    public async Task ForcedCriticalCategory_OverridesRequestedPriority()
    {
        _query.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<AgentSelectionCandidate>());

        var result = await Sut().TriageAsync(OutageCategory(), Standard(), requestedPriority: TicketPriority.Low, CancellationToken.None);

        Assert.Equal(TicketPriority.Critical, result.Priority);
        Assert.Contains("forces Critical", result.PriorityReason);
    }

    [Fact]
    public async Task PremiumCustomer_HalvesTheWindow()
    {
        _query.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<AgentSelectionCandidate>());

        // Medium = 24h, Premium halves = 12h.
        var result = await Sut().TriageAsync(GeneralCategory(), Premium(), requestedPriority: null, CancellationToken.None);

        var hours = (result.DueAtUtc!.Value - _clock.UtcNow).TotalHours;
        Assert.Equal(12, hours, precision: 2);
        Assert.Contains("halved", result.SlaWindowReason);
    }

    [Fact]
    public async Task NoEligibleAgent_LeavesTicketUnassignedWithReason()
    {
        _query.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<AgentSelectionCandidate>());

        var result = await Sut().TriageAsync(GeneralCategory(), Standard(), null, CancellationToken.None);

        Assert.Null(result.AssignedAgent);
        Assert.Contains("No active agents", result.AssignmentReason);
    }

    [Fact]
    public async Task EligibleAgent_IsChosenAndReasonMentionsThem()
    {
        _query.Setup(q => q.GetCandidatesAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  new AgentSelectionCandidate(1, "Alex Turner", true, 10, 2, new[] { 1 })
              });

        var result = await Sut().TriageAsync(GeneralCategory(), Standard(), null, CancellationToken.None);

        Assert.NotNull(result.AssignedAgent);
        Assert.Equal(1, result.AssignedAgent!.Id);
        Assert.Contains("Alex Turner", result.AssignmentReason);
    }
}