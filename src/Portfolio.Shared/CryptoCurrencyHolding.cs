using CSharpFunctionalExtensions;
using Newtonsoft.Json.Linq;
using Portfolio.Shared;

namespace Portfolio;

public class CryptoCurrencyHolding
{
    public string Asset { get; init; }
    public decimal AverageBoughtPrice { get; set; }
    public decimal Balance { get; set; }
    public List<ICryptoCurrencyTransaction> Transactions { get; set; } = new();
    public decimal Fees { get; set; }

    public CryptoCurrencyHolding(string asset)
    {
        Asset = asset;
    }

    public Result AddTransaction(ICryptoCurrencyTransaction transaction)
    {
        var lastTransaction = Transactions.LastOrDefault();
        if (lastTransaction != null && transaction.DateTime < lastTransaction.DateTime)
            return Result.Failure("Transactions not in order. Ensure transactions are added in a chronological order.");

        if (transaction is CryptoCurrencyDepositTransaction deposit)
        {
            Balance += deposit.Amount.Amount;
            Fees += deposit.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
        {
            Balance -= transaction.Amount.Add(transaction.FeeAmount).Amount;
            Fees += transaction.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyTradeTransaction trade)
        {
            if (trade.Amount.CurrencyCode == Asset)
            {
                Balance += trade.Amount.Amount;
            }
            else if (trade.TradeAmount.CurrencyCode == Asset)
            {
                Balance -= trade.TradeAmount.Amount;
                if (trade.FeeAmount.CurrencyCode == Asset)
                {
                    Balance -= trade.FeeAmount.Amount;
                    Fees += trade.FeeAmount.Amount;
                }
            }
            else
                return Result.Failure("Invalid transaction for this holding.");


        }

        Transactions.Add(transaction);

        return Result.Success();
    }

    public Result RemoveTransaction(ICryptoCurrencyTransaction transaction)
    {
        if (transaction is CryptoCurrencyDepositTransaction deposit)
        {
            Balance -= deposit.Amount.Amount;
            Fees -= deposit.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
        {
            Balance += transaction.Amount.Add(transaction.FeeAmount).Amount;
            Fees -= transaction.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyTradeTransaction trade)
        {
            if (trade.Amount.CurrencyCode == Asset)
            {
                Balance -= trade.Amount.Amount;
            }
            else if (trade.TradeAmount.CurrencyCode == Asset)
            {
                Balance += trade.TradeAmount.Amount;
            }
            else
                return Result.Failure("Invalid transaction for this holding.");

            if (trade.FeeAmount.CurrencyCode == trade.Amount.CurrencyCode)
                Fees -= trade.FeeAmount.Amount;
            else if (trade.FeeAmount.CurrencyCode == trade.TradeAmount.CurrencyCode)
            {
                Balance += trade.FeeAmount.Amount;
                Fees -= trade.FeeAmount.Amount;
            }
        }

        Transactions.Remove(transaction);

        return Result.Success();
    }

    // public decimal CalculateAverageBuyingPrice()
    // {
    //     decimal balance = 0;
    //     decimal totalCost = 0;
    //     decimal averageBoughtPrice = 0;
    //     foreach(var tx in Transactions)
    //     {
    //         if(tx.ReceivedAmount != null && tx.ReceivedAmount.CurrencyCode == Asset)
    //         {
    //             totalCost += tx.ReceivedAmount.AbsoluteAmount; 

    //             balance += tx.ReceivedAmount.AbsoluteAmount;
    //             // averageBoughtPrice = totalCost / 
    //         }
    //     }

    //     //         // Define the cryptocurrency, date, and currency you want to get the value for
    //     // string cryptoId = "bitcoin"; // The ID for Bitcoin in CoinGecko API
    //     // string date = "01-01-2023";  // The date in dd-mm-yyyy format
    //     // string vsCurrency = "usd";   // The currency to compare against

    //     // // Call the function to get the historical price
    //     // decimal? price = await GetHistoricalPriceAsync(cryptoId, date, vsCurrency);


    // }

    //     static async Task<decimal?> GetHistoricalPriceAsync(string cryptoId, string date, string vsCurrency)
    // {
    //     string baseUrl = "https://api.coingecko.com/api/v3";
    //     string endpoint = $"/coins/{cryptoId}/history?date={date}&localization=false";

    //     using (HttpClient client = new HttpClient())
    //     {
    //         client.BaseAddress = new Uri(baseUrl);

    //         HttpResponseMessage response = await client.GetAsync(endpoint);
    //         if (response.IsSuccessStatusCode)
    //         {
    //             string content = await response.Content.ReadAsStringAsync();
    //             JObject json = JObject.Parse(content);

    //             // Navigate through the JSON to get the price
    //             var marketData = json["market_data"];
    //             if (marketData != null)
    //             {
    //                 var priceObject = marketData["current_price"];
    //                 if (priceObject != null)
    //                 {
    //                     return priceObject[vsCurrency]?.Value<decimal>();
    //                 }
    //             }
    //         }
    //     }

    //     return null; // Return null if the price could not be retrieved
    // }

}