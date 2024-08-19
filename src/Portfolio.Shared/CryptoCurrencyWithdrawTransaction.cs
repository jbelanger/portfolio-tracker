using CSharpFunctionalExtensions;

namespace Portfolio.Shared
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>
    public class CryptoCurrencyWithdrawTransaction : ICryptoCurrencyTransaction
    {
        /// <summary>
        /// Gets or sets the date and time of the transaction.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the total amount received in the transaction without any fees.
        /// </summary>
        public Money Amount { get; set; } = null!;

        /// <summary>
        /// Gets or sets the fee amount for the transaction.
        /// </summary>
        public Money FeeAmount { get; set; } = null!;

        /// <summary>
        /// Gets or sets the account identifier associated with the transaction.
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a note or description associated with the transaction.
        /// </summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of transaction identifiers related to this transaction.
        /// </summary>
        public IEnumerable<string> TransactionIds { get; set; } = new List<string>();

        public object State { get; set; }

        /// <summary>
        /// Private constructor used internally for factory methods.
        /// </summary>
        private CryptoCurrencyWithdrawTransaction()
        { }

        /// <summary>
        /// Factory method to create a deposit transaction.
        /// </summary>
        /// <param name="date">The date and time of the deposit.</param>
        /// <param name="receivedAmount">The amount received in the deposit (without fees).</param>
        /// <param name="feeAmount">The transaction fee associated with the deposit.</param>
        /// <param name="account">The account identifier associated with the deposit.</param>
        /// <param name="transactionIds">The list of transaction identifiers.</param>
        /// <param name="note">An optional note associated with the deposit.</param>
        /// <returns>A new instance of <see cref="CryptoCurrencyTransaction"/> representing a deposit.</returns>
        public static Result<CryptoCurrencyWithdrawTransaction> Create(
            DateTime date,
            Money amount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (amount == null)
                return Result.Failure<CryptoCurrencyWithdrawTransaction>("Sent amount cannot be null for a withdrawal.");

            if (feeAmount == null)
                feeAmount = new Money(0, amount.CurrencyCode);
            else if (feeAmount.CurrencyCode != amount.CurrencyCode)
                return Result.Failure<CryptoCurrencyWithdrawTransaction>($"Fees are not in the same currency as the deposit currency.");


            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyWithdrawTransaction>("Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyWithdrawTransaction>("Transaction IDs cannot be null or empty.");

            var withdrawal = new CryptoCurrencyWithdrawTransaction()
            {
                DateTime = date,
                Amount = amount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };

            return withdrawal;
        }

        public CryptoCurrencyTransaction ToGenericTransaction()
        {
            return new CryptoCurrencyTransaction
            {
                DateTime = DateTime,
                Type = TransactionType.Withdrawal,
                ReceivedAmount = null,
                SentAmount = Amount,
                FeeAmount = FeeAmount,
                Account = Account,
                Note = Note,
                TransactionIds = TransactionIds
            };
        }
    }
}
