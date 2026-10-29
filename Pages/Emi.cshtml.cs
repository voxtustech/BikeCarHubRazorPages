using BikeCarHub.Services;
using BikeCarHub.Web;

namespace BikeCarHub.Pages;

public class EmiModel : BasePageModel
{
    public decimal Amount { get; private set; } = 100_000;
    public decimal Rate { get; private set; } = 10;
    public int Years { get; private set; } = 3;
    public decimal Fee { get; private set; }
    public EmiResult Result { get; private set; } = null!;
    public decimal FeeAmount { get; private set; }
    public IReadOnlyList<EmiYear> Schedule { get; private set; } = [];
    public bool OutOfRange { get; private set; }

    /// <summary>Share of the total repayment that is interest, as a whole percentage.</summary>
    public int InterestShare => Result.TotalPayable == 0 ? 0 : (int)Math.Round(Result.TotalInterest * 100 / Result.TotalPayable);

    public void OnGet(decimal? amount, decimal? rate, int? years, decimal? fee)
    {
        var requested = (amount ?? Amount, rate ?? Rate, years ?? Years, fee ?? Fee);
        if (EmiCalculator.IsValid(requested.Item1, requested.Item2, requested.Item3) && EmiCalculator.IsValidFee(requested.Item4))
            (Amount, Rate, Years, Fee) = requested;
        else OutOfRange = true;
        Result = EmiCalculator.Calculate(Amount, Rate, Years);
        FeeAmount = EmiCalculator.ProcessingFee(Amount, Fee);
        Schedule = EmiCalculator.Schedule(Amount, Rate, Years);

        Meta.Title = "Bike Loan EMI Calculator – Monthly EMI and Interest | BikeCarHub";
        Meta.Description = "Work out the monthly EMI, total interest, processing fee and year-by-year repayment on a bike or car loan from ₹50,000 to ₹1 crore.";
        Meta.CanonicalPath = "/emi-calculator";
        Meta.NoIndex = Request.Query.Count > 0;
        Meta.Crumbs(("EMI calculator", "/emi-calculator"));
    }
}
