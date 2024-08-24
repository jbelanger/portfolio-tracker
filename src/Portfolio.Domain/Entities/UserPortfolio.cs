using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.Constants;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class UserPortfolio : AggregateRoot
    {
        private readonly TransactionProcessor _transactionProcessor;
        private List<AssetHolding> _holdings = new();
        private List<Wallet> _wallets = new();
        private List<FinancialEvent> _financialEvents = new();
        private ICostBasisCalculationStrategy _costBasisStrategy = null!;
        private readonly Dictionary<string, ICostBasisCalculationStrategy> _costBasisStrategies = new()
        {
            { "AVG", new AcbCostBasisCalculationStrategy() },
            { "FIFO", new FifoCostBasisCalculationStrategy() },
            { "LIFO", new LifoCostBasisCalculationStrategy() }
        };

        public string DefaultCurrency { get; private set; } = Strings.CURRENCY_USD;

        public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();
        public IReadOnlyCollection<AssetHolding> Holdings => _holdings.AsReadOnly();
        public IReadOnlyCollection<FinancialEvent> FinancialEvents => _financialEvents.AsReadOnly();

        public UserPortfolio()
        {
            _transactionProcessor = new TransactionProcessor();
            SetCostBasisStrategy("AVG"); // Default to AVG
        }

        public void SetCostBasisStrategy(string strategy)
        {
            if (_costBasisStrategies.ContainsKey(strategy))
            {
                _costBasisStrategy = _costBasisStrategies[strategy];
            }
            else
            {
                throw new ArgumentException($"Cost basis strategy {strategy} not supported.");
            }
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

        internal AssetHolding GetOrCreateHolding(string currencyCode)
        {
            var holding = _holdings.SingleOrDefault(h => h.Asset == currencyCode);
            if (holding == null)
            {
                holding = new AssetHolding(currencyCode);
                _holdings.Add(holding);
            }
            return holding;
        }

        /// <summary>
        /// Records a financial transaction event, such as a trade or withdrawal, 
        /// calculating the capital gain or loss based on the provided transaction data.
        /// </summary>
        /// <param name="transaction">The raw transaction data containing details of the trade or withdrawal.</param>
        /// <param name="holding">The current holding of the disposed asset.</param>
        /// <param name="marketPricePerUnit">The market price per unit of the disposed asset at the time of the transaction.</param>
        internal void RecordFinancialEvent(
            FinancialTransaction transaction,
            AssetHolding holding,
            decimal marketPricePerUnit
        )
        {
            // Check if the transaction involves a fiat currency and is a purchase (fiat-to-asset)
            if (transaction.SentAmount.IsFiatCurrency)
            {
                // Fiat-to-asset purchase, not a taxable event, so skip
                return;
            }

            // Calculate the cost basis per unit for the disposed asset
            var costBasisPerUnit = _costBasisStrategy.CalculateCostBasis([holding], transaction) / transaction.SentAmount.Amount;

            // Create the financial event (whether it's a trade or a withdrawal)
            var financialEventResult = FinancialEvent.Create(
                transaction.DateTime,
                transaction.SentAmount.CurrencyCode,
                costBasisPerUnit,
                marketPricePerUnit,
                transaction.SentAmount.Amount,
                DefaultCurrency
            );

            // Add the event to the portfolio's list if successful
            if (financialEventResult.IsSuccess)
            {
                _financialEvents.Add(financialEventResult.Value);
            }
            else
            {
                // Handle the error if the event creation failed
                transaction.ErrorMessage = "Could not create financial event for this transaction.";
                transaction.ErrorType = ErrorType.EventCreationFailed;
            }
        }

        /// <summary>
        /// Retrieves all transactions from all wallets in the portfolio.
        /// </summary>
        /// <returns>A collection of transactions ordered by date.</returns>
        private IEnumerable<FinancialTransaction> GetTransactionsFromAllWallets()
        {
            return _wallets.SelectMany(w => w.Transactions).OrderBy(t => t.DateTime).ToList();
        }
    }
}