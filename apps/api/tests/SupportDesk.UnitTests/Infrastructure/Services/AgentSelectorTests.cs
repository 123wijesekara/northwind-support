using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public class AgentSelectorTests
{
    private readonly AgentSelector _sut = new();

    private static AgentSelectionCandidate Agent(
        int id,
        int openCount = 0,
        int maxOpen = 10,
        bool active = true,
        params int[] specializations)
        => new(
            AgentId: id,
            FullName: $"Agent {id}",
            IsActive: active,
            MaxOpenTickets: maxOpen,
            OpenTicketCount: openCount,
            SpecializationCategoryIds: specializations);

    [Fact]
    public void Select_NoCandidates_ReturnsUnassignedWithReason()
    {
        var result = _sut.Select(Array.Empty<AgentSelectionCandidate>(), categoryId: 1, categoryRequiresSpecialist: false);

        Assert.False(result.Assigned);
        Assert.Contains("No active agents", result.Reason);
    }

    [Fact]
    public void Select_PicksAgentWithFewestOpenTickets()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 5),
            Agent(2, openCount: 2),
            Agent(3, openCount: 7)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: false);

        Assert.Equal(2, result.SelectedAgentId);
    }

    [Fact]
    public void Select_TieOnOpenCount_BreaksDeterministicallyByAgentId()
    {
        var candidates = new[]
        {
            Agent(3, openCount: 3),
            Agent(1, openCount: 3),
            Agent(2, openCount: 3)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: false);

        Assert.Equal(1, result.SelectedAgentId);
    }

    [Fact]
    public void Select_CategoryRequiresSpecialist_OnlySpecialistsConsidered()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 0, specializations: 1),
            Agent(2, openCount: 0, specializations: 2),
            Agent(3, openCount: 1, specializations: 1)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: true);

        Assert.Equal(1, result.SelectedAgentId);
    }

    [Fact]
    public void Select_NoSpecialistForCategory_ReturnsUnassignedWithReason()
    {
        var candidates = new[]
        {
            Agent(1, specializations: 2),
            Agent(2, specializations: 3)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: true);

        Assert.False(result.Assigned);
        Assert.Contains("specializes", result.Reason);
    }

    [Fact]
    public void Select_AllCandidatesAtLimit_ReturnsUnassignedWithReason()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 10, maxOpen: 10),
            Agent(2, openCount: 8, maxOpen: 8)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: false);

        Assert.False(result.Assigned);
        Assert.Contains("limit", result.Reason);
    }

    [Fact]
    public void Select_AllSpecialistsAtLimit_ReturnsUnassignedWithReason()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 10, maxOpen: 10, specializations: 1),
            Agent(2, openCount: 8, maxOpen: 8, specializations: 1)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: true);

        Assert.False(result.Assigned);
        Assert.Contains("specialists", result.Reason);
    }

    [Fact]
    public void Select_AgentStrictlyBelowLimitIsEligible()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 9, maxOpen: 10)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: false);

        Assert.Equal(1, result.SelectedAgentId);
    }

    [Fact]
    public void Select_AgentExactlyAtLimitIsNotEligible()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 10, maxOpen: 10),
            Agent(2, openCount: 5, maxOpen: 10)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: false);

        Assert.Equal(2, result.SelectedAgentId);
    }

    [Fact]
    public void Select_SpecialistWithFewerOpenWins()
    {
        var candidates = new[]
        {
            Agent(1, openCount: 3, specializations: 1),
            Agent(2, openCount: 1, specializations: 1),
            Agent(3, openCount: 0, specializations: 2)
        };

        var result = _sut.Select(candidates, categoryId: 1, categoryRequiresSpecialist: true);

        Assert.Equal(2, result.SelectedAgentId);
    }
}