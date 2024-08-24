using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Strategies.CostBasis
{
    /// <summary>
    /// Implements the Average Cost Basis (ACB) calculation strategy.
    /// This strategy calculates the cost basis by determining the average purchase price
    /// per unit of an asset and multiplying it by the quantity being disposed of.
    /// The ACB method is commonly used in tax reporting to calculate capital gains or losses.
    /// </summary>
    public class AcbCostBasisCalculationStrategy : ICostBasisCalculationStrategy
    {
        /// <summary>
        /// Calculates the cost basis of an asset using the Average Cost Basis (ACB) method.
        /// </summary>
        /// <param name="holding">The asset holding containing the purchase records for the asset.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The calculated cost basis for the disposed amount, based on the ACB strategy.
        /// </returns>
        public Result<decimal> CalculateCostBasis(AssetHolding holding, FinancialTransaction tx)
        {
            // Calculate the cost basis using the average purchase price per unit
            return holding.AverageBoughtPrice * tx.SentAmount.Amount;
        }
    }
}
