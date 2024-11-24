using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a financial transaction, which can be a trade, deposit, or withdrawal.
    /// This class can be used for various financial assets, including cryptocurrencies, stocks, bonds, and fiat currencies.
    /// </summary>
    public class FinancialTransaction : BaseAuditableEntity
    {
        /// <summary>
        /// Gets the ID of the wallet or account associated with this transaction.
        /// </summary>
        public long WalletId { get; private set; }

        /// <summary>
        /// Gets the date and time when the transaction occurred.
        /// </summary>
        public DateTime DateTime { get; private set; }

        /// <summary>
        /// Gets the type of the transaction (e.g., Trade, Deposit, Withdrawal).
        /// </summary>
        public TransactionType Type { get; init; }

        /// <summary>
        /// Gets the amount of asset received in the transaction.
        /// This applies to deposits and trades.
        /// </summary>
        public Money ReceivedAmount { get; private set; } = Money.Empty;

        /// <summary>
        /// Gets the amount of asset sent in the transaction.
        /// This applies to withdrawals and trades.
        /// </summary>
        public Money SentAmount { get; private set; } = Money.Empty;

        /// <summary>
        /// Gets the fee amount associated with the transaction, if any.
        /// </summary>
        public Money FeeAmount { get; private set; } = Money.Empty;

        /// <summary>
        /// Gets the account identifier associated with the transaction.
        /// This typically represents the financial account or wallet involved.
        /// </summary>
        public string Account { get; init; } = string.Empty;

        /// <summary>
        /// Gets any additional notes or descriptions associated with the transaction.
        /// </summary>
        public string Note { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the unique identifiers related to this transaction, such as IDs from an exchange or broker.
        /// </summary>
        public IEnumerable<string>? TransactionIds { get; init; }

        /// <summary>
        /// Gets or sets the error type associated with the transaction, if any.
        /// </summary>
        public ErrorType ErrorType { get; set; } = ErrorType.None;

        /// <summary>
        /// Gets or sets the error message associated with the transaction, if any.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the transaction in the portfolio's base currency.
        /// </summary>
        public Money ValueInDefaultCurrency { get; set; } = Money.Empty;

        /// <summary>
        /// Gets or sets the value of the transaction fees in the portfolio's base currency.
        /// </summary>
        public Money FeeValueInDefaultCurrency { get; set; } = Money.Empty;

        /// <summary>
        /// When the transaction is created from a CSV import, this contains the CSV lines that were used to build this transaction.
        /// </summary>
        public string CsvLinesJson { get; set; } = string.Empty;

        /// <summary>
        /// Protected constructor to enforce the use of factory methods for creating transactions.
        /// </summary>
        internal FinancialTransaction() { }

        /// <summary>
        /// Factory method to create a deposit transaction.
        /// </summary>
        /// <param name="date">The date and time of the deposit.</param>
        /// <param name="receivedAmount">The amount of asset received in the deposit.</param>
        /// <param name="feeAmount">The transaction fee associated with the deposit, if any.</param>
        /// <param name="account">The account identifier associated with the deposit.</param>
        /// <param name="transactionIds">The unique identifiers for the transaction.</param>
        /// <param name="note">Any additional notes associated with the deposit.</param>
        /// <returns>A Result object containing the newly created FinancialTransaction instance.</returns>
        public static Result<FinancialTransaction> CreateDeposit(
            DateTime date,
            Money receivedAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<FinancialTransaction>($"Account cannot be null or whitespace.");

            var transaction = new FinancialTransaction()
            {
                Type = TransactionType.Deposit,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(transaction)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(receivedAmount, Money.Empty, feeAmount ?? Money.Empty));
        }

        /// <summary>
        /// Factory method to create a withdrawal transaction.
        /// </summary>
        /// <param name="date">The date and time of the withdrawal.</param>
        /// <param name="sentAmount">The amount of asset sent in the withdrawal.</param>
        /// <param name="feeAmount">The transaction fee associated with the withdrawal, if any.</param>
        /// <param name="account">The account identifier associated with the withdrawal.</param>
        /// <param name="transactionIds">The unique identifiers for the transaction.</param>
        /// <param name="note">Any additional notes associated with the withdrawal.</param>
        /// <returns>A Result object containing the newly created FinancialTransaction instance.</returns>
        public static Result<FinancialTransaction> CreateWithdraw(
            DateTime date,
            Money sentAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<FinancialTransaction>($"Account cannot be null or whitespace.");

            var transaction = new FinancialTransaction()
            {
                Type = TransactionType.Withdrawal,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(transaction)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(Money.Empty, sentAmount, feeAmount ?? Money.Empty));
        }

        /// <summary>
        /// Factory method to create a trade transaction.
        /// </summary>
        /// <param name="date">The date and time of the trade.</param>
        /// <param name="receivedAmount">The amount of asset received in the trade.</param>
        /// <param name="sentAmount">The amount of asset sent in the trade.</param>
        /// <param name="feeAmount">The transaction fee associated with the trade, if any.</param>
        /// <param name="account">The account identifier associated with the trade.</param>
        /// <param name="transactionIds">The unique identifiers for the transaction.</param>
        /// <param name="note">Any additional notes associated with the trade.</param>
        /// <returns>A Result object containing the newly created FinancialTransaction instance.</returns>
        public static Result<FinancialTransaction> CreateTrade(
            DateTime date,
            Money receivedAmount,
            Money sentAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<FinancialTransaction>("Account cannot be null or whitespace.");

            var transaction = new FinancialTransaction()
            {
                Type = TransactionType.Trade,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(transaction)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(receivedAmount, sentAmount, feeAmount ?? Money.Empty));
        }

        /// <summary>
        /// Sets the transaction amounts for received, sent, and fee.
        /// Ensures that amounts are valid based on the transaction type.
        /// </summary>
        /// <param name="receivedAmount">The amount of asset received.</param>
        /// <param name="sentAmount">The amount of asset sent.</param>
        /// <param name="feeAmount">The transaction fee amount.</param>
        /// <returns>A Result indicating success or failure.</returns>
        public Result SetTransactionAmounts(Money receivedAmount, Money sentAmount, Money feeAmount)
        {
            receivedAmount = receivedAmount ?? Money.Empty;
            sentAmount = sentAmount ?? Money.Empty;
            feeAmount = feeAmount ?? Money.Empty;

            if ((receivedAmount == Money.Empty || receivedAmount.Amount <= 0) && (Type == TransactionType.Deposit || Type == TransactionType.Trade))
                return Result.Failure<FinancialTransaction>($"Received amount must be greater than zero.");
            else if (receivedAmount.Amount > 0 && Type == TransactionType.Withdrawal)
                return Result.Failure($"Received amount cannot be set on a 'withdrawal' transaction.");

            if ((sentAmount == Money.Empty || sentAmount.Amount <= 0) && (Type == TransactionType.Withdrawal || Type == TransactionType.Trade))
                return Result.Failure<FinancialTransaction>($"Sent amount must be greater than zero.");
            else if (sentAmount.Amount > 0 && Type == TransactionType.Deposit)
                return Result.Failure($"Sent amount cannot be set on a 'deposit' transaction.");

            ReceivedAmount = receivedAmount;
            SentAmount = sentAmount;
            FeeAmount = feeAmount;

            return Result.Success();
        }

        /// <summary>
        /// Sets the date and time of the transaction.
        /// </summary>
        /// <param name="date">The date and time of the transaction.</param>
        /// <returns>A Result indicating success or failure.</returns>
        public Result SetTransactionDate(DateTime date)
        {
            if (date == null || date == DateTime.MinValue)
                return Result.Failure("Transaction date is invalid.");

            // This ensures the date is stored up to the second only, making it safer for comparison.
            DateTime = date.TruncateToSecond();

            return Result.Success();
        }

        /// <summary>
        /// Sets the note or description for the transaction.
        /// </summary>
        /// <param name="note">The note or description.</param>
        /// <returns>A Result indicating success or failure.</returns>
        public Result SetNote(string note)
        {
            if (note?.Length > 500)
                return Result.Failure("Note cannot be longer than 500 characters.");
            Note = note ?? string.Empty;

            return Result.Success();
        }

        /// <summary>
        /// Gets the relevant transaction amount based on the transaction type.
        /// For a deposit, this is the received amount. For a withdrawal or trade, this is the sent amount.
        /// </summary>
        /// <returns>The relevant transaction amount as a Money object.</returns>
        public Money GetRelevantTransactionAmount()
        {
            return Type switch
            {
                TransactionType.Deposit => ReceivedAmount,
                TransactionType.Withdrawal => SentAmount,
                TransactionType.Trade => SentAmount, // Typically, SentAmount is used for trade cost basis
                _ => throw new NotSupportedException($"Transaction type {Type} is not supported.")
            };
        }
    }
}
