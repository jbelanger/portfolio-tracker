namespace Portfolio.Domain.Entities;

public interface ICostBasisCalculationStrategy
{
    decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx);
}
