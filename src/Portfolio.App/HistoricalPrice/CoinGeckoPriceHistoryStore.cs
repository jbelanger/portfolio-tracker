using CSharpFunctionalExtensions;
using Serilog;

namespace Portfolio.App.HistoricalPrice
{
    public class CoinGeckoPriceHistoryApi : IPriceHistoryApi
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.coingecko.com/api/v3";

        public CoinGeckoPriceHistoryApi(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Fetches historical price data from CoinGecko for a given symbol and date range.
        /// </summary>
        /// <param name="symbolPair">The trading pair symbol, e.g., "bitcoin/usd".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="CryptoPriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<CryptoPriceRecord>>> FetchDataAsync(string symbolPair, DateTime startDate, DateTime endDate)
        {
            try
            {
                Log.Information("Fetching data from CoinGecko for {SymbolPair} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.", symbolPair, startDate, endDate);

                var coinId = GetCoinGeckoId(symbolPair.Split('/')[0]);
                if (string.IsNullOrEmpty(coinId))
                {
                    return Result.Failure<IEnumerable<CryptoPriceRecord>>($"CoinGecko ID not found for symbol: {symbolPair}");
                }

                var priceData = new List<CryptoPriceRecord>();

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var timestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
                    var url = $"{BaseUrl}/coins/{coinId}/history?date={date:dd-MM-yyyy}";
                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Warning("Failed to fetch data from CoinGecko for {SymbolPair} on {Date:yyyy-MM-dd}. Status Code: {StatusCode}.", symbolPair, date, response.StatusCode);
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var jsonData = Newtonsoft.Json.Linq.JObject.Parse(content);
                    var price = jsonData["market_data"]?["current_price"]?[symbolPair.Split('/')[1].ToLower()]?.ToObject<decimal?>();

                    if (price.HasValue)
                    {
                        priceData.Add(new CryptoPriceRecord
                        {
                            CurrencyPair = symbolPair,
                            CloseDate = date,
                            ClosePrice = price.Value
                        });
                    }
                    else
                    {
                        Log.Warning("Price data not available for {SymbolPair} on {Date:yyyy-MM-dd}.", symbolPair, date);
                    }
                }

                if (priceData.Any())
                {
                    Log.Information("Successfully retrieved price data from CoinGecko for {SymbolPair}.", symbolPair);
                    return Result.Success<IEnumerable<CryptoPriceRecord>>(priceData);
                }
                else
                {
                    return Result.Failure<IEnumerable<CryptoPriceRecord>>("No price data retrieved from CoinGecko.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching data from CoinGecko for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<CryptoPriceRecord>>("An error occurred while fetching data from CoinGecko.");
            }
        }

        /// <summary>
        /// Determines the appropriate CoinGecko ID based on the cryptocurrency symbol.
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol (e.g., "bitcoin").</param>
        /// <returns>The corresponding CoinGecko ID (e.g., "bitcoin").</returns>
        private string GetCoinGeckoId(string symbol)
        {
            // In a production scenario, you'd typically map these from a more extensive and dynamic source.
            return symbol.ToLower() switch
            {
                "btc" => "bitcoin",
                "eth" => "ethereum",
                "xrp" => "ripple",
                "ltc" => "litecoin",
                "ada" => "cardano",
                // Add more mappings as needed
                _ => string.Empty
            };
        }

        /// <summary>
        /// Determines the appropriate trading pair symbol for CoinGecko.
        /// </summary>
        /// <param name="fromSymbol">The base currency or coin symbol.</param>
        /// <param name="toSymbol">The quote currency or coin symbol.</param>
        /// <returns>The trading pair symbol in the format "base/quote" (e.g., "bitcoin/usd").</returns>
        public string DetermineTradingPair(string fromSymbol, string toSymbol)
        {
            return $"{fromSymbol.ToLower()}/{toSymbol.ToLower()}";
        }
    }
}
