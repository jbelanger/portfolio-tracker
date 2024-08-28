using Newtonsoft.Json.Linq;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Portfolio.App.HistoricalPrice.CoinGecko
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryApi"/> that retrieves historical cryptocurrency price data from the CoinGecko API.
    /// </summary>
    public class CoinGeckoPriceHistoryApi : IPriceHistoryApi
    {
        private readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, bool> _invalidSymbolsCache = new();
        private static Dictionary<string, string>? _cachedCoinIds;
        private static readonly object _cacheLock = new();

        private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;
        
        public int RequestsPerMinute {get; set;}

        public CoinGeckoPriceHistoryApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Fetches historical price data for a given cryptocurrency symbol and date range from the CoinGecko API.
        /// </summary>
        /// <param name="symbol">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbol, string currency, DateTime startDate, DateTime endDate)
        {
            try
            {
                await EnsureRateLimitAsync();

                // Log the beginning of the data fetch operation
                Log.ForContext<CoinGeckoPriceHistoryApi>().Information("Initiating data fetch for {SymbolPair} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.", symbol, startDate, endDate);

                var coinIdResult = await GetCoinIdAsync(symbol);

                if (coinIdResult.IsFailure)
                {
                    Log.Error("Coin ID for symbol {Symbol} could not be found.", symbol);
                    return Result.Failure<IEnumerable<PriceRecord>>(coinIdResult.Error);
                }

                var coinId = coinIdResult.Value;

                var uri = $"https://api.coingecko.com/api/v3/coins/{coinId}/market_chart/range?vs_currency={currency.ToLower()}&from={new DateTimeOffset(startDate).ToUnixTimeSeconds()}&to={new DateTimeOffset(endDate).ToUnixTimeSeconds()}";
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch data for {SymbolPair}. HTTP status: {StatusCode}", symbol, response.StatusCode);
                    return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JObject.Parse(content);

                if (json["prices"] == null)
                {
                    Log.Error("Invalid JSON returned for {Symbol}-{Currency}.", symbol, currency);
                    return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
                }

                List<PriceRecord> priceData = json["prices"]!
                                .Select(x => new PriceRecord
                                {
                                    CurrencyPair = $"{symbol.ToUpper()}/{currency.ToUpper()}",
                                    CloseDate = DateTimeOffset.FromUnixTimeMilliseconds((long)x[0]).DateTime,
                                    ClosePrice = x[1].Value<decimal>()
                                })
                                .ToList();

                // Log the time taken to fetch the data
                Log.Information("Data fetch for {SymbolPair} completed. Retrieved {RecordsCount} day(s) of data.", symbol, priceData.Count);

                return Result.Success(priceData.AsEnumerable());
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred while fetching data for {SymbolPair}.", symbol);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
            catch (TimeoutException timeoutEx)
            {
                // Log timeout errors separately
                Log.Error(timeoutEx, "Timeout occurred while fetching data for {SymbolPair}.", symbol);
                return Result.Failure<IEnumerable<PriceRecord>>("Timeout while fetching data.");
            }
            catch (Exception ex)
            {
                // General catch-all for unexpected errors
                Log.Error(ex, "Unexpected error in {MethodName} for {SymbolPair}.", nameof(FetchPriceHistoryAsync), symbol);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
        }

        /// <summary>
        /// Fetches the current price for a given cryptocurrency symbol from the CoinGecko API.
        /// </summary>
        /// <param name="symbols">The list of symbols, e.g., ["BTC", "ETH"]</param>
        /// <param name="currency">The currency, e.g., "USD"</param>
        /// <returns>A <see cref="Result{T}"/> containing the current price as a <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchCurrentPriceAsync(IEnumerable<string> symbols, string currency)
        {
            try
            {
                await EnsureRateLimitAsync();

                List<Tuple<string, string>> symbolsIds = new();
                foreach (var s in symbols)
                {
                    var coinIdResult = await GetCoinIdAsync(s);
                    if (coinIdResult.IsFailure)
                    {
                        Log.Warning("Coin ID for symbol {Symbol} could not be found.", s);
                        continue;
                    }
                    symbolsIds.Add(new(s, coinIdResult.Value));
                }

                var symbolIdsString = string.Join(",", symbolsIds.Select(i => i.Item2));
                var currencySymbol = currency.ToLower();

                var uri = $"https://api.coingecko.com/api/v3/simple/price?ids={symbolIdsString}&vs_currencies={currencySymbol}";
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch current price for {symbolIdsString}. HTTP status: {StatusCode}", symbolIdsString, response.StatusCode);
                    return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JObject.Parse(content);

                List<PriceRecord> priceRecords = new List<PriceRecord>();
                foreach (var s in symbolsIds)
                {
                    var price = json[s.Item2]?[currencySymbol]?.Value<decimal>();
                    if (price == null)
                    {
                        Log.Warning("Price data not found for {CurrencySymbol}.", currencySymbol);
                        continue;
                    }
                    else
                    {
                        var priceRecord = new PriceRecord
                        {
                            CurrencyPair = $"{s.Item1.ToUpper()}/{currency.ToUpper()}",
                            CloseDate = DateTime.UtcNow,
                            ClosePrice = price.Value
                        };
                        priceRecords.Add(priceRecord);
                    }
                }

                return Result.Success(priceRecords.AsEnumerable());
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred while fetching current price for {symbols}.", symbols);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
            catch (TimeoutException timeoutEx)
            {
                // Log timeout errors separately
                Log.Error(timeoutEx, "Timeout occurred while fetching current price for {symbols}.", symbols);
                return Result.Failure<IEnumerable<PriceRecord>>("Timeout while fetching data.");
            }
            catch (Exception ex)
            {
                // General catch-all for unexpected errors
                Log.Error(ex, "Unexpected error in {MethodName} for {symbols}.", nameof(FetchCurrentPriceAsync), symbols);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
        }

        /// <summary>
        /// Retrieves a dictionary of all supported coins and their corresponding IDs from the CoinGecko API.
        /// </summary>
        /// <returns>A <see cref="Result{T}"/> containing a dictionary where the key is the symbol and the value is the CoinGecko ID, or an error message.</returns>
        public async Task<Result<Dictionary<string, string>>> GetAllSupportedCoinIdsAsync()
        {
            try
            {
                // Check if the cache is populated
                if (_cachedCoinIds != null)
                {
                    return Result.Success(_cachedCoinIds);
                }

                lock (_cacheLock)
                {
                    if (_cachedCoinIds != null) // Double-checked locking
                    {
                        return Result.Success(_cachedCoinIds);
                    }
                }

                var uri = "https://api.coingecko.com/api/v3/coins/list";
                var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch supported coins from CoinGecko. HTTP status: {StatusCode}", response.StatusCode);
                    return Result.Failure<Dictionary<string, string>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JArray.Parse(content);

                var coinDictionary = new Dictionary<string, string>();
                foreach (var coin in json)
                {
                    var symbol = coin["symbol"]?.Value<string>()?.ToLower();
                    var id = coin["id"]?.Value<string>();

                    if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(id) && !coinDictionary.ContainsKey(symbol))
                        coinDictionary.Add(symbol, id);
                }

                // Cache the result
                lock (_cacheLock)
                {
                    _cachedCoinIds = coinDictionary;
                }

                return Result.Success(coinDictionary);
            }
            catch (Exception ex)
            {
                _cachedCoinIds = new Dictionary<string, string>(); // Avoid trying again

                Log.Error(ex, "An error occurred while fetching supported coins from CoinGecko.");
                return Result.Failure<Dictionary<string, string>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
        }

        /// <summary>
        /// Gets the CoinGecko ID for a given symbol.
        /// </summary>
        /// <param name="symbol">The cryptocurrency symbol, e.g., "btc".</param>
        /// <returns>A <see cref="Result{T}"/> containing the CoinGecko ID for the symbol, or an error message.</returns>
        public async Task<Result<string>> GetCoinIdAsync(string symbol)
        {
            // Check if the symbol is known to be invalid
            if (_invalidSymbolsCache.ContainsKey(symbol.ToLower()))
            {
                return Result.Failure<string>($"Coin ID for symbol {symbol} could not be found.");
            }

            // Try to get from the cache first
            if (_cachedCoinIds != null && _cachedCoinIds.TryGetValue(symbol.ToLower(), out var cachedCoinId))
            {
                return Result.Success(cachedCoinId);
            }

            // If not in cache, fetch all coin IDs and cache them
            var supportedCoinsResult = await GetAllSupportedCoinIdsAsync();
            if (supportedCoinsResult.IsFailure)
            {
                return Result.Failure<string>(supportedCoinsResult.Error);
            }

            var supportedCoins = supportedCoinsResult.Value;
            if (supportedCoins.TryGetValue(symbol.ToLower(), out var coinId))
            {
                return Result.Success(coinId);
            }

            // Cache the invalid symbol
            _invalidSymbolsCache.TryAdd(symbol.ToLower(), true);

            return Result.Failure<string>($"Coin ID for symbol {symbol} could not be found.");
        }

        /// <summary>
        /// Ensures that the rate limit of the API is respected.
        /// </summary>
        private async Task EnsureRateLimitAsync()
        {
            await _rateLimitSemaphore.WaitAsync();

            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                var delay = TimeSpan.FromMinutes(1.0 / RequestsPerMinute);

                if (timeSinceLastRequest < delay)
                {
                    await Task.Delay(delay - timeSinceLastRequest);
                }

                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }
    }
}
