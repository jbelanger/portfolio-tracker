using CSharpFunctionalExtensions;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>
    public class CryptoCurrencyTradeTransaction : CryptoCurrencyTransaction
    {
        /// <summary>
        /// Gets or sets the total amount sent in the transaction without any fees.
        /// </summary>
        public Money TradeAmount { get; set; } = null!;

        /// <summary>
        /// Private constructor used internally for factory methods.
        /// </summary>
        private CryptoCurrencyTradeTransaction()
        { }

        /// <summary>
        /// Factory method to create a trade transaction.
        /// </summary>
        /// <param name="date">The date and time of the trade.</param>
        /// <param name="receivedAmount">The amount received in the trade.</param>
        /// <param name="sentAmount">The amount sent in the trade.</param>
        /// <param name="feeAmount">The transaction fee associated with the trade.</param>
        /// <param name="account">The account identifier associated with the trade.</param>
        /// <param name="transactionIds">The list of transaction identifiers.</param>
        /// <param name="note">An optional note associated with the trade.</param>
        /// <returns>A new instance of <see cref="CryptoCurrencyTradeTransaction"/> representing a trade.</returns>
        public static Result<CryptoCurrencyTradeTransaction> Create(
            DateTime date,
            Money receivedAmount,
            Money sentAmount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (receivedAmount == null)
                return Result.Failure<CryptoCurrencyTradeTransaction>("Received amount cannot be null for a trade transaction.");

            if (sentAmount == null)
                return Result.Failure<CryptoCurrencyTradeTransaction>("Sent amount cannot be null for a trade transaction.");

            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyTradeTransaction>("Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyTradeTransaction>("Transaction IDs cannot be null or empty.");

            if (feeAmount == null)
                feeAmount = new Money(0, receivedAmount.CurrencyCode);

            var trade = new CryptoCurrencyTradeTransaction()
            {
                DateTime = date,
                Amount = receivedAmount.ToAbsoluteAmountMoney(),
                TradeAmount = sentAmount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };
            return trade;
        }
    }
}
