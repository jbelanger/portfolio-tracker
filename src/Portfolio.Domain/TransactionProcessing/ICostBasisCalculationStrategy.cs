using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities;

public interface ICostBasisCalculationStrategy
{
    decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx);
}

/// <summary>
/// Not accurate, but way lower capital gains...
/// </summary>
public class AcbCostBasisCalculationStrategy2 : ICostBasisCalculationStrategy
{
    public decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx)
    {
        var holding = holdings.First(); // Assuming one holding per asset type in the portfolio

        // Use the new GetRelevantAmount method to determine the amount
        var relevantAmount = tx.GetRelevantTransactionAmount();

        // Calculate the cost basis using the average price
        return holding.PurchaseRecords.Average(p => p.PricePerUnit) * relevantAmount.Amount;
    }
}

/// <summary>
/// Accurate according to https://www.canada.ca/en/revenue-agency/services/tax/individuals/topics/about-your-tax-return/tax-return/completing-a-tax-return/personal-income/line-12700-capital-gains/shares-funds-other-units/identical-properties/examples-calculating-average-cost-property.html
/// </summary>
public class AcbCostBasisCalculationStrategy : ICostBasisCalculationStrategy
{
    public decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx)
    {
        var holding = holdings.First(); // Assuming one holding per asset type in the portfolio

        // Calculate the ACB (Adjusted Cost Base)
        decimal totalCost = holding.PurchaseRecords.Sum(p => p.PricePerUnit * p.Amount);
        decimal totalUnits = holding.PurchaseRecords.Sum(p => p.Amount);

        decimal acbPerUnit = totalUnits > 0 ? totalCost / totalUnits : 0;

        // Use the new GetRelevantAmount method to determine the amount being disposed
        var relevantAmount = tx.GetRelevantTransactionAmount();

        // Calculate the cost basis for the amount being disposed
        return acbPerUnit * relevantAmount.Amount;
    }
}



public class FifoCostBasisCalculationStrategy : ICostBasisCalculationStrategy
{
    public decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx)
    {
        var sortedHoldings = holdings.OrderBy(h => h.PurchaseRecords.First().PurchaseDate).ToList();
        return CalculateCostFromHoldings(sortedHoldings, tx);
    }

    private decimal CalculateCostFromHoldings(List<CryptoCurrencyHolding> sortedHoldings, CryptoCurrencyRawTransaction tx)
    {
        decimal totalCost = 0m;
        decimal amountToMatch = tx.SentAmount.Amount;

        foreach (var holding in sortedHoldings)
        {
            foreach (var record in holding.PurchaseRecords.OrderBy(r => r.PurchaseDate))
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
