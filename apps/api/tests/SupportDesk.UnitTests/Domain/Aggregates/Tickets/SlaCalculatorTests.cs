using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaCalculatorTests
{
    private static readonly DateTime Created = FixedClock.DefaultNow;

    [Theory]
    [InlineData(TicketPriority.Low, 72)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Critical, 4)]
    public void CalculateDueAt_Standard_UsesBaseHours(TicketPriority priority, int expectedHours)
    {
        var due = SlaCalculator.CalculateDueAt(priority, CustomerTier.Standard, Created, SlaWindowRules.Default);

        Assert.Equal(Created.AddHours(expectedHours), due);
    }

    [Theory]
    [InlineData(TicketPriority.Low, 36)]
    [InlineData(TicketPriority.Medium, 12)]
    [InlineData(TicketPriority.High, 4)]
    public void CalculateDueAt_Premium_HalvesTheWindow(TicketPriority priority, int expectedHours)
    {
        var due = SlaCalculator.CalculateDueAt(priority, CustomerTier.Premium, Created, SlaWindowRules.Default);

        Assert.Equal(Created.AddHours(expectedHours), due);
    }

    [Fact]
    public void CalculateDueAt_PremiumCritical_AppliesTheOneHourFloor()
    {
        // Critical is 4h; half is 2h; well above the 1h floor.
        // To hit the floor, use rules where the halved window would drop below 1h.
        var rules = SlaWindowRules.Default with
        {
            BaseWindowHours = new Dictionary<TicketPriority, int>
            {
                [TicketPriority.Critical] = 1
            }
        };

        var due = SlaCalculator.CalculateDueAt(TicketPriority.Critical, CustomerTier.Premium, Created, rules);

        Assert.Equal(Created.AddHours(1), due);
    }

    [Fact]
    public void CalculateDueAt_PremiumNeverShorterThanFloor_EvenIfMultiplierIsTiny()
    {
        var rules = SlaWindowRules.Default with { PremiumCustomerMultiplier = 0.01 };

        var due = SlaCalculator.CalculateDueAt(TicketPriority.Critical, CustomerTier.Premium, Created, rules);

        Assert.Equal(Created.AddHours(1), due);
    }
}