using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    public class Wallet : BaseAuditableEntity
    {        
        public string Name { get; init; } = string.Empty;
        private readonly HashSet<CryptoCurrencyRawTransaction> _transactions = new();
        
        public IReadOnlyCollection<CryptoCurrencyRawTransaction> Transactions => _transactions;

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

            if (!_transactions.Add(transaction))
            {
                return Result.Failure("Transaction already exists in this holding.");
            }            

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
    }
}
