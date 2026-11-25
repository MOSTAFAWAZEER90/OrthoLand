using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;

namespace OrthoFlowAI.Application.Billing;

public sealed record PlannedInstallment(int Number, DateTime DueDate, decimal Amount);

/// <summary>
/// Pure instalment arithmetic (no database), so it is easy to test.
/// </summary>
public static class InstallmentPlanner
{
    public const int MaxInstallments = 60;

    /// <summary>
    /// Splits <paramref name="amount"/> into <paramref name="count"/> payments.
    /// Every instalment is rounded down to <paramref name="roundTo"/> (whole pounds
    /// by default, as clinics quote round numbers) and the last one absorbs the
    /// remainder, so the schedule always adds up exactly to the amount.
    /// </summary>
    public static IReadOnlyList<PlannedInstallment> Plan(
        decimal amount, int count, DateTime firstDueDate, int intervalMonths = 1, decimal roundTo = 1m)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        if (count < 1 || count > MaxInstallments)
            throw new ArgumentOutOfRangeException(nameof(count), $"Count must be between 1 and {MaxInstallments}.");
        if (intervalMonths < 1) throw new ArgumentOutOfRangeException(nameof(intervalMonths));
        if (roundTo <= 0) throw new ArgumentOutOfRangeException(nameof(roundTo));

        var regular = Math.Floor(amount / count / roundTo) * roundTo;
        // Tiny amounts: fall back to cent precision so no instalment is zero.
        if (regular <= 0) regular = Math.Floor(amount / count * 100m) / 100m;

        var result = new List<PlannedInstallment>(count);
        for (var i = 0; i < count; i++)
        {
            var isLast = i == count - 1;
            var value = isLast ? amount - regular * (count - 1) : regular;
            result.Add(new PlannedInstallment(i + 1, firstDueDate.Date.AddMonths(i * intervalMonths), value));
        }
        return result;
    }

    /// <summary>
    /// Allocates a payment to the oldest unpaid instalments first.
    /// Returns the part of the payment that did not fit (overpayment), normally 0.
    /// </summary>
    public static decimal ApplyPayment(IEnumerable<Installment> installments, decimal amount, DateTime paidAt)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var left = amount;
        foreach (var inst in installments.Where(i => i.Status != InstallmentStatus.Paid).OrderBy(i => i.Number))
        {
            if (left <= 0) break;

            var take = Math.Min(left, inst.Amount - inst.PaidAmount);
            inst.PaidAmount += take;
            left -= take;

            if (inst.PaidAmount >= inst.Amount)
            {
                inst.Status = InstallmentStatus.Paid;
                inst.PaidAt = paidAt;
            }
            else
            {
                inst.Status = InstallmentStatus.PartiallyPaid;
            }
        }
        return left;
    }

    /// <summary>Marks unpaid instalments whose due date has passed as overdue.</summary>
    public static int MarkOverdue(IEnumerable<Installment> installments, DateTime today)
    {
        var changed = 0;
        foreach (var inst in installments)
        {
            if (inst.Status is InstallmentStatus.Pending or InstallmentStatus.PartiallyPaid
                && inst.DueDate.Date < today.Date)
            {
                inst.Status = InstallmentStatus.Overdue;
                changed++;
            }
        }
        return changed;
    }
}
