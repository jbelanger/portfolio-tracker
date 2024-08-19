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
        public DateTime DateTime { get; init; }

        public TransactionType Type { get; init; }

        public Money? ReceivedAmount { get; private set; }

        public Money? SentAmount { get; private set; }

        public Money? FeeAmount { get; private set; }

        public string Account { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public IEnumerable<string> TransactionIds { get; init; } = new List<string>();        

        protected CryptoCurrencyRawTransaction()
        { }

        public static Result<CryptoCurrencyRawTransaction> CreateDeposit(
            DateTime date,
            Money receivedAmount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (receivedAmount == null)
                return Result.Failure<CryptoCurrencyRawTransaction>($"Received amount cannot be null for a deposit.");

            if (feeAmount == null)
                feeAmount = new Money(0, receivedAmount.CurrencyCode);
            else if (feeAmount.CurrencyCode != receivedAmount.CurrencyCode)
                return Result.Failure<CryptoCurrencyRawTransaction>($"Fees are not in the same currency as the deposit currency.");

            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>($"Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyRawTransaction>($"Transaction IDs cannot be null or empty.");

            return new CryptoCurrencyRawTransaction()
            {
                DateTime = date,
                Type = TransactionType.Deposit,
                ReceivedAmount = receivedAmount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };
        }

        public static Result<CryptoCurrencyRawTransaction> CreateWithdraw(
            DateTime date,
            Money amount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (amount == null)
                return Result.Failure<CryptoCurrencyRawTransaction>("Sent amount cannot be null for a withdrawal.");

            if (feeAmount == null)
                feeAmount = new Money(0, amount.CurrencyCode);
            else if (feeAmount.CurrencyCode != amount.CurrencyCode)
                return Result.Failure<CryptoCurrencyRawTransaction>($"Fees are not in the same currency as the withdraw currency.");

            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>("Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyRawTransaction>("Transaction IDs cannot be null or empty.");

            return new CryptoCurrencyRawTransaction()
            {
                DateTime = date,
                Type = TransactionType.Withdrawal,
                SentAmount = amount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };
        }

        public static Result<CryptoCurrencyRawTransaction> CreateTrade(
            DateTime date,
            Money receivedAmount,
            Money sentAmount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (receivedAmount == null)
                return Result.Failure<CryptoCurrencyRawTransaction>("Received amount cannot be null for a trade transaction.");

            if (sentAmount == null)
                return Result.Failure<CryptoCurrencyRawTransaction>("Sent amount cannot be null for a trade transaction.");

            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyRawTransaction>("Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyRawTransaction>("Transaction IDs cannot be null or empty.");

            if (feeAmount == null)
                feeAmount = new Money(0, receivedAmount.CurrencyCode);

            return new CryptoCurrencyRawTransaction()
            {
                DateTime = date,
                Type = TransactionType.Trade,
                ReceivedAmount = receivedAmount.ToAbsoluteAmountMoney(),
                SentAmount = sentAmount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is not CryptoCurrencyRawTransaction other)
                return false;

            return DateTime.TruncateToSecond() == other.DateTime.TruncateToSecond() &&
                   Type == other.Type &&
                   Equals(ReceivedAmount, other.ReceivedAmount) &&
                   Equals(SentAmount, other.SentAmount) &&
                   Equals(FeeAmount, other.FeeAmount);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DateTime.TruncateToSecond(),
                                    Type,
                                    ReceivedAmount,
                                    SentAmount,
                                    FeeAmount);
        }
    }
}
