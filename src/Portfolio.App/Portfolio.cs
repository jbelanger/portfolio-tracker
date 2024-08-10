using System.ComponentModel;
using System.Reflection;
using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using Portfolio.Kraken;
using Portfolio.Shared;
using RestSharp;
using Serilog;

namespace Portfolio;

internal class Portfolio
{
    List<ICryptoCurrencyTransaction> _transactions = new();
    List<CryptoCurrencyHolding> _holdings = new();
    List<ICryptoCurrencyTransaction> _taxableEvents = new();

    public string DefaultCurrency { get; }

    public Portfolio(string defaultCurrency = "USD")
    {
        DefaultCurrency = defaultCurrency;
    }

    internal void AddTransactions(IEnumerable<ICryptoCurrencyTransaction> transactions)
    {
        _transactions.AddRange(transactions);
    }

    // internal async Task<IEnumerable<CryptoCurrencyHolding>> GetHoldings2() 
    // {}

    internal async Task<IEnumerable<CryptoCurrencyHolding>> GetHoldings()
    {
        foreach (var tx in _transactions)
        {
            var lines = tx.State as KrakenCsvEntry[];
            var line = lines.First();
            var line2 = lines.ElementAtOrDefault(1);

            CryptoCurrencyHolding? sender = null;
            CryptoCurrencyHolding? receiver = null;

            if (tx is CryptoCurrencyDepositTransaction deposit)
            {
                receiver = GetOrCreateHolding(deposit.Amount.CurrencyCode);
                var addTxResult = receiver.AddTransaction(deposit);
                if (addTxResult.IsFailure)
                    throw new Exception(addTxResult.Error);

                // TODO, for crypto coins, get value at the time of deposit. Required for PL calcs.

                //Log.Debug($"Deposit [{string.Join("|", tx.TransactionIds)}: {receiver.Asset}: {receiver.Balance}");
                if (line.Balance.AbsoluteAmount != receiver.Balance)
                    Log.Error("Balances do not match");
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

                //Log.Debug($"Withdraw [{string.Join("|", tx.TransactionIds)}: {sender.Asset}: {sender.Balance}");
                if (line.Balance.AbsoluteAmount != sender.Balance)
                    Log.Error("Balances do not match");
            }

            if (tx is CryptoCurrencyTradeTransaction trade)
            {
                if (line2.ReferenceId == "TDAVEN-Q5IGP-SR3WD4")
                    ;

                receiver = GetOrCreateHolding(trade.Amount.CurrencyCode);
                var addTxResult = receiver.AddTransaction(trade);
                if (addTxResult.IsFailure)
                    throw new Exception(addTxResult.Error);
                CheckBalance(line2.Balance.Amount, receiver.Balance);

                sender = GetOrCreateHolding(trade.TradeAmount.CurrencyCode);
                var addTxResult2 = sender.AddTransaction(trade);
                if (addTxResult2.IsFailure)
                    throw new Exception(addTxResult2.Error);
                CheckBalance(line.Balance.Amount, sender.Balance);

                var isBuyWithFiat = trade.TradeAmount.IsFiatCurrency;
                if (isBuyWithFiat)
                {
                    if (!trade.Amount.IsFiatCurrency)
                    {
                        // Get all transactions involving this asset before this taxable event to calculate Average Buying Price.
                        var previousTrades = sender.Transactions
                            .Where(t => t is CryptoCurrencyTradeTransaction && t.Amount.CurrencyCode == trade.Amount.CurrencyCode)
                            .Cast<CryptoCurrencyTradeTransaction>()
                            .ToList();

                        var totalQty = previousTrades.Sum(t => t.Amount.Amount);

                        decimal totalCost = 0;
                        foreach (var t in previousTrades)
                        {
                            totalCost += await GetConvertedTradeAmountIfRequired(t);
                        }

                        var averageBuyingPrice = totalCost / totalQty;
                        var currentPrice = /* test */ averageBuyingPrice * (decimal)1.1;// GetHistoricalPriceAsync(sender.Asset, withdraw.DateTime, "usd").Result;
                        var profitLoss = currentPrice - averageBuyingPrice;
                        receiver.AverageBoughtPrice = averageBuyingPrice;
                    }
                }
                else
                    _taxableEvents.Add(trade);

                if (sender.Balance < 0)
                {
                    Log.Error($"{sender.Asset} Balance is under zero: {sender.Balance}");
                }

                // Log.Debug($"Trade Out [{string.Join("|", trade.TransactionIds)}: {sender.Asset}: {sender.Balance}");
                // Log.Debug($"Trade In [{string.Join("|", trade.TransactionIds)}: {receiver.Asset}: {receiver.Balance}");
            }
        }

        return _holdings;
    }

    public void CheckForMissingTransactions()
    {
        foreach (var holding in _holdings)
        {
            if(FiatCurrencies.Codes.Contains(holding.Asset))
                continue;

            decimal expectedTotal = 0;

            if(holding.Asset == "LINK")
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
                    if(trade.Amount.CurrencyCode == holding.Asset)                    
                        expectedTotal += trade.Amount.Amount;
                    else if(trade.TradeAmount.CurrencyCode == holding.Asset)
                    {
                        expectedTotal -= trade.TradeAmount.Amount;
                        if(trade.TradeAmount.CurrencyCode == trade.FeeAmount.CurrencyCode)
                            expectedTotal -= trade.FeeAmount.Amount;                            
                    }
                }
            }

            if(expectedTotal != holding.Balance)
                Log.Warning($"Missing transactions for holding {holding.Asset}");
        }

    }

    private async Task<decimal> GetConvertedTradeAmountIfRequired(CryptoCurrencyTradeTransaction trade)
    {
        // Before getting the sending holding, convert the trade amount to the user's chosen currency.                
        if (trade.TradeAmount.CurrencyCode.ToUpper() != DefaultCurrency)
        {
            // Call an exchange rate service to convert to USD
            MockCurrencyExchangeService exchangeRateService = new MockCurrencyExchangeService();
            var exchangeRate = await exchangeRateService.GetExchangeRateAsync(trade.TradeAmount.CurrencyCode, DefaultCurrency, trade.DateTime);
            return trade.TradeAmount.Amount / exchangeRate;
        }
        return trade.TradeAmount.Amount;
    }

    private static void CheckBalance(decimal csvLineBalance, decimal holdingBalance)
    {
        if (csvLineBalance != holdingBalance)
            Log.Error("Balances do not match");
    }

    internal CryptoCurrencyHolding GetOrCreateHolding(string currencyCode)
    {
        var receiver = _holdings.SingleOrDefault(h => h.Asset == currencyCode);
        if (receiver == null)
        {
            receiver = new CryptoCurrencyHolding(currencyCode);
            _holdings.Add(receiver);
        }
        return receiver;
    }

    internal void CalculateAverageBuyingCost(IEnumerable<CryptoCurrencyHolding> holdings)
    {

    }


    static async Task<decimal?> GetHistoricalPriceAsync(string cryptoId, DateTime date, string vsCurrency)
    {

        var options = new RestClientOptions($"https://api.coingecko.com/api/v3/coins/bitcoin/history?date={date.ToString("dd-MM-yyyy")}&localization=false&x_cg_demo_api_key=CG-t63vc5f31tfZKMZSYfvY3e5x");
        var client = new RestClient(options);
        var request = new RestRequest("");
        request.AddHeader("accept", "application/json");
        request.AddHeader("x-cg-demo-api-key", "CG-t63vc5f31tfZKMZSYfvY3e5x");
        var response = await client.GetAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string content = response.Content ?? string.Empty;
            JObject json = JObject.Parse(content);

            // Navigate through the JSON to get the price
            var marketData = json["market_data"];
            if (marketData != null)
            {
                var priceObject = marketData["current_price"];
                if (priceObject != null)
                {
                    return priceObject[vsCurrency.ToLower()]?.Value<decimal>();
                }
            }
        }

        // string baseUrl = "https://api.coingecko.com/api/v3";
        // string endpoint = $"/coins/{cryptoId}/history?date={date}&localization=false&x_cg_demo_api_key=CG-t63vc5f31tfZKMZSYfvY3e5x";

        // using (HttpClient client = new HttpClient())
        // {
        //     client.BaseAddress = new Uri(baseUrl);

        //     HttpResponseMessage response = await client.GetAsync(endpoint);
        //     if (response.IsSuccessStatusCode)
        //     {
        //         string content = await response.Content.ReadAsStringAsync();
        //         JObject json = JObject.Parse(content);

        //         // Navigate through the JSON to get the price
        //         var marketData = json["market_data"];
        //         if (marketData != null)
        //         {
        //             var priceObject = marketData["current_price"];
        //             if (priceObject != null)
        //             {
        //                 return priceObject[vsCurrency]?.Value<decimal>();
        //             }
        //         }
        //     }
        // }

        return null; // Return null if the price could not be retrieved
    }

}

internal class TaxableEvent
{
    public ICryptoCurrencyTransaction Transaction { get; set; }
    public decimal AverageBuyingPrice { get; set; }
    public decimal PriceAtEvent { get; set; }
}