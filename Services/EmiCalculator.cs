namespace BikeCarHub.Services;

public sealed record EmiResult(decimal Principal, decimal MonthlyEmi, decimal TotalInterest, decimal TotalPayable);

/// <summary>What one year of repayments does to the loan.</summary>
public sealed record EmiYear(int Year, decimal PrincipalPaid, decimal InterestPaid, decimal Balance);

public static class EmiCalculator
{
    public const int MinPrincipal = 50_000;
    public const int MaxPrincipal = 10_000_000;
    public const decimal MinRate = 1m;
    public const decimal MaxRate = 18m;
    public const int MinYears = 1;
    public const int MaxYears = 7;
    public const decimal MaxFee = 5m;

    public static bool IsValid(decimal principal, decimal annualRatePercent, int years) =>
        principal is >= MinPrincipal and <= MaxPrincipal && annualRatePercent is >= MinRate and <= MaxRate && years is >= MinYears and <= MaxYears;

    public static bool IsValidFee(decimal feePercent) => feePercent is >= 0 and <= MaxFee;

    /// <summary>Standard reducing-balance EMI, rounded to whole rupees.</summary>
    public static EmiResult Calculate(decimal principal, decimal annualRatePercent, int years)
    {
        var months = years * 12;
        var emi = MonthlyEmi(principal, annualRatePercent, months);
        var total = emi * months;
        return new EmiResult(principal, Math.Round((decimal)emi), Math.Round((decimal)total - principal), Math.Round((decimal)total));
    }

    /// <summary>One-time fee charged by the lender, as a percentage of the loan.</summary>
    public static decimal ProcessingFee(decimal principal, decimal feePercent) => Math.Round(principal * feePercent / 100);

    /// <summary>Year-by-year split of the same instalments into principal and interest.</summary>
    public static IReadOnlyList<EmiYear> Schedule(decimal principal, decimal annualRatePercent, int years)
    {
        var monthlyRate = (double)annualRatePercent / 1200d;
        var emi = MonthlyEmi(principal, annualRatePercent, years * 12);
        var balance = (double)principal;
        var rows = new List<EmiYear>(years);
        for (var year = 1; year <= years; year++)
        {
            double interest = 0, repaid = 0;
            for (var month = 0; month < 12; month++)
            {
                var due = balance * monthlyRate;
                interest += due;
                repaid += emi - due;
                balance -= emi - due;
            }
            rows.Add(new EmiYear(year, Math.Round((decimal)repaid), Math.Round((decimal)interest), Math.Max(0, Math.Round((decimal)balance))));
        }
        return rows;
    }

    private static double MonthlyEmi(decimal principal, decimal annualRatePercent, int months)
    {
        var monthlyRate = (double)annualRatePercent / 1200d;
        var growth = Math.Pow(1 + monthlyRate, months);
        return (double)principal * monthlyRate * growth / (growth - 1);
    }
}
