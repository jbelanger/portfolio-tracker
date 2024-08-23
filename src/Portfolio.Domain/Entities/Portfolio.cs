using CSharpFunctionalExtensions;
using Portfolio.Domain;
using Portfolio.Domain.Common;
using Portfolio.Domain.Constants;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class UserPortfolio : AggregateRoot
    {
        private List<CryptoCurrencyHolding> _holdings = new();
        private List<Wallet> _wallets = new();
        private List<TaxableEvent> _taxableEvents = new();

        public string DefaultCurrency { get; private set; } = Strings.CURRENCY_USD;

        public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();
        public IReadOnlyCollection<CryptoCurrencyHolding> Holdings => _holdings.AsReadOnly();
        public IReadOnlyCollection<TaxableEvent> TaxableEvents => _taxableEvents.AsReadOnly();

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
            _holdings = (await GetHoldings(transactions, priceHistoryService).ConfigureAwait(false)).ToList();

            return Result.Success();
        }

        private IEnumerable<CryptoCurrencyRawTransaction> GetTransactionsFromAllWallets()
        {
            List<CryptoCurrencyRawTransaction> allTransactions = [.. Wallets.SelectMany(w => w.Transactions)];
            return allTransactions.OrderBy(t => t.DateTime).ToList();
        }

        internal async Task<IEnumerable<CryptoCurrencyHolding>> GetHoldings(IEnumerable<CryptoCurrencyRawTransaction> transactions, IPriceHistoryService priceHistoryService)
        {
            priceHistoryService.DefaultCurrency = DefaultCurrency;

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
                        decimal price = 0m;
                        var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.FeeAmount.CurrencyCode, tx.DateTime).ConfigureAwait(false);
                        if (priceResult.IsFailure)
                        {
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            tx.ErrorMessage = $"Could not get price history for {fees.Asset} fees. Fees calculations will be incorrect.";
                        }
                        else
                        {
                            price = priceResult.Value;
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
                        decimal price = 0m;
                        var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.ReceivedAmount.CurrencyCode, tx.DateTime);
                        if (priceResult.IsFailure)
                        {
                            // Inform the user that average bought price will not be correct until 
                            // he adds the price at that time manually.
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            tx.ErrorMessage = $"Could not get price history for {receiver.Asset}. Average price will be incorrect.";

                            price = receiver.AverageBoughtPrice; // Fallback                            
                        }
                        else
                            price = priceResult.Value;

                        tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount * price, DefaultCurrency);

                        decimal newAverage = (receiver.AverageBoughtPrice * (receiver.Balance - tx.ReceivedAmount.Amount) + tx.ValueInDefaultCurrency.Amount) / receiver.Balance;
                        receiver.AverageBoughtPrice = newAverage;
                    }
                }
                else if (tx.Type == TransactionType.Withdrawal)
                {
                    if (!EnsureAboveZeroAmount(tx, false)) continue;

                    sender = GetOrCreateHolding(tx.SentAmount.CurrencyCode);

                    decimal price = 0m;
                    var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
                    if (priceResult.IsFailure)
                    {
                        // Inform the user that average bought price will not be correct until 
                        // he adds the price at that time manually.
                        tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                        tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";

                        price = sender.AverageBoughtPrice; // Fallback
                    }
                    else
                        price = priceResult.Value;

                    tx.ValueInDefaultCurrency = new Money(tx.SentAmount.Amount * price, DefaultCurrency);

                    var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, price, tx.SentAmount.Amount, DefaultCurrency);
                    if (taxableEventResult.IsFailure)
                    {
                        tx.ErrorMessage = $"Could not create taxable event for this transaction.";
                        tx.ErrorType = ErrorType.TaxEventNotCreated;
                    }
                    _taxableEvents.Add(taxableEventResult.Value);

                    sender.Balance -= tx.SentAmount.Amount;
                    if (sender.Balance == 0)
                        sender.AverageBoughtPrice = 0m;

                    EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
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
                        decimal price = 0m;
                        var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
                        if (priceResult.IsFailure)
                        {
                            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                            tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";
                            price = sender.AverageBoughtPrice; // Fallback   
                        }
                        else
                            price = priceResult.Value;

                        tradedCostInUsd = tx.SentAmount.Amount * price;
                    }

                    tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, DefaultCurrency);

                    // Calculate the new average bought price for the receiver in USD
                    decimal newAverage = (receiver.AverageBoughtPrice * receiver.Balance + tx.ValueInDefaultCurrency.Amount) / (receiver.Balance + tx.ReceivedAmount.Amount);
                    receiver.AverageBoughtPrice = newAverage;

                    decimal boughtPriceInUsd = tradedCostInUsd / tx.SentAmount.Amount;
                    var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, boughtPriceInUsd, tx.SentAmount.Amount, DefaultCurrency);
                    if (taxableEventResult.IsFailure)
                    {
                        tx.ErrorMessage = $"Could not create taxable event for this transaction.";
                        tx.ErrorType = ErrorType.TaxEventNotCreated;
                    }
                    _taxableEvents.Add(taxableEventResult.Value);

                    receiver.Balance += tx.ReceivedAmount.Amount;
                    sender.Balance -= tx.SentAmount.Amount;

                    if (sender.Balance == 0)
                        sender.AverageBoughtPrice = 0m;

                    EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
                }
            }

            // Fetch latest prices...
            foreach (var holding in _holdings.Where(h => h.Balance > 0))
            {
                if (holding.Asset == DefaultCurrency)
                    holding.CurrentPrice = new Money(1m, holding.Asset);
                else
                {
                    var priceValueResult = await priceHistoryService.GetPriceAtCloseTimeAsync(holding.Asset, DateTime.Now).ConfigureAwait(false);
                    if (priceValueResult.IsFailure)
                    {
                        holding.ErrorType = ErrorType.PriceHistoryUnavailable;
                        holding.ErrorMessage = $"Could not get price history for {holding.Asset}. Average price will be incorrect.";
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
                tx.ErrorMessage = $"{asset} Balance is under zero: {asset}";
            }
        }

        private static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx, bool incoming = true)
        {
            if (tx.SentAmount.Amount < 0 || tx.ReceivedAmount.Amount < 0 || tx.FeeAmount.Amount < 0)
            {
                tx.ErrorMessage = $"Invalid trade amounts. Fees, sent or Received amounts are non-positive in transaction: {tx.TransactionIds}";
                tx.ErrorType = ErrorType.InvalidCurrency; // or another appropriate ErrorType                    
                return false;
            }

            if (incoming && tx.ReceivedAmount.Amount == 0)
            {
                tx.ErrorMessage = $"Received amount is zero in trade transaction: {tx.TransactionIds}";
                tx.ErrorType = ErrorType.ManualReviewRequired;
                return false;
            }

            if (!incoming && tx.SentAmount.Amount == 0)
            {
                tx.ErrorMessage = $"Sent amount is zero in trade transaction: {tx.TransactionIds}";
                tx.ErrorType = ErrorType.ManualReviewRequired;
                return false;
            }
            return true;
        }

        // public void CheckForMissingTransactions(IEnumerable<CryptoCurrencyRawTransaction> transactions)
        // {
        //     var transactionsByHolding = transactions.GroupBy(t => t.ReceivedAmount.CurrencyCode);
        //     foreach (var txByHolding in transactionsByHolding)
        //     {
        //         var holding = _holdings.Single(h => h.Asset == txByHolding.Key);
        //         var holdingAsset = holding.Asset;
        //         if (FiatCurrency.All.Any(f => f == holdingAsset))
        //             continue;

        //         decimal expectedTotal = 0;

        //         // Total number of IN transactions should equal to the balance.
        //         foreach (var tx in txByHolding)
        //         {                    
        //             if (tx.Type == TransactionType.Deposit)
        //             {
        //                 expectedTotal += tx.ReceivedAmount.Amount;
        //             }
        //             else if (tx.Type == TransactionType.Withdrawal)
        //             {
        //                 expectedTotal -= tx.SentAmount.Amount;
        //                 expectedTotal -= tx.FeeAmount.Amount;
        //             }
        //             else if (tx.Type == TransactionType.Trade)
        //             {
        //                 if (tx.ReceivedAmount.CurrencyCode == holdingAsset)
        //                     expectedTotal += tx.ReceivedAmount.Amount;
        //                 else if (tx.SentAmount.CurrencyCode == holdingAsset)
        //                 {
        //                     expectedTotal -= tx.SentAmount.Amount;
        //                     if (tx.SentAmount.CurrencyCode == tx.FeeAmount.CurrencyCode)
        //                         expectedTotal -= tx.FeeAmount.Amount;
        //                 }
        //             }
        //         }

        //         if (expectedTotal != holding.Balance)
        //         {
        //             holding.ErrorMessage = $"Missing transactions for holding {Asset}", holding.Asset);
        //             holding.ErrorType = ErrorType.ManualReviewRequired; // or another appropriate ErrorType        
        //         }                    
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