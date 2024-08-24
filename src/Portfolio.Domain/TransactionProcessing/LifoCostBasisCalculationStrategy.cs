namespace Portfolio.Domain.Entities;

public class LifoCostBasisCalculationStrategy : ICostBasisCalculationStrategy
{
    public decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx)
    {
        var sortedHoldings = holdings.OrderByDescending(h => h.PurchaseRecords.First().PurchaseDate).ToList();
        return CalculateCostFromHoldings(sortedHoldings, tx);
    }

    private decimal CalculateCostFromHoldings(List<CryptoCurrencyHolding> sortedHoldings, CryptoCurrencyRawTransaction tx)
    {
        decimal totalCost = 0m;
        decimal amountToMatch = tx.SentAmount.Amount;

        foreach (var holding in sortedHoldings)
        {
            foreach (var record in holding.PurchaseRecords.OrderByDescending(r => r.PurchaseDate))
            {
                if (amountToMatch <= 0)
                    break;

                var matchAmount = Math.Min(record.Amount, amountToMatch);
                totalCost += matchAmount * record.PricePerUnit;
                amountToMatch -= matchAmount;
            }
        }

        return totalCost;
    }
}