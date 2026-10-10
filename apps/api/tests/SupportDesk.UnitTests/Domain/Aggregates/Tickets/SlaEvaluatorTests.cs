using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaEvaluatorTests
{
    private static readonly DateTime Now = FixedClock.DefaultNow;

    [Fact]
    public void Evaluate_WithoutADueDate_IsNotApplicable() =>
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(null, null, Now));

    [Fact]
    public void Evaluate_ResolvedOnTime_IsMet() =>
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(Now, Now, Now.AddDays(1)));

    [Fact]
    public void Evaluate_ResolvedLate_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now, Now.AddMinutes(1), Now));

    [Fact]
    public void Evaluate_OpenAndPastDue_IsBreached() =>
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(Now.AddMinutes(-1), null, Now));

    [Fact]
    public void Evaluate_OpenAndBeforeDue_IsWithinSla() =>
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(Now.AddHours(1), null, Now));



      private static readonly DateTime Created = Now;

    [Fact]
    public void Evaluate_AtRiskThreshold_ResolvedOnTime_IsMet()
    {
        var due = Created.AddHours(4);

        var status = SlaEvaluator.Evaluate(due, Created.AddHours(2), Created, Created.AddHours(2), 25);

        Assert.Equal(SlaStatus.Met, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_ResolvedLate_IsBreached()
    {
        var due = Created.AddHours(4);

        var status = SlaEvaluator.Evaluate(due, Created.AddHours(5), Created, Created.AddHours(5), 25);

        Assert.Equal(SlaStatus.Breached, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_OpenAndPastDue_IsBreached()
    {
        var due = Created.AddHours(4);

        var status = SlaEvaluator.Evaluate(due, null, Created, Created.AddHours(5), 25);

        Assert.Equal(SlaStatus.Breached, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_ExactlyAt25PercentRemaining_IsAtRisk()
    {
        // Window 4h. 25% remaining is exactly 1h left. Boundary: at risk.
        var due = Created.AddHours(4);
        var now = Created.AddHours(3);

        var status = SlaEvaluator.Evaluate(due, null, Created, now, 25);

        Assert.Equal(SlaStatus.AtRisk, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_JustAbove25Percent_IsWithinSla()
    {
        // Window 4h. 1 minute more than 1h remaining → 25.4% → within SLA.
        var due = Created.AddHours(4);
        var now = Created.AddHours(3).AddMinutes(-1);

        var status = SlaEvaluator.Evaluate(due, null, Created, now, 25);

        Assert.Equal(SlaStatus.WithinSla, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_JustBelow25Percent_IsAtRisk()
    {
        // Window 4h. 1 minute less than 1h remaining → 24.6% → at risk.
        var due = Created.AddHours(4);
        var now = Created.AddHours(3).AddMinutes(1);

        var status = SlaEvaluator.Evaluate(due, null, Created, now, 25);

        Assert.Equal(SlaStatus.AtRisk, status);
    }

    [Fact]
    public void Evaluate_AtRiskThreshold_Zero_DisablesAtRisk()
    {
        // With threshold 0, an open unbreached ticket is always WithinSla.
        var due = Created.AddHours(4);
        var now = Created.AddHours(3).AddMinutes(59);

        var status = SlaEvaluator.Evaluate(due, null, Created, now, 0);

        Assert.Equal(SlaStatus.WithinSla, status);
    }

   
}
