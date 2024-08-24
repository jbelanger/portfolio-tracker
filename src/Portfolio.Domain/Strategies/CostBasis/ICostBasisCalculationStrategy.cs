namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Defines a strategy for calculating the cost basis of an asset holding.
    /// This is used to determine the cost basis when disposing of an asset,
    /// which is essential for calculating capital gains or losses for tax purposes.
    /// </summary>
    public interface ICostBasisCalculationStrategy
    {
        /// <summary>
        /// Calculates the cost basis of an asset using a specific calculation strategy.
        /// </summary>
        /// <param name="holdings">The list of asset holdings, typically containing one holding per asset type.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The calculated cost basis for the disposed amount, which will be used to determine capital gains or losses.
        /// </returns>
        decimal CalculateCostBasis(IEnumerable<AssetHolding> holdings, FinancialTransaction tx);
    }
}
