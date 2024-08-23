namespace Portfolio.Domain.Entities;

public interface ITaxCalculationStrategy
{
    TaxableEvent CalculateTax(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding holding, decimal currentPrice, string defaultCurrency, ICostBasisCalculationStrategy costBasisCalculationStrategy);
}

public class GenericTaxCalculationStrategy : ITaxCalculationStrategy
{
    public TaxableEvent CalculateTax(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding holding, decimal currentPrice, string defaultCurrency, ICostBasisCalculationStrategy costBasisCalculationStrategy)
    {
        // Use the new GetTaxableAmount method to determine the amount
        var relevantAmount = tx.GetRelevantTransactionAmount();

        // Calculate cost basis using the provided strategy (e.g., ACB, FIFO, LIFO)
        decimal costBasis = costBasisCalculationStrategy.CalculateCostBasis(new[] { holding }, tx);

        // Calculate the gain or loss
        decimal gain = (currentPrice * relevantAmount.Amount) - costBasis;

        // Create and return the taxable event with the gain and units disposed
        return TaxableEvent.Create(tx.DateTime, holding.Asset, costBasis / relevantAmount.Amount, currentPrice, relevantAmount.Amount, gain, defaultCurrency).Value;
    }
}
