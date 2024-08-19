using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.Events;

namespace Portfolio.Domain.Entities
{
    public class Wallet : AggregateRoot
    {        
        public string Name { get; private set; } = string.Empty;

        private readonly HashSet<CryptoCurrencyHolding> _holdings = new();
        public IReadOnlyCollection<CryptoCurrencyHolding> Holdings => _holdings;

        private Wallet() { }

        public static Result<Wallet> Create(string walletName, IEnumerable<ICryptoCurrencyTransaction> transactions)
        {
            if (string.IsNullOrWhiteSpace(walletName))
                throw new ArgumentException("Name cannot be empty.", nameof(walletName));

            var wallet = new Wallet
            {
                Name = walletName
            };

            foreach (var transaction in transactions.OrderBy(t => t.DateTime))
            {
                wallet.ProcessTransaction(transaction);
            }

            return wallet;
        }

        private void ProcessTransaction(ICryptoCurrencyTransaction transaction)
        {
            CryptoCurrencyHolding? receiver = null;

            if (transaction is CryptoCurrencyDepositTransaction deposit)
            {
                receiver = GetOrCreateHolding(deposit.Amount.CurrencyCode);
                receiver.AddTransaction(deposit);
                AddDomainEvent(new TransactionAddedDomainEvent(receiver, deposit));
            }

            if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
            {
                receiver = GetOrCreateHolding(withdraw.Amount.CurrencyCode);
                receiver.AddTransaction(withdraw);
                AddDomainEvent(new TransactionAddedDomainEvent(receiver, withdraw));
            }

            if (transaction is CryptoCurrencyTradeTransaction trade)
            {
                receiver = GetOrCreateHolding(trade.Amount.CurrencyCode);
                receiver.AddTransaction(trade);
                AddDomainEvent(new TransactionAddedDomainEvent(receiver, trade));
            }
        }

        private CryptoCurrencyHolding GetOrCreateHolding(string currencyCode)
        {
            var holding = _holdings.SingleOrDefault(h => h.Asset == currencyCode);
            if (holding == null)
            {
                holding = new CryptoCurrencyHolding(currencyCode);
                _holdings.Add(holding);
            }
            return holding;
        }
    }
}
