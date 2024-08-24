namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Implements the Last In, First Out (LIFO) cost basis calculation strategy.
    /// This strategy calculates the cost basis by assuming that the most recently acquired assets
    /// are sold or disposed of first. LIFO is commonly used in scenarios where the cost of newer purchases 
    /// is expected to be higher, affecting the calculation of capital gains or losses.
    /// </summary>
    public class LifoCostBasisCalculationStrategy : ICostBasisCalculationStrategy
    {
        /// <summary>
        /// Calculates the cost basis of an asset using the LIFO (Last In, First Out) method.
        /// </summary>
        /// <param name="holdings">The list of asset holdings, typically containing one holding per asset type.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The calculated cost basis for the disposed amount, based on the LIFO strategy.
        /// </returns>
        public decimal CalculateCostBasis(IEnumerable<AssetHolding> holdings, FinancialTransaction tx)
        {
            // Sort holdings by purchase date in descending order to simulate LIFO
            var sortedHoldings = holdings.OrderByDescending(h => h.PurchaseRecords.First().PurchaseDate).ToList();
            return CalculateCostFromHoldings(sortedHoldings, tx);
        }

        /// <summary>
        /// Helper method to calculate the total cost from the sorted list of holdings, using the LIFO strategy.
        /// </summary>
        /// <param name="sortedHoldings">The list of holdings sorted by purchase date in descending order.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The total calculated cost for the disposed amount, using the LIFO method.
        /// </returns>
        private decimal CalculateCostFromHoldings(List<AssetHolding> sortedHoldings, FinancialTransaction tx)
        {
            decimal totalCost = 0m;
            decimal amountToMatch = tx.SentAmount.Amount;

            // Iterate over the sorted holdings to calculate the cost of the disposed amount
            foreach (var holding in sortedHoldings)
            {
                foreach (var record in holding.PurchaseRecords.OrderByDescending(r => r.PurchaseDate))
                {
                    if (amountToMatch <= 0)
                        break;

                    // Determine how much of the current record can be matched with the disposed amount
                    var matchAmount = Math.Min(record.Amount, amountToMatch);
                    totalCost += matchAmount * record.PricePerUnit;
                    amountToMatch -= matchAmount;
                }
            }

            return totalCost;
        }
    }
}
