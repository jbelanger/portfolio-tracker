using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>
    public class CryptoCurrencyRawTransaction : BaseAuditableEntity
    {
        public long WalletId { get; private set; }
        public DateTime DateTime { get; private set; }

        public TransactionType Type { get; init; }

        public Money ReceivedAmount { get; private set; } = Money.Empty;

        public Money SentAmount { get; private set; } = Money.Empty;

        public Money FeeAmount { get; private set; } = Money.Empty;

        public string Account { get; init; } = string.Empty;

        public string Note { get; private set; } = string.Empty;

        public IEnumerable<string>? TransactionIds { get; init; }

        public ErrorType ErrorType { get; set; } = ErrorType.None;

        public Money ValueInDefaultCurrency { get; set; } = Money.Empty;
        public Money FeeValueInDefaultCurrency { get; set; } = Money.Empty;


        /// <summary>
        /// When transaction is created from a csv import, this contains the csv lines that were used to build this transaction.
        /// </summary>
        public string CsvLinesJson { get; set; } = string.Empty;

        protected CryptoCurrencyRawTransaction()
        { }

        public static Result<CryptoCurrencyRawTransaction> CreateDeposit(
            DateTime date,
            Money receivedAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>($"Account cannot be null or whitespace.");

            var tx = new CryptoCurrencyRawTransaction()
            {
                Type = TransactionType.Deposit,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(tx)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(receivedAmount, Money.Empty, feeAmount ?? Money.Empty));
        }

        public static Result<CryptoCurrencyRawTransaction> CreateWithdraw(
            DateTime date,
            Money sentAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>($"Account cannot be null or whitespace.");

            var tx = new CryptoCurrencyRawTransaction()
            {
                Type = TransactionType.Withdrawal,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(tx)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(Money.Empty, sentAmount, feeAmount ?? Money.Empty));
        }

        public static Result<CryptoCurrencyRawTransaction> CreateTrade(
            DateTime date,
            Money receivedAmount,
            Money sentAmount,
            Money? feeAmount,
            string account,
            IEnumerable<string>? transactionIds,
            string note = "")
        {
            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>("Account cannot be null or whitespace.");

            var tx = new CryptoCurrencyRawTransaction()
            {
                Type = TransactionType.Trade,
                Account = account,
                TransactionIds = transactionIds
            };

            return Result.Success(tx)
                .Check(t => t.SetTransactionDate(date))
                .Check(t => t.SetNote(note))
                .Check(t => t.SetTransactionAmounts(receivedAmount, sentAmount, feeAmount ?? Money.Empty));

        }

        public Result SetTransactionAmounts(Money receivedAmount, Money sentAmount, Money feeAmount)
        {
            // var received = receivedAmount?.Amount ?? 0;
            // var sent = sentAmount?.Amount ?? 0;
            // var fee = feeAmount?.Amount ?? 0;
            receivedAmount = receivedAmount ?? Money.Empty;
            sentAmount = sentAmount ?? Money.Empty;
            feeAmount = feeAmount ?? Money.Empty;

            if ((receivedAmount == Money.Empty || receivedAmount.Amount <= 0) && (Type == TransactionType.Deposit || Type == TransactionType.Trade))
                return Result.Failure<CryptoCurrencyRawTransaction>($"Received amount must be greater than zero.");
            else if (receivedAmount.Amount > 0 && Type == TransactionType.Withdrawal)
                return Result.Failure($"Received amount can not be set on a 'withdrawal' transaction.");

            if ((sentAmount == Money.Empty || sentAmount.Amount <= 0) && (Type == TransactionType.Withdrawal || Type == TransactionType.Trade))
                return Result.Failure<CryptoCurrencyRawTransaction>($"Sent amount must be greater than zero.");
            else if (sentAmount.Amount > 0 && Type == TransactionType.Deposit)
                return Result.Failure($"Sent amount can not be set on a 'deposit' transaction.");

            // if (feeAmount != Money.Empty)
            // {
            //     if (Type == TransactionType.Deposit && receivedAmount.CurrencyCode != feeAmount.CurrencyCode)
            //         return Result.Failure($"Fees are not in the same currency as the deposit currency.");
            //     else if (Type == TransactionType.Withdrawal && sentAmount.CurrencyCode != feeAmount.CurrencyCode)
            //         return Result.Failure($"Fees are not in the same currency as the withdraw currency.");
            //     else if (feeAmount.CurrencyCode != receivedAmount.CurrencyCode && feeAmount.CurrencyCode != sentAmount.CurrencyCode)
            //         return Result.Failure<CryptoCurrencyRawTransaction>($"Fees must be in the same currency as the received or sent amounts.");
            // }

            ReceivedAmount = receivedAmount;
            SentAmount = sentAmount;
            FeeAmount = feeAmount;

            return Result.Success();
        }

        public Result SetTransactionDate(DateTime date)
        {
            if (date == null || date == DateTime.MinValue)
                return Result.Failure("Transaction date is invalid.");

            // This is important to ensure we always store the date up to the second only. 
            // This makes it safer for comparison later on...
            DateTime = date.TruncateToSecond();

            return Result.Success();
        }

        public Result SetNote(string note)
        {
            if (note?.Length > 500)
                return Result.Failure("Note cannot be longer than 500 characters.");
            Note = note ?? string.Empty;

            return Result.Success();
        }

        // public override bool Equals(object? obj)
        // {
        //     if (obj is not CryptoCurrencyRawTransaction other)
        //         return false;

        //     return DateTime == other.DateTime &&
        //            Type == other.Type &&
        //            Equals(ReceivedAmount, other.ReceivedAmount) &&
        //            Equals(SentAmount, other.SentAmount) &&
        //            Equals(FeeAmount, other.FeeAmount);
        // }

        // public override int GetHashCode()
        // {
        //     return HashCode.Combine(DateTime,
        //                             Type,
        //                             ReceivedAmount,
        //                             SentAmount,
        //                             FeeAmount);
        // }
    }
}
