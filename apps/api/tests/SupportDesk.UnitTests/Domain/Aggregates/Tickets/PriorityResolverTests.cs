using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class PriorityResolverTests
{
    [Theory]
    [InlineData(null, TicketPriority.Medium)]
    [InlineData(TicketPriority.Low, TicketPriority.Low)]
    [InlineData(TicketPriority.High, TicketPriority.High)]
    [InlineData(TicketPriority.Critical, TicketPriority.Critical)]
    public void Resolve_NoForcedCritical_UsesRequestedOrDefault(TicketPriority? requested, TicketPriority expected)
    {
        var actual = PriorityResolver.Resolve(requested, categoryForcesCritical: false);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TicketPriority.Low)]
    [InlineData(TicketPriority.Medium)]
    [InlineData(TicketPriority.High)]
    public void Resolve_ForcedCriticalCategory_AlwaysReturnsCritical(TicketPriority? requested)
    {
        var actual = PriorityResolver.Resolve(requested, categoryForcesCritical: true);

        Assert.Equal(TicketPriority.Critical, actual);
    }
}