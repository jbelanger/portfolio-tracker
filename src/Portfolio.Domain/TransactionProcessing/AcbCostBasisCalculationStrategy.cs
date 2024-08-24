namespace Portfolio.Domain.Entities;

/// <summary>
/// Accurate according to https://www.canada.ca/en/revenue-agency/services/tax/individuals/topics/about-your-tax-return/tax-return/completing-a-tax-return/personal-income/line-12700-capital-gains/shares-funds-other-units/identical-properties/examples-calculating-average-cost-property.html
/// </summary>
public class AcbCostBasisCalculationStrategy : ICostBasisCalculationStrategy
{
    public decimal CalculateCostBasis(IEnumerable<CryptoCurrencyHolding> holdings, CryptoCurrencyRawTransaction tx)
    {
        var holding = holdings.First(); // Assuming one holding per asset type in the portfolio
        return holding.AverageBoughtPrice;
    }
}
