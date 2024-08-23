using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.Constants;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class UserPortfolio : AggregateRoot
    {
        private readonly TransactionProcessor _transactionProcessor;
        private List<CryptoCurrencyHolding> _holdings = new();
        private List<Wallet> _wallets = new();
        private List<TaxableEvent> _taxableEvents = new();

        public string DefaultCurrency { get; private set; } = Strings.CURRENCY_USD;

        public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();
        public IReadOnlyCollection<CryptoCurrencyHolding> Holdings => _holdings.AsReadOnly();
        public IReadOnlyCollection<TaxableEvent> TaxableEvents => _taxableEvents.AsReadOnly();

        public UserPortfolio()
        {
            _transactionProcessor = new TransactionProcessor();
        }

        public Result AddWallet(Wallet wallet)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));

            if (_wallets.Any(w => w.Name == wallet.Name))
                return Result.Failure("Wallet already exists.");

            _wallets.Add(wallet);

            return Result.Success();
        }

        public Result SetDefaultCurrency(string currencyCode)
        {
            currencyCode = currencyCode.ToUpper();
            if (!FiatCurrency.All.Any(f => f == currencyCode))
                return Result.Failure("Currency code unknown.");

            DefaultCurrency = currencyCode;

            return Result.Success();
        }

        public async Task<Result> CalculateTradesAsync(IPriceHistoryService priceHistoryService)
        {
            if (!Wallets.Any())
                return Result.Failure("No wallets to process. Start by adding a wallet.");

            var transactions = GetTransactionsFromAllWallets();
            var result = await _transactionProcessor.ProcessTransactionsAsync(transactions, this, priceHistoryService);

            if (result.IsFailure)
            {
                return result;
            }

            return Result.Success();
        }

        internal CryptoCurrencyHolding GetOrCreateHolding(string currencyCode)
        {
            var holding = _holdings.SingleOrDefault(h => h.Asset == currencyCode);
            if (holding == null)
            {
                holding = new CryptoCurrencyHolding(currencyCode);
                _holdings.Add(holding);
            }
            return holding;
        }

        internal void AddTaxableEvent(TaxableEvent taxableEvent)
        {
            _taxableEvents.Add(taxableEvent);
        }

        private IEnumerable<CryptoCurrencyRawTransaction> GetTransactionsFromAllWallets()
        {
            return Wallets.SelectMany(w => w.Transactions).OrderBy(t => t.DateTime).ToList();
        }
    }
}