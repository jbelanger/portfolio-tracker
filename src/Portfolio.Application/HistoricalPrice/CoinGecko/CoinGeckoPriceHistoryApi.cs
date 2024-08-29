using Microsoft.Extensions.Caching.Memory;
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
        private readonly MemoryCache _errorCache = new MemoryCache(new MemoryCacheOptions());

        private readonly MemoryCache _cache;

        private int _errorCacheDurationMinutes = 5;
        private int _requestsPerMinute = 15;

        private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        public int RequestsPerMinute
        {
            get => _requestsPerMinute;
            set
            {
                if (value > 0)
                    _requestsPerMinute = value;
                throw new InvalidOperationException("RequestsPerMinute must be greater than 0.");
            }
        }

        public int ErrorCacheDurationMinutes
        {
            get => _errorCacheDurationMinutes;
            set
            {
                if (value > 0)
                    _errorCacheDurationMinutes = value;
                throw new InvalidOperationException("ErrorCacheInMinutes must be greater than 0.");
            }
        }

        public CoinGeckoPriceHistoryApi(IHttpClientFactory httpClientFactory, MemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("CoinGeckoClient") ?? throw new InvalidOperationException(nameof(httpClientFactory.CreateClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
        public async Task<Result<Dictionary<string, CoinInfo>>> GetAllSupportedCoinIdsAsync()
        {
            try
            {
                var cacheKey = "CoinGecko_CoinInfo";
                Dictionary<string, CoinInfo>? cachedCoinInfo;

                // Check if the data is already cached
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(cacheKey, out cachedCoinInfo))
                    {
                        return Result.Success(cachedCoinInfo!);
                    }
                }

                // Fetch data from the markets API with pagination
                var coinDictionary = new Dictionary<string, CoinInfo>();
                var fetchResult = await FetchPaginatedCoinDataAsync(coinDictionary, 1, 1000, ErrorCacheDurationMinutes);

                if (!fetchResult.IsSuccess)
                {
                    return fetchResult;
                }

                // Cache the result until midnight UTC
                lock (_cacheLock)
                {
                    CacheCoinInfo(cacheKey, coinDictionary);
                }

                return Result.Success(coinDictionary);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while fetching supported coins from CoinGecko.");
                return Result.Failure<Dictionary<string, CoinInfo>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }
        }

        private async Task<Result<Dictionary<string, CoinInfo>>> FetchPaginatedCoinDataAsync(Dictionary<string, CoinInfo> coinDictionary, int currentPage, int totalCoins, int errorCacheDurationMinutes, int pageSize = 250)
        {
            var totalPages = (int)Math.Ceiling(totalCoins / (double)pageSize);

            for (int page = currentPage; page <= totalPages; page++)
            {
                await EnsureRateLimitAsync();

                var uriMarkets = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={pageSize}&page={page}";

                // Check error cache to prevent unnecessary queries
                if (IsInErrorCache(uriMarkets))
                {
                    Log.Warning("Skipping request for page {Page} due to recent error.", page);
                    return Result.Failure<Dictionary<string, CoinInfo>>("Coin market data is not available at this time. Please try again later.");
                }

                var response = await _httpClient.GetAsync(uriMarkets).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch supported coins from CoinGecko markets API. HTTP status: {StatusCode} for page {Page}", response.StatusCode, page);
                    AddToErrorCache(uriMarkets, errorCacheDurationMinutes);
                    return Result.Failure<Dictionary<string, CoinInfo>>("Coin market data is not available at this time. Please try again later.");
                }

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JArray.Parse(content);

                var pageData = ParseCoinInfo(json);
                foreach (var coin in pageData)
                {
                    if (!coinDictionary.ContainsKey(coin.Key))
                    {
                        coinDictionary[coin.Key] = coin.Value;
                    }
                }
            }

            return Result.Success<Dictionary<string, CoinInfo>>(coinDictionary);
        }

        private bool TryGetCachedCoinInfo(string cacheKey, out Dictionary<string, CoinInfo>? cachedCoinInfo)
        {
            return _cache.TryGetValue(cacheKey, out cachedCoinInfo);
        }

        private async Task<Dictionary<string, CoinInfo>?> FetchCoinDataFromMarketsApiAsync()
        {
            var uriMarkets = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd";
            var response = await _httpClient.GetAsync(uriMarkets).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Failed to fetch supported coins from CoinGecko markets API. HTTP status: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JArray.Parse(content);

            return ParseCoinInfo(json);
        }

        private async Task<Dictionary<string, CoinInfo>> FetchCoinDataFromListApiAsync(IEnumerable<string> missingSymbols)
        {
            var uriList = "https://api.coingecko.com/api/v3/coins/list";
            var response = await _httpClient.GetAsync(uriList).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Failed to fetch supported coins from CoinGecko list API. HTTP status: {StatusCode}", response.StatusCode);
                return new Dictionary<string, CoinInfo>();
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JArray.Parse(content);

            return ParseFallbackCoinInfo(json, missingSymbols);
        }

        private Dictionary<string, CoinInfo> ParseCoinInfo(JArray json)
        {
            var coinDictionary = new Dictionary<string, CoinInfo>();

            foreach (var coin in json)
            {
                var symbol = coin["symbol"]?.Value<string>()?.ToLower();
                var id = coin["id"]?.Value<string>();

                if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(id))
                {
                    coinDictionary[symbol] = new CoinInfo
                    {
                        Id = id,
                        Symbol = symbol,
                        Name = coin["name"]?.Value<string>() ?? string.Empty,
                        Image = coin["image"]?.Value<string>() ?? string.Empty,
                        CurrentPrice = SafeConvertToDecimal(coin["current_price"]),
                        MarketCap = SafeConvertToDecimal(coin["market_cap"]),
                        MarketCapRank = coin["market_cap_rank"]?.Value<int?>(),
                        FullyDilutedValuation = SafeConvertToDecimal(coin["fully_diluted_valuation"]),
                        TotalVolume = SafeConvertToDecimal(coin["total_volume"]),
                        High24h = SafeConvertToDecimal(coin["high_24h"]),
                        Low24h = SafeConvertToDecimal(coin["low_24h"]),
                        PriceChange24h = SafeConvertToDecimal(coin["price_change_24h"]),
                        PriceChangePercentage24h = SafeConvertToDecimal(coin["price_change_percentage_24h"]),
                        MarketCapChange24h = SafeConvertToDecimal(coin["market_cap_change_24h"]),
                        MarketCapChangePercentage24h = SafeConvertToDecimal(coin["market_cap_change_percentage_24h"]),
                        CirculatingSupply = SafeConvertToDecimal(coin["circulating_supply"]),
                        TotalSupply = SafeConvertToDecimal(coin["total_supply"]),
                        MaxSupply = SafeConvertToDecimal(coin["max_supply"]),
                        Ath = SafeConvertToDecimal(coin["ath"]),
                        AthChangePercentage = SafeConvertToDecimal(coin["ath_change_percentage"]),
                        AthDate = coin["ath_date"]?.Value<DateTime?>(),
                        Atl = SafeConvertToDecimal(coin["atl"]),
                        AtlChangePercentage = SafeConvertToDecimal(coin["atl_change_percentage"]),
                        AtlDate = coin["atl_date"]?.Value<DateTime?>(),
                        LastUpdated = coin["last_updated"]?.Value<string>() ?? string.Empty
                    };
                }
            }

            return coinDictionary;
        }

        private Dictionary<string, CoinInfo> ParseFallbackCoinInfo(JArray json, IEnumerable<string> missingSymbols)
        {
            var fallbackCoinData = new Dictionary<string, CoinInfo>();

            foreach (var coin in json)
            {
                var symbol = coin["symbol"]?.Value<string>()?.ToLower();
                var id = coin["id"]?.Value<string>();

                if (!string.IsNullOrWhiteSpace(symbol) && missingSymbols.Contains(symbol) && !string.IsNullOrWhiteSpace(id))
                {
                    fallbackCoinData[symbol] = new CoinInfo
                    {
                        Id = id,
                        Symbol = symbol,
                        Name = coin["name"]?.Value<string>() ?? string.Empty
                    };
                }
            }

            return fallbackCoinData;
        }

        private decimal? SafeConvertToDecimal(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            try
            {
                return token.Value<decimal?>();
            }
            catch (OverflowException)
            {
                Log.Warning("Overflow when converting {TokenValue} to decimal.", token);
                return null; // Returning null to handle the overflow scenario
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error when converting {TokenValue} to decimal.", token);
                return null; // Returning null for any other conversion error
            }
        }

        private void AddToErrorCache(string uri, int errorCacheDurationMinutes)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(errorCacheDurationMinutes));
            _errorCache.Set(uri, true, cacheEntryOptions);
        }

        private bool IsInErrorCache(string uri)
        {
            return _errorCache.TryGetValue(uri, out _);
        }

        private List<string> GetMissingSymbols(Dictionary<string, CoinInfo> coinDictionary)
        {
            // Implement logic to determine which symbols are missing
            return new List<string>(); // Placeholder, implement as needed
        }

        private void CacheCoinInfo(string cacheKey, Dictionary<string, CoinInfo> coinDictionary)
        {
            var cacheExpiration = DateTime.UtcNow.Date.AddDays(1); // Midnight UTC
            _cache.Set(cacheKey, coinDictionary, cacheExpiration);
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
                return Result.Success(coinId.Id);
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
