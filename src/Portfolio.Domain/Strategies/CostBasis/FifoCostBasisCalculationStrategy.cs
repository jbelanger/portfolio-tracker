using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Strategies.CostBasis
{
    /// <summary>
    /// Implements the First In, First Out (FIFO) cost basis calculation strategy.
    /// This strategy calculates the cost basis by assuming that the oldest (first acquired) assets
    /// are sold or disposed of first.
    /// </summary>
    public class FifoCostBasisCalculationStrategy : ICostBasisCalculationStrategy
    {
        /// <summary>
        /// Calculates the cost basis of an asset using the FIFO (First In, First Out) method.
        /// </summary>
        /// <param name="holding">The asset holding containing the purchase records for the asset.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The calculated cost basis for the disposed amount, based on the FIFO strategy.
        /// </returns>
        public Result<decimal> CalculateCostBasis(AssetHolding holding, FinancialTransaction tx)
        {
            // Sort the purchase records by purchase date in ascending order to simulate FIFO
            var sortedRecords = holding.PurchaseRecords.OrderBy(r => r.PurchaseDate).ToList();
            return CalculateCostFromRecords(sortedRecords, tx);
        }

        /// <summary>
        /// Helper method to calculate the total cost from the sorted list of purchase records, using the FIFO strategy.
        /// </summary>
        /// <param name="sortedRecords">The list of purchase records sorted by purchase date in ascending order.</param>
        /// <param name="tx">The financial transaction that disposes of the asset.</param>
        /// <returns>
        /// The total calculated cost for the disposed amount, using the FIFO method.
        /// </returns>
        private Result<decimal> CalculateCostFromRecords(List<PurchaseRecord> sortedRecords, FinancialTransaction tx)        
        {
            decimal totalCost = 0m;
            decimal amountToMatch = tx.SentAmount.Amount;

            // Iterate over the sorted purchase records to calculate the cost of the disposed amount
            foreach (var record in sortedRecords)
            {
                if (amountToMatch <= 0)
                    break;

                // Determine how much of the current record can be matched with the disposed amount
                var matchAmount = Math.Min(record.Amount, amountToMatch);
                totalCost += matchAmount * record.PricePerUnit;
                amountToMatch -= matchAmount;
            }

            // If there's still an unmatched amount, log an error
            if (amountToMatch > 0)
            {
                tx.ErrorType = ErrorType.InsufficientFunds;
                tx.ErrorMessage = $"Insufficient holdings to match the transaction amount. Unable to match {amountToMatch} {tx.SentAmount.CurrencyCode}.";
                return Result.Failure<decimal>($"Insufficient holdings to match the transaction amount. Unable to match {amountToMatch} {tx.SentAmount.CurrencyCode}.");
            }

            return totalCost;
        }
    }
}
