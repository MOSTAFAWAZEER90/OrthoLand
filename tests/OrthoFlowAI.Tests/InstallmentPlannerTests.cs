using OrthoFlowAI.Application.Billing;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Tests;

public class InstallmentPlannerTests
{
    private static readonly DateTime First = new(2026, 12, 25);

    [Fact]
    public void Splits_into_round_pounds_and_last_takes_the_remainder()
    {
        var plan = InstallmentPlanner.Plan(25_000m, 12, First);

        Assert.Equal(12, plan.Count);
        Assert.All(plan.Take(11), p => Assert.Equal(2_083m, p.Amount));
        Assert.Equal(2_087m, plan[^1].Amount);
        Assert.Equal(25_000m, plan.Sum(p => p.Amount));
    }

    [Fact]
    public void Due_dates_are_monthly_from_the_first_date()
    {
        var plan = InstallmentPlanner.Plan(1_200m, 3, First, intervalMonths: 2);

        Assert.Equal(new[] { First, First.AddMonths(2), First.AddMonths(4) }, plan.Select(p => p.DueDate));
        Assert.Equal(new[] { 1, 2, 3 }, plan.Select(p => p.Number));
    }

    [Fact]
    public void Tiny_amounts_never_produce_zero_installments()
    {
        var plan = InstallmentPlanner.Plan(5m, 12, First);

        Assert.All(plan, p => Assert.True(p.Amount > 0));
        Assert.Equal(5m, plan.Sum(p => p.Amount));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-10, 12)]
    [InlineData(1000, 0)]
    [InlineData(1000, 61)]
    public void Invalid_input_is_rejected(decimal amount, int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InstallmentPlanner.Plan(amount, count, First));
    }

    private static List<Installment> Schedule(params decimal[] amounts) =>
        amounts.Select((a, i) => new Installment { Number = i + 1, Amount = a, DueDate = First.AddMonths(i) }).ToList();

    [Fact]
    public void Payment_settles_oldest_installments_first()
    {
        var items = Schedule(1_875m, 1_875m, 1_875m);
        var paidAt = new DateTime(2026, 12, 1);

        var leftover = InstallmentPlanner.ApplyPayment(items, 2_500m, paidAt);

        Assert.Equal(0m, leftover);
        Assert.Equal(InstallmentStatus.Paid, items[0].Status);
        Assert.Equal(paidAt, items[0].PaidAt);
        Assert.Equal(625m, items[1].PaidAmount);
        Assert.Equal(InstallmentStatus.PartiallyPaid, items[1].Status);
        Assert.Equal(InstallmentStatus.Pending, items[2].Status);
    }

    [Fact]
    public void Overpayment_is_returned_as_leftover()
    {
        var items = Schedule(100m, 100m);

        var leftover = InstallmentPlanner.ApplyPayment(items, 250m, First);

        Assert.Equal(50m, leftover);
        Assert.All(items, i => Assert.Equal(InstallmentStatus.Paid, i.Status));
    }

    [Fact]
    public void Overdue_marks_only_unpaid_past_installments()
    {
        var items = Schedule(100m, 100m, 100m);
        items[0].Status = InstallmentStatus.Paid;

        var changed = InstallmentPlanner.MarkOverdue(items, today: First.AddMonths(1).AddDays(1));

        Assert.Equal(1, changed); // #2 is late; #1 is paid; #3 is in the future
        Assert.Equal(InstallmentStatus.Paid, items[0].Status);
        Assert.Equal(InstallmentStatus.Overdue, items[1].Status);
        Assert.Equal(InstallmentStatus.Pending, items[2].Status);
    }
}
