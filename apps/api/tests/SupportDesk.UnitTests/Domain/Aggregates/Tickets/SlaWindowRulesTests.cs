using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaWindowRulesTests
{
    [Fact]
    public void BaseHoursFor_ReturnsConfiguredHours()
    {
        var rules = SlaWindowRules.Default;

        Assert.Equal(72, rules.BaseHoursFor(TicketPriority.Low));
        Assert.Equal(24, rules.BaseHoursFor(TicketPriority.Medium));
        Assert.Equal(8, rules.BaseHoursFor(TicketPriority.High));
        Assert.Equal(4, rules.BaseHoursFor(TicketPriority.Critical));
    }

    [Fact]
    public void BaseHoursFor_UnknownPriority_Throws()
    {
        var rules = SlaWindowRules.Default with
        {
            BaseWindowHours = new Dictionary<TicketPriority, int>()
        };

        var ex = Assert.Throws<InvalidOperationException>(() => rules.BaseHoursFor(TicketPriority.High));
        Assert.Contains("High", ex.Message);
    }
}