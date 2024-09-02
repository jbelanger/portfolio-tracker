using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Portfolio.App.Common.Interfaces;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.HistoricalPrice.CoinGecko
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryApi"/> that retrieves historical cryptocurrency price data from the CoinGecko API.
    /// </summary>
    public class CoinGeckoPriceHistoryApi : IPriceHistoryApi
    {
        private readonly HttpClient _httpClient;
        private readonly MemoryCache _cache;
        private readonly IApplicationDbContext dbContext;
        private readonly RateLimiter _rateLimiter;
        private CoinGeckoCoinListApi _coinGeckoCoinListApi;
        private int RetryAfter => 60 / _rateLimiter.RequestsPerMinute;

        public int RequestsPerMinute
        {
            get => _rateLimiter.RequestsPerMinute;
            set => _rateLimiter.UpdateRequestsPerMinute(value);
        }

        public CoinGeckoPriceHistoryApi(IHttpClientFactory httpClientFactory, MemoryCache cache, IApplicationDbContext dbContext)
        {
            _httpClient = httpClientFactory.CreateClient("CoinGeckoClient") ?? throw new InvalidOperationException(nameof(httpClientFactory.CreateClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _rateLimiter = new RateLimiter(15); // Default to 15 requests per minute

            _coinGeckoCoinListApi = new CoinGeckoCoinListApi(httpClientFactory, cache, dbContext) { RequestsPerMinute = RequestsPerMinute };
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
            await _rateLimiter.EnsureRateLimitAsync();

            // Log the beginning of the data fetch operation
            Log.ForContext<CoinGeckoPriceHistoryApi>().Information("Initiating data fetch for {SymbolPair} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.", symbol, startDate, endDate);

            var coinInfoResult = await _coinGeckoCoinListApi.FindBestMatchBySymbol(symbol);
            if (coinInfoResult.IsFailure)
            {
                Log.Error("Coin ID for symbol {Symbol} could not be found.", symbol);
                return Result.Failure<IEnumerable<PriceRecord>>(coinInfoResult.Error);
            }

            var coinInfo = coinInfoResult.Value;

            var uri = $"https://api.coingecko.com/api/v3/coins/{coinInfo.CoinId}/market_chart/range?vs_currency={currency.ToLower()}&from={new DateTimeOffset(startDate).ToUnixTimeSeconds()}&to={new DateTimeOffset(endDate).ToUnixTimeSeconds()}";
            var response = await RetryUtility.RetryAsync(
                () => _httpClient.GetAsync(uri),
                maxRetries: 3,
                delay: TimeSpan.FromSeconds(RetryAfter)).ConfigureAwait(false);

            if (response.IsFailure || !response.Value.IsSuccessStatusCode)
            {
                Log.Error("Failed to fetch data for {SymbolPair}. HTTP status: {StatusCode}", symbol, response.Value.StatusCode);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }

            var content = await response.Value.Content.ReadAsStringAsync().ConfigureAwait(false);
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

        /// <summary>
        /// Fetches the current price for a given cryptocurrency symbol from the CoinGecko API.
        /// </summary>
        /// <param name="symbols">The list of symbols, e.g., ["BTC", "ETH"]</param>
        /// <param name="currency">The currency, e.g., "USD"</param>
        /// <returns>A <see cref="Result{T}"/> containing the current price as a <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchCurrentPriceAsync(IEnumerable<string> symbols, string currency)
        {
            await _rateLimiter.EnsureRateLimitAsync();

            var coinInfoResult = await _coinGeckoCoinListApi.FindBestMatchBySymbols(symbols);
            if (coinInfoResult.IsFailure)
                return Result.Failure<IEnumerable<PriceRecord>>(coinInfoResult.Error);

            var coinInfos = coinInfoResult.Value;
            var coinIdsString = string.Join(",", coinInfos.Select(i => i.CoinId));
            var currencySymbol = currency.ToLower();

            var uri = $"https://api.coingecko.com/api/v3/simple/price?ids={coinIdsString}&vs_currencies={currencySymbol}";
            var response = await RetryUtility.RetryAsync(
                () => _httpClient.GetAsync(uri),
                maxRetries: 3,
                delay: TimeSpan.FromSeconds(RetryAfter)).ConfigureAwait(false);

            if (response.IsFailure || !response.Value.IsSuccessStatusCode)
            {
                Log.Error("Failed to fetch current price for {symbolIdsString}. HTTP status: {StatusCode}", coinIdsString, response.Value.StatusCode);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_COINGECKO_API_FETCH_FAILURE);
            }

            var content = await response.Value.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JObject.Parse(content);

            List<PriceRecord> priceRecords = new List<PriceRecord>();
            foreach (var s in coinInfos)
            {
                var price = json[s.CoinId]?[currencySymbol]?.Value<decimal>();
                if (price == null)
                {
                    Log.Warning("Price data not found for {CurrencySymbol}.", currencySymbol);
                    continue;
                }
                else
                {
                    var priceRecord = new PriceRecord
                    {
                        CurrencyPair = $"{s.Symbol.ToUpper()}/{currency.ToUpper()}",
                        CloseDate = DateTime.UtcNow,
                        ClosePrice = price.Value
                    };
                    
                    priceRecords.Add(priceRecord);
                }
            }

            return Result.Success(priceRecords.AsEnumerable());
        }
    }
}
