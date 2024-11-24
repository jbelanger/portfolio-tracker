using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Strategies.Transactions;

namespace Portfolio.Domain.Services
{
    /// <summary>
    /// Processes financial transactions by applying the appropriate transaction strategy 
    /// based on the type of transaction (Deposit, Withdrawal, or Trade).
    /// This class acts as a coordinator that delegates the processing of each transaction
    /// to the correct strategy.
    /// </summary>
    public class TransactionProcessor
    {
        /// <summary>
        /// A dictionary mapping transaction types to their corresponding processing strategies.
        /// </summary>
        private readonly Dictionary<TransactionType, ITransactionStrategy> _transactionStrategies;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionProcessor"/> class,
        /// setting up the available strategies for processing transactions.
        /// </summary>
        public TransactionProcessor()
        {
            _transactionStrategies = new Dictionary<TransactionType, ITransactionStrategy>
            {
                { TransactionType.Deposit, new DepositTransactionStrategy() },
                { TransactionType.Withdrawal, new WithdrawalTransactionStrategy() },
                { TransactionType.Trade, new TradeTransactionStrategy() }
            };
        }

        /// <summary>
        /// Processes a collection of financial transactions, applying the appropriate strategy
        /// for each transaction based on its type.
        /// </summary>
        /// <param name="transactions">The collection of financial transactions to be processed.</param>
        /// <param name="portfolio">The user's portfolio to which the transactions will be applied.</param>
        /// <param name="priceHistoryService">A service for retrieving historical price data, used during transaction processing.</param>
        /// <returns>
        /// A Result indicating the success or failure of the transaction processing.
        /// The processing stops on the first encountered failure.
        /// </returns>
        public async Task<Result> ProcessTransactionsAsync(
            IEnumerable<FinancialTransaction> transactions,
            UserPortfolio portfolio,
            IPriceHistoryService priceHistoryService)
        {
            foreach (var tx in transactions)
            {
                if (_transactionStrategies.TryGetValue(tx.Type, out var strategy))
                {
                    var result = await strategy.ProcessTransactionAsync(tx, portfolio, priceHistoryService);
                    if (result.IsFailure)
                    {
                        return result; // Optionally stop processing on failure
                    }
                }
                else
                {
                    return Result.Failure($"No strategy found for transaction type: {tx.Type}");
                }
            }

            return Result.Success();
        }
    }
}
