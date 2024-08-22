using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    public class Wallet : BaseAuditableEntity
    {
        public string Name { get; init; } = string.Empty;
        public long PortfolioId { get; set; }
        private readonly List<CryptoCurrencyRawTransaction> _transactions = new();
        public IReadOnlyCollection<CryptoCurrencyRawTransaction> Transactions => _transactions.AsReadOnly();

        private Wallet() { }

        public static Result<Wallet> Create(string walletName)
        {
            if (string.IsNullOrWhiteSpace(walletName)) return Result.Failure<Wallet>("Name cannot be empty.");

            return new Wallet
            {
                Name = walletName
            };
        }

        public Result AddTransaction(CryptoCurrencyRawTransaction transaction)
        {
            if (transaction == null)
                return Result.Failure("Transaction cannot be null.");

            if (_transactions.Any(t => IsSameTransaction(t, transaction)))// || !_transactions.Add(transaction))
                return Result.Failure("Transaction already exists in this holding.");
            _transactions.Add(transaction);
            return Result.Success();
        }

        public Result RemoveTransaction(CryptoCurrencyRawTransaction transaction)
        {
            if (transaction == null)
                return Result.Failure("Transaction cannot be null.");

            if (!_transactions.Remove(transaction))
            {
                return Result.Failure("Transaction not found in this holding.");
            }

            return Result.Success();
        }

        public bool IsSameTransaction(CryptoCurrencyRawTransaction obj, CryptoCurrencyRawTransaction other)
        {
            return obj.DateTime.TruncateToSecond() == other.DateTime.TruncateToSecond() &&
                   obj.Type == other.Type &&
                   Equals(obj.ReceivedAmount, other.ReceivedAmount) &&
                   Equals(obj.SentAmount, other.SentAmount) &&
                   Equals(obj.FeeAmount, other.FeeAmount);
        }
    }
}
