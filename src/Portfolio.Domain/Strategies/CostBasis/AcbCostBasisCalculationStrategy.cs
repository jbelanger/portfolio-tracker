namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Implements the Average Cost Basis (ACB) calculation strategy for determining the cost basis
    /// of an asset, which is used to calculate capital gains or losses for tax purposes.
    /// This strategy is accurate according to the guidelines provided by the Canada Revenue Agency (CRA)
    /// for calculating the average cost of identical properties.
    /// </summary>
    /// <remarks>
    /// The calculation is based on the ACB method, where the cost basis of the disposed asset 
    /// is calculated by multiplying the average purchase price per unit by the quantity being disposed of.
    /// See the following CRA guide for more details:
    /// https://www.canada.ca/en/revenue-agency/services/tax/individuals/topics/about-your-tax-return/tax-return/completing-a-tax-return/personal-income/line-12700-capital-gains/shares-funds-other-units/identical-properties/examples-calculating-average-cost-property.html
    /// </remarks>
    public class AcbCostBasisCalculationStrategy : ICostBasisCalculationStrategy
    {
        /// <summary>
        /// Calculates the cost basis of an asset using the Average Cost Basis (ACB) method.
        /// </summary>
        /// <param name="holdings">The list of asset holdings, typically containing one holding per asset type.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The calculated cost basis for the disposed amount, based on the average purchase price per unit.
        /// </returns>
        public decimal CalculateCostBasis(IEnumerable<AssetHolding> holdings, FinancialTransaction tx)
        {
            var holding = holdings.First(); // Assuming one holding per asset type in the portfolio
            return holding.AverageBoughtPrice * tx.SentAmount.Amount;
        }
    }
}
