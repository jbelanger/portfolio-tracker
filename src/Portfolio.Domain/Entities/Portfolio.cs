using Portfolio.Domain.Common;


namespace Portfolio.Domain.Entities
{
    public class Portfolio : AggregateRoot
    {
        private readonly List<Wallet> _wallets = new();
        private readonly Dictionary<string, CryptoCurrencyHolding> _holdings = new();
        private readonly List<CryptoCurrencyProcessedTransaction> _processedTransactions = new();

        public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();
        public IReadOnlyCollection<CryptoCurrencyHolding> Holdings => _holdings.Values.ToList().AsReadOnly();
        public IReadOnlyCollection<CryptoCurrencyProcessedTransaction> ProcessedTransactions => _processedTransactions.AsReadOnly();

        public void AddWallet(Wallet wallet)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));

            if (_wallets.Any(w => w.Name == wallet.Name))
                throw new InvalidOperationException("Wallet already exists.");

            _wallets.Add(wallet);
        }

        public async Task CalculateTradesAsync()
        {
            _holdings.Clear();
            _processedTransactions.Clear();

            var transactions = _wallets.SelectMany(w => w.Transactions).OrderBy(t => t.DateTime);

            foreach (var transaction in transactions)
            {
                //await ProcessTransactionAsync(transaction);
            }
        }

        // private async Task ProcessTransactionAsync(CryptoCurrencyRawTransaction transaction)
        // {
        //     CryptoCurrencyHolding? sender = null;
        //     CryptoCurrencyHolding? receiver = null;

        //     if (transaction is CryptoCurrencyDepositTransaction deposit)
        //     {
        //         receiver = GetOrCreateHolding(deposit.Amount.CurrencyCode);
        //         receiver.Balance += deposit.Amount.Amount;
        //     }

        //     if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
        //     {
        //         sender = GetOrCreateHolding(withdraw.Amount.CurrencyCode);
        //         sender.Balance -= withdraw.Amount.Amount;
        //     }

        //     if (transaction is CryptoCurrencyTradeTransaction trade)
        //     {
        //         receiver = GetOrCreateHolding(trade.Amount.CurrencyCode);
        //         sender = GetOrCreateHolding(trade.TradeAmount.CurrencyCode);

        //         sender.Balance -= trade.TradeAmount.Amount;
        //         receiver.Balance += trade.Amount.Amount;

        //         // Calculate new average bought price for the receiver
        //         decimal tradedCost = trade.TradeAmount.Amount * (sender.AverageBoughtPrice ?? 0);
        //         decimal boughtPrice = tradedCost / trade.Amount.Amount;

        //         receiver.AverageBoughtPrice = ((receiver.AverageBoughtPrice ?? 0) * receiver.Balance + boughtPrice * trade.Amount.Amount) / receiver.Balance;
        //     }

        //     // Create and store processed transaction
        //     var holding = receiver ?? sender;
        //     if (holding != null)
        //     {
        //         var processedTransaction = ProcessedTransaction.CreateFromTransaction(_wallets.First(w => w.Transactions.Contains(transaction)), holding, transaction);
        //         _processedTransactions.Add(processedTransaction);
        //     }
        // }

        private CryptoCurrencyHolding GetOrCreateHolding(string currencyCode)
        {
            if (!_holdings.TryGetValue(currencyCode, out var holding))
            {
                holding = new CryptoCurrencyHolding(currencyCode);
                _holdings[currencyCode] = holding;
            }
            return holding;
        }
    }
}
