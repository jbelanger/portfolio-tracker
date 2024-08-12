using CSharpFunctionalExtensions;
using Portfolio.Shared;
using Serilog;

namespace Portfolio.App;

public delegate void DepositAddedHandler(CryptoCurrencyDepositTransaction deposit, CryptoCurrencyHolding holding);
public delegate void WithdrawAddedHandler(CryptoCurrencyWithdrawTransaction deposit, CryptoCurrencyHolding holding);
public delegate void TradeAddedHandler(CryptoCurrencyTradeTransaction deposit, CryptoCurrencyHolding holding);

public class Portfolio
{
    private readonly IPriceHistoryStoreFactory _priceHistoryStoreFactory = null!;
    private Dictionary<string, IPriceHistoryStore> _priceHistoryStores = new();
    private List<CryptoCurrencyHolding> _holdings = new();

    public event DepositAddedHandler OnDepositAdded;
    public event WithdrawAddedHandler OnWithdrawAdded;
    public event TradeAddedHandler OnTradeAdded;

    List<ICryptoCurrencyTransaction> _taxableEvents = new();

    public string DefaultCurrency { get; private set; } = "USD";
    public IReadOnlyCollection<Wallet> Wallets { get; private set; } = new List<Wallet>();
    public IReadOnlyCollection<CryptoCurrencyHolding> Holdings { get => _holdings; }

    public Portfolio(IPriceHistoryStoreFactory priceHistoryStoreFactory)
    {
        _priceHistoryStoreFactory = priceHistoryStoreFactory;
    }

    public Result SetDefaultCurrency(string currencyCode)
    {
        currencyCode = currencyCode.ToUpper();
        if (!FiatCurrencies.Codes.Contains(currencyCode))
            return Result.Failure("Currency code unknown.");

        DefaultCurrency = currencyCode;

        return Result.Success();
    }

    public Result AddWallet(Wallet wallet)
    {
        if (Wallets.Any(w => w.Name == wallet.Name))
            return Result.Failure("Wallet already exists.");

        var wallets = Wallets.ToList();
        wallets.Add(wallet);
        Wallets = wallets.AsReadOnly();

        return Result.Success();
    }

    public async Task<Result> Process()
    {
        if (!Wallets.Any())
            return Result.Failure("No wallets to process. Start by adding a wallet.");

        // Get all transactions, in order, for easier processing.
        var transactions = GetTransactionsFromAllWallets();

        // Load historical conversion price data. Used to calculate profit-loss.
        _priceHistoryStores = await GetPriceHistoryDataStores(_priceHistoryStoreFactory, transactions);

        _holdings = (await GetHoldings(transactions)).ToList();

        return Result.Success();
    }

    private async Task<Dictionary<string, IPriceHistoryStore>> GetPriceHistoryDataStores(IPriceHistoryStoreFactory priceHistoryStoreFactory, IEnumerable<ICryptoCurrencyTransaction> transactions)
    {
        Dictionary<string, IPriceHistoryStore> priceHistoryStores = new();
        var transactionsByCurrency = transactions.GroupBy(t => t.Amount.CurrencyCode);
        foreach (var txByCurrency in transactionsByCurrency)
        {
            var currency = txByCurrency.Key;
            var earliestTransaction = txByCurrency.First();
            var storeResult = await priceHistoryStoreFactory.Create(currency, DefaultCurrency, earliestTransaction.DateTime, DateTime.Now);
            if (storeResult.IsFailure)
            {
                Log.Error(storeResult.Error);
                continue;
            }
            priceHistoryStores.Add(currency, storeResult.Value);
        }

        // Also add the traded currency in trades...
        transactionsByCurrency = transactions
            .Where(t => t is CryptoCurrencyTradeTransaction)
            .Cast<CryptoCurrencyTradeTransaction>()
            .GroupBy(t => t.TradeAmount.CurrencyCode);
        foreach (var txByCurrency in transactionsByCurrency)
        {
            if (!priceHistoryStores.Any(s => s.Key == txByCurrency.Key))
            {
                var currency = txByCurrency.Key;
                var earliestTransaction = txByCurrency.First();
                var storeResult = await priceHistoryStoreFactory.Create(currency, DefaultCurrency, earliestTransaction.DateTime, DateTime.Now);
                if (storeResult.IsFailure)
                {
                    Log.Error(storeResult.Error);
                    continue;
                }
                priceHistoryStores.Add(currency, storeResult.Value);
            }
        }

        return priceHistoryStores;
    }

    private IEnumerable<ICryptoCurrencyTransaction> GetTransactionsFromAllWallets()
    {
        List<ICryptoCurrencyTransaction> allTransactions = [.. Wallets.SelectMany(w => w.Transactions)];
        return allTransactions.OrderBy(t => t.DateTime).ToList();
    }

    internal async Task<IEnumerable<CryptoCurrencyHolding>> GetHoldings(IEnumerable<ICryptoCurrencyTransaction> transactions)
    {
        foreach (var tx in transactions)
        {
            CryptoCurrencyHolding? sender = null;
            CryptoCurrencyHolding? receiver = null;

            if (tx is CryptoCurrencyDepositTransaction deposit)
            {
                receiver = GetOrCreateHolding(deposit.Amount.CurrencyCode);
                var addTxResult = receiver.AddTransaction(deposit);
                if (addTxResult.IsFailure)
                    throw new Exception(addTxResult.Error);

                if (deposit.Amount.CurrencyCode == DefaultCurrency)
                    receiver.AverageBoughtPrice = 1;
                else
                {
                    if (!_priceHistoryStores.ContainsKey(deposit.Amount.CurrencyCode))
                    {
                        Log.Error($"No price history available for {receiver.Asset}.");
                        continue;
                    }

                    var priceValueResult = await _priceHistoryStores[deposit.Amount.CurrencyCode].GetPriceDataAsync(deposit.DateTime);
                    if (priceValueResult.IsFailure)
                    {
                        Log.Error(priceValueResult.Error);
                        continue;
                    }
                    deposit.UnitValue = new Money(priceValueResult.Value.Close, DefaultCurrency);

                    decimal newAverage = ((receiver.AverageBoughtPrice * (receiver.Balance - deposit.Amount.Amount)) + (deposit.Amount.Amount * priceValueResult.Value.Close)) / (receiver.Balance);
                    receiver.AverageBoughtPrice = newAverage;
                }

                //Log.Debug($"Deposit [{string.Join("|", tx.TransactionIds)}: {receiver.Asset}: {receiver.Balance}");

                OnDepositAdded?.Invoke(deposit, receiver);
            }

            if (tx is CryptoCurrencyWithdrawTransaction withdraw)
            {
                sender = GetOrCreateHolding(withdraw.Amount.CurrencyCode);
                var addTxResult = sender.AddTransaction(withdraw);
                if (addTxResult.IsFailure)
                    throw new Exception(addTxResult.Error);

                _taxableEvents.Add(tx);

                if (sender.Balance < 0)
                {
                    Log.Warning($"{sender.Asset} Balance is under zero: {sender.Balance}");
                }

                OnWithdrawAdded?.Invoke(withdraw, sender);

                //Log.Debug($"Withdraw [{string.Join("|", tx.TransactionIds)}: {sender.Asset}: {sender.Balance}");
            }

            if (tx is CryptoCurrencyTradeTransaction trade)
            {

                // TODO: Ensure to log whenever a trade amount comes from a currency not 
                // owned in holdings. It would means the user spends money he doesn't have deposited or traded before.

                receiver = GetOrCreateHolding(trade.Amount.CurrencyCode);
                sender = GetOrCreateHolding(trade.TradeAmount.CurrencyCode);

                if (trade.Amount.CurrencyCode == DefaultCurrency)
                    receiver.AverageBoughtPrice = 1;
                else //if(!deposit.Amount.IsFiatCurrency)
                {
                    decimal tradedCost = trade.TradeAmount.Amount * sender.AverageBoughtPrice; // 0.04 * 25000 = 1000
                    decimal boughtPrice = tradedCost / trade.Amount.Amount;

                    //var priceValue = await _priceHistoryStores[trade.Amount.CurrencyCode].GetPriceDataAsync(trade.DateTime);
                    //trade.UnitValue = new Money(priceValue.Close, DefaultCurrency);

                    decimal newAverage = ((receiver.AverageBoughtPrice * receiver.Balance) + (trade.Amount.Amount * boughtPrice)) / (receiver.Balance + trade.Amount.Amount);
                    receiver.AverageBoughtPrice = newAverage;
                }
                var addTxResult = receiver.AddTransaction(trade);
                if (addTxResult.IsFailure)
                    throw new Exception(addTxResult.Error);
                OnTradeAdded?.Invoke(trade, receiver);

                var addTxResult2 = sender.AddTransaction(trade);
                if (addTxResult2.IsFailure)
                    throw new Exception(addTxResult2.Error);
                OnTradeAdded?.Invoke(trade, sender);

                if (sender.Balance < 0)
                {
                    Log.Error($"{sender.Asset} Balance is under zero: {sender.Balance}");
                }

                // Log.Debug($"Trade Out [{string.Join("|", trade.TransactionIds)}: {sender.Asset}: {sender.Balance}");
                // Log.Debug($"Trade In [{string.Join("|", trade.TransactionIds)}: {receiver.Asset}: {receiver.Balance}");
            }
        }

        // Fetch latest prices...
        foreach (var holding in _holdings)
        {
            if(holding.Asset == "UNI")
            ;
            if (holding.Asset == DefaultCurrency)
            {
                holding.CurrentPrice = new Money(1m, holding.Asset);
            }
            else
            {
                if (!_priceHistoryStores.ContainsKey(holding.Asset))
                {
                    Log.Error($"No price history available for {holding.Asset}.");
                    holding.CurrentPrice = new Money(0m, holding.Asset);
                    continue;
                }
                var coinGeckoResult = await new CoinGeckoPriceHistoryStoreFactory().Create(holding.Asset, DefaultCurrency, DateTime.Now, DateTime.Now);
                if(coinGeckoResult.IsFailure)
                {
                    Log.Error(coinGeckoResult.Error);
                    holding.CurrentPrice = new Money(0m, holding.Asset);
                    continue;
                }
                var priceValueResult = await coinGeckoResult.Value.GetPriceDataAsync(DateTime.Now);
                if (priceValueResult.IsFailure)
                {
                    Log.Error(priceValueResult.Error);
                    holding.CurrentPrice = new Money(0m, holding.Asset);
                    continue;
                }
                holding.CurrentPrice = new Money(priceValueResult.Value.Close, holding.Asset);
            }
        }

        return _holdings;
    }

    public void CheckForMissingTransactions()
    {
        foreach (var holding in _holdings)
        {
            if (FiatCurrencies.Codes.Contains(holding.Asset))
                continue;

            decimal expectedTotal = 0;

            if (holding.Asset == "LINK")
                ;
            // Total number of IN transactions should equal to the balance.
            foreach (var tx in holding.Transactions)
            {
                if (tx is CryptoCurrencyDepositTransaction deposit)
                {
                    expectedTotal += deposit.Amount.Amount; //48,219
                }
                else if (tx is CryptoCurrencyWithdrawTransaction withdraw)
                {
                    expectedTotal -= withdraw.Amount.Amount;//15 36
                    expectedTotal -= withdraw.FeeAmount.Amount;
                }
                else if (tx is CryptoCurrencyTradeTransaction trade)
                {
                    if (trade.Amount.CurrencyCode == holding.Asset)
                        expectedTotal += trade.Amount.Amount;
                    else if (trade.TradeAmount.CurrencyCode == holding.Asset)
                    {
                        expectedTotal -= trade.TradeAmount.Amount;
                        if (trade.TradeAmount.CurrencyCode == trade.FeeAmount.CurrencyCode)
                            expectedTotal -= trade.FeeAmount.Amount;
                    }
                }
            }

            if (expectedTotal != holding.Balance)
                Log.Warning($"Missing transactions for holding {holding.Asset}");
        }

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
}

internal class TaxableEvent
{
    public ICryptoCurrencyTransaction Transaction { get; set; }
    public decimal AverageBuyingPrice { get; set; }
    public decimal PriceAtEvent { get; set; }
}