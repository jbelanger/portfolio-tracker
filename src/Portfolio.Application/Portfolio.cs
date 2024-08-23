using Flurl.Util;
using Portfolio.App.HistoricalPrice;
using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App
{
    public delegate void DepositAddedHandler(CryptoCurrencyRawTransaction deposit, CryptoCurrencyHolding holding);
    public delegate void WithdrawAddedHandler(CryptoCurrencyRawTransaction deposit, CryptoCurrencyHolding holding);
    public delegate void TradeAddedHandler(CryptoCurrencyRawTransaction deposit, CryptoCurrencyHolding holding);

    public class Portfolio
    {
        private readonly IPriceHistoryService _priceHistoryService = null!;
        private List<CryptoCurrencyHolding> _holdings = new();

        public event DepositAddedHandler? OnDepositAdded;
        public event WithdrawAddedHandler? OnWithdrawAdded;
        public event TradeAddedHandler? OnTradeAdded;

        List<TaxableEvent> _taxableEvents = new();

        public string DefaultCurrency { get; private set; } = Strings.CURRENCY_USD;
        public List<Wallet> Wallets { get; set; } = new List<Wallet>();
        public List<CryptoCurrencyHolding> Holdings { get => _holdings; }
        public List<TaxableEvent> TaxableEvents { get => _taxableEvents; set => _taxableEvents = value; }

        public Portfolio(IPriceHistoryService priceHistoryService)
        {
            _priceHistoryService = priceHistoryService;
        }

        public Result SetDefaultCurrency(string currencyCode)
        {
            currencyCode = currencyCode.ToUpper();
            if (!FiatCurrency.All.Any(f => f == currencyCode))
                return Result.Failure("Currency code unknown.");

            DefaultCurrency = currencyCode;
            _priceHistoryService.DefaultCurrency = currencyCode;

            return Result.Success();
        }

        public async Task<Result> ProcessAsync()
        {
            if (!Wallets.Any())
                return Result.Failure("No wallets to process. Start by adding a wallet.");

            var transactions = GetTransactionsFromAllWallets();
            _holdings = (await GetHoldings(transactions).ConfigureAwait(false)).ToList();

            return Result.Success();
        }
        private IEnumerable<CryptoCurrencyRawTransaction> GetTransactionsFromAllWallets()
        {
            List<CryptoCurrencyRawTransaction> allTransactions = [.. Wallets.SelectMany(w => w.Transactions)];
            return allTransactions.OrderBy(t => t.DateTime).ToList();
        }

        internal async Task<IEnumerable<CryptoCurrencyHolding>> GetHoldings(IEnumerable<CryptoCurrencyRawTransaction> transactions)
        {
            foreach (var tx in transactions)
            {
                CryptoCurrencyHolding? sender = null;
                CryptoCurrencyHolding? receiver = null;
                CryptoCurrencyHolding? fees = null;

                if (tx.FeeAmount != Money.Empty)
                {
                    fees = GetOrCreateHolding(tx.FeeAmount.CurrencyCode);
                    bool shouldDeductFeesFromBalance = true;

                    if (tx.Type == TransactionType.Deposit || tx.Type == TransactionType.Trade)
                    {
                        // Fee might already be deducted from amount...
                        // Ex: Deposit of USD_Amount=95, USD_Fee=5, Total=USD_100, we do not deduct fees  
                        // We only deduct when fees not paid in the transaction's currency...
                        shouldDeductFeesFromBalance = !(tx.FeeAmount.CurrencyCode == tx.ReceivedAmount.CurrencyCode);
                    }

                    if (shouldDeductFeesFromBalance)
                        fees.Balance -= tx.FeeAmount.Amount;

                    if (tx.FeeAmount.CurrencyCode == DefaultCurrency)
                    {
                        tx.FeeValueInDefaultCurrency = tx.FeeAmount;
                    }
                    else
                    {
                        // Fetch the price of the sent currency in USD
                        decimal price = await GetPriceWithRetryAsync(tx.FeeAmount.CurrencyCode, tx.DateTime);
                        if (price == 0)
                        {
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            Log.Warning("Could not get price history for {Asset} fees. Fees calculations will be incorrect.", fees.Asset);
                        }
                        else
                        {
                            tx.FeeValueInDefaultCurrency = new Money(tx.FeeAmount.Amount * price, DefaultCurrency);
                        }
                    }

                    EnsureBalanceNotNegative(tx, fees.Asset, fees.Balance);
                }

                if (tx.Type == TransactionType.Deposit)
                {
                    if (!EnsureAboveZeroAmount(tx)) continue;

                    receiver = GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
                    receiver.Balance += tx.ReceivedAmount.Amount;

                    if (tx.ReceivedAmount.CurrencyCode == DefaultCurrency)
                    {
                        receiver.AverageBoughtPrice = 1;
                        tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount, DefaultCurrency);
                    }
                    else
                    {
                        decimal price = await GetPriceWithRetryAsync(tx.ReceivedAmount.CurrencyCode, tx.DateTime);
                        if (price == 0)
                        {
                            // Inform the user that average bought price will not be correct until 
                            // he adds the price at that time manually.
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            Log.Warning("Could not get price history for {Asset}. Average price will be incorrect.", receiver.Asset);
                            
                            price = receiver.AverageBoughtPrice; // Fallback                            
                        }

                        tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount * price, DefaultCurrency);

                        decimal newAverage = ((receiver.AverageBoughtPrice * (receiver.Balance - tx.ReceivedAmount.Amount)) + tx.ValueInDefaultCurrency.Amount) / (receiver.Balance);
                        receiver.AverageBoughtPrice = newAverage;
                    }

                    OnDepositAdded?.Invoke(tx, receiver);
                }
                else if (tx.Type == TransactionType.Withdrawal)
                {
                    if (!EnsureAboveZeroAmount(tx, false)) continue;

                    sender = GetOrCreateHolding(tx.SentAmount.CurrencyCode);

                    decimal price = await GetPriceWithRetryAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
                    if (price == 0)
                    {
                        // Inform the user that average bought price will not be correct until 
                        // he adds the price at that time manually.
                        tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                        Log.Warning("Could not get price history for {Asset}. Average price will be incorrect.", sender.Asset);
                        
                        price = sender.AverageBoughtPrice; // Fallback
                    }

                    tx.ValueInDefaultCurrency = new Money(tx.SentAmount.Amount * price, DefaultCurrency);

                    var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, price, tx.SentAmount.Amount, DefaultCurrency);
                    if (taxableEventResult.IsFailure)
                    {
                        Log.Warning("Could not create taxable event for this transaction.");
                        tx.ErrorType = ErrorType.DataCorruption;
                    }
                    TaxableEvents.Add(taxableEventResult.Value);

                    sender.Balance -= tx.SentAmount.Amount;
                    if (sender.Balance == 0)
                        sender.AverageBoughtPrice = 0m;

                    EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);

                    OnWithdrawAdded?.Invoke(tx, sender);
                }
                else if (tx.Type == TransactionType.Trade)
                {
                    if (!EnsureAboveZeroAmount(tx)) continue;
                    if (!EnsureAboveZeroAmount(tx, false)) continue;

                    receiver = GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
                    sender = GetOrCreateHolding(tx.SentAmount.CurrencyCode);

                    decimal tradedCostInUsd;

                    // Scenario 1: Received amount is in USD (default currency)
                    if (tx.ReceivedAmount.CurrencyCode == DefaultCurrency)
                    {
                        tradedCostInUsd = tx.ReceivedAmount.Amount;
                    }
                    // Scenario 2: Sent amount is in USD (default currency)
                    else if (tx.SentAmount.CurrencyCode == DefaultCurrency)
                    {
                        tradedCostInUsd = tx.SentAmount.Amount;
                    }
                    // Scenario 3: Crypto-to-crypto trade where neither currency is the default (USD)
                    else
                    {
                        // Fetch the price of the sent currency in USD
                        decimal price = await GetPriceWithRetryAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
                        if (price == 0)
                        {
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            Log.Warning("Could not get price history for {Asset}. Average price will be incorrect.", sender.Asset);
                            
                            price = sender.AverageBoughtPrice; // Fallback   
                        }
                        tradedCostInUsd = tx.SentAmount.Amount * price;
                    }

                    tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, DefaultCurrency);

                    // Calculate the new average bought price for the receiver in USD
                    decimal newAverage = ((receiver.AverageBoughtPrice * receiver.Balance) + tx.ValueInDefaultCurrency.Amount) / (receiver.Balance + tx.ReceivedAmount.Amount);
                    receiver.AverageBoughtPrice = newAverage;

                    decimal boughtPriceInUsd = tradedCostInUsd / tx.SentAmount.Amount;
                    var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, boughtPriceInUsd, tx.SentAmount.Amount, DefaultCurrency);
                    if (taxableEventResult.IsFailure)
                    {
                        Log.Warning("Could not create taxable event for this transaction.");
                        tx.ErrorType = ErrorType.DataCorruption;
                    }
                    TaxableEvents.Add(taxableEventResult.Value);

                    receiver.Balance += tx.ReceivedAmount.Amount;
                    sender.Balance -= tx.SentAmount.Amount;

                    if (sender.Balance == 0)
                        sender.AverageBoughtPrice = 0m;

                    EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);

                    OnTradeAdded?.Invoke(tx, sender);
                }
            }

            // Fetch latest prices...
            foreach (var holding in _holdings.Where(h => h.Balance > 0))
            {
                if (holding.Asset == DefaultCurrency)
                    holding.CurrentPrice = new Money(1m, holding.Asset);
                else
                {
                    var priceValueResult = await _priceHistoryService.GetPriceAtCloseTimeAsync(holding.Asset, DateTime.Now).ConfigureAwait(false);
                    if (priceValueResult.IsFailure)
                    {
                        Log.Error(priceValueResult.Error);
                        holding.CurrentPrice = new Money(0m, holding.Asset);
                        continue;
                    }
                    holding.CurrentPrice = new Money(priceValueResult.Value, holding.Asset);
                }
            }

            return _holdings;
        }

        private static void EnsureBalanceNotNegative(CryptoCurrencyRawTransaction tx, string asset, decimal balance)
        {
            if (balance < 0)
            {
                // Ensure the asset currency is correctly set... for example make sure the fiat CAD is not recognized as the Cadence crypto project with the same symbol.
                tx.ErrorType = ErrorType.InsufficientFunds;
                Log.Error("{Asset} Balance is under zero: {Balance}", asset, balance);
            }
        }

        private static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx, bool incoming = true)
        {
            if (tx.SentAmount.Amount < 0 || tx.ReceivedAmount.Amount < 0 || tx.FeeAmount.Amount < 0)
            {
                Log.Error("Invalid trade amounts. Fees, sent or Received amounts are non-positive in transaction: {TransactionId}", tx.TransactionIds);
                tx.ErrorType = ErrorType.InvalidCurrency; // or another appropriate ErrorType                    
                return false;
            }

            if (incoming && tx.ReceivedAmount.Amount == 0)
            {
                Log.Warning("Received amount is zero in trade transaction: {TransactionId}", tx.TransactionIds);
                tx.ErrorType = ErrorType.ManualReviewRequired;
                return false;
            }

            if (!incoming && tx.SentAmount.Amount == 0)
            {
                Log.Warning("Sent amount is zero in trade transaction: {TransactionId}", tx.TransactionIds);
                tx.ErrorType = ErrorType.ManualReviewRequired;
                return false;
            }
            return true;
        }

        private async Task<decimal> GetPriceWithRetryAsync(string currencyCode, DateTime dateTime, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                var priceResult = await _priceHistoryService.GetPriceAtCloseTimeAsync(currencyCode, dateTime).ConfigureAwait(false);
                if (priceResult.IsSuccess)
                {
                    return priceResult.Value;
                }

                retryCount++;
                Log.Warning("Retrying price retrieval for {CurrencyCode} on {Date:yyyy-MM-dd}. Attempt {RetryCount}/{MaxRetries}", currencyCode, dateTime, retryCount, maxRetries);
            }

            return 0; // Indicating failure
        }

        // public void CheckForMissingTransactions()
        // {
        //     foreach (var holding in _holdings)
        //     {
        //         if (FiatCurrency.All.Any(f => f == holding.Asset))
        //             continue;

        //         decimal expectedTotal = 0;

        //         // Total number of IN transactions should equal to the balance.
        //         foreach (var tx in holding.Transactions)
        //         {
        //             if (tx is CryptoCurrencyDepositTransaction deposit)
        //             {
        //                 expectedTotal += deposit.Amount.Amount; //48,219
        //             }
        //             else if (tx is CryptoCurrencyWithdrawTransaction withdraw)
        //             {
        //                 expectedTotal -= withdraw.Amount.Amount;//15 36
        //                 expectedTotal -= withdraw.FeeAmount.Amount;
        //             }
        //             else if (tx is CryptoCurrencyTradeTransaction trade)
        //             {
        //                 if (trade.Amount.CurrencyCode == holding.Asset)
        //                     expectedTotal += trade.Amount.Amount;
        //                 else if (trade.TradeAmount.CurrencyCode == holding.Asset)
        //                 {
        //                     expectedTotal -= trade.TradeAmount.Amount;
        //                     if (trade.TradeAmount.CurrencyCode == trade.FeeAmount.CurrencyCode)
        //                         expectedTotal -= trade.FeeAmount.Amount;
        //                 }
        //             }
        //         }

        //         if (expectedTotal != holding.Balance)
        //             Log.Warning("Missing transactions for holding {Asset}", holding.Asset);
        //     }
        // }

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
    }
}