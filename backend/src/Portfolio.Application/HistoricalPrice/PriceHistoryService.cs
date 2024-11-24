using Microsoft.Extensions.Caching.Memory;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.HistoricalPrice
{
    /// <summary>
    /// Provides functionality for retrieving and storing historical price data for cryptocurrencies.
    /// </summary>
    public class PriceHistoryService : IPriceHistoryService
    {
        private readonly IPriceHistoryApi _priceHistoryApi;
        private readonly IPriceHistoryStorageService _priceHistoryStorage;
        private readonly MemoryCache _cache;
        private readonly object _lock = new();

        /// <summary>
        /// Gets or sets the default currency symbol used in price calculations.
        /// </summary>
        public string DefaultCurrency { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriceHistoryService"/> class.
        /// </summary>
        /// <param name="priceHistoryApi">The API used to fetch historical price data.</param>
        /// <param name="priceHistoryStorage">The storage service used to save and load historical price data.</param>
        /// <param name="cache">The memory cache instance used for caching results.</param>
        /// <param name="defaultCurrencySymbol">The default currency symbol (e.g., "USD").</param>
        public PriceHistoryService(
            IPriceHistoryApi priceHistoryApi,
            IPriceHistoryStorageService priceHistoryStorage,
            MemoryCache cache,
            string defaultCurrencySymbol = Strings.CURRENCY_USD)
        {
            _priceHistoryApi = priceHistoryApi ?? throw new ArgumentNullException(nameof(priceHistoryApi));
            _priceHistoryStorage = priceHistoryStorage ?? throw new ArgumentNullException(nameof(priceHistoryStorage));          
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));

            DefaultCurrency = defaultCurrencySymbol ?? throw new ArgumentNullException(nameof(defaultCurrencySymbol));
        }

        /// <summary>
        /// Retrieves the closing price of a specific cryptocurrency symbol at a specified date.
        /// If the requested symbol matches the default currency, an error is returned.
        /// If no price is found for the specified date, the service attempts to fetch it from the API,
        /// or recursively checks up to four previous days for fiat currencies.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC").</param>
        /// <param name="date">The date and time for which to retrieve the closing price.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        public async Task<Result<decimal>> GetPriceAtCloseTimeAsync(string symbol, DateTime date)
        {
            if (symbol == DefaultCurrency)
                return HandleDefaultCurrencyError(symbol);

            var priceResult = await _priceHistoryStorage.GetPriceAsync(symbol, date).ConfigureAwait(false);

            if (priceResult.IsSuccess)
            {
                return priceResult.Value.ClosePrice;
            }

            if (FiatCurrency.All.Any(f => f == symbol))
            {
                var handleMissingFiatResult = await HandleMissingFiatDataAsync(symbol, date, 4).ConfigureAwait(false);
                if (handleMissingFiatResult.IsSuccess)
                {
                    return handleMissingFiatResult.Value;
                }
            }

            return await FetchAndSavePriceData(symbol, date).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the case where the requested symbol matches the default currency.
        /// Returns an error to indicate that fetching the price for the same symbol as the default currency is not allowed.
        /// </summary>
        /// <param name="symbol">The currency symbol.</param>
        /// <returns>A <see cref="Result{T}"/> indicating a failure due to the same symbol as the default currency.</returns>
        private Result<decimal> HandleDefaultCurrencyError(string symbol)
        {
            Log.ForContext<PriceHistoryService>().Warning("Attempted to fetch price for the same symbol as the default currency ({Symbol}).", symbol);
            return Result.Failure<decimal>(string.Format(Errors.ERR_SAME_SYMBOLS, symbol, DefaultCurrency));
        }

        /// <summary>
        /// Recursively checks for the availability of fiat currency data for the given symbol and date.
        /// If no data is found for the specified date, it will check up to four previous days.
        /// </summary>
        /// <param name="symbol">The currency symbol.</param>
        /// <param name="date">The date for which to check for price data.</param>
        /// <param name="daysToCheck">The number of previous days to check if data is missing.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        private async Task<Result<decimal>> HandleMissingFiatDataAsync(string symbol, DateTime date, int daysToCheck = 4)
        {
            if (daysToCheck <= 0)
            {
                Log.ForContext<PriceHistoryService>().Debug("No available fiat data for {Symbol} on {Date:yyyy-MM-dd} or previous days.", symbol);
                return Result.Failure<decimal>($"No price data available for {symbol} on {date:yyyy-MM-dd} or previous days.");
            }

            Log.ForContext<PriceHistoryService>().Debug("Missing fiat data for {Symbol} on {Date:yyyy-MM-dd}. Checking previous day...", symbol, date);

            var previousDate = date.AddDays(-1);
            var priceResult = await _priceHistoryStorage.GetPriceAsync(symbol, previousDate).ConfigureAwait(false);

            if (priceResult.IsSuccess)
            {
                Log.ForContext<PriceHistoryService>().Debug("Fiat data found for {Symbol} on {PreviousDate:yyyy-MM-dd}.", symbol, previousDate);
                return Result.Success(priceResult.Value.ClosePrice);
            }

            // Recurse to check the previous day
            return await HandleMissingFiatDataAsync(symbol, previousDate, daysToCheck - 1).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches new price data from the API for the specified symbol and date range, and saves it to storage.
        /// If data is successfully fetched and saved, it returns the price for the requested date.
        /// If the price is still not found, it attempts to handle missing fiat data.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency.</param>
        /// <param name="date">The date for which data is being fetched.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        private async Task<Result<decimal>> FetchAndSavePriceData(string symbol, DateTime date)
        {            
            var endDate = AdjustEndDate(date);
            var result = await _priceHistoryApi.FetchPriceHistoryAsync(symbol, DefaultCurrency, date.Date, endDate).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await _priceHistoryStorage.SaveHistoryAsync(symbol, result.Value).ConfigureAwait(false);

                var matchingRecord = result.Value.FirstOrDefault(r => r.CloseDate.Date == date.Date);
                if (matchingRecord != null)
                {
                    return matchingRecord.ClosePrice;
                }

                if (FiatCurrency.All.Any(f => f == symbol))
                {
                    var handleMissingFiatResult = await HandleMissingFiatDataAsync(symbol, date);
                    if (handleMissingFiatResult.IsSuccess)
                        return handleMissingFiatResult.Value;
                }
            }

            return Result.Failure<decimal>("Error retrieving or saving price data.");
        }

        /// <summary>
        /// Adjusts the end date for fetching historical data, ensuring it does not exceed the current date.
        /// </summary>
        /// <param name="date">The start date for the data range.</param>
        /// <returns>The adjusted end date.</returns>
        private DateTime AdjustEndDate(DateTime date)
        {
            var endDate = date.AddDays(365);
            return endDate > DateTime.Now ? DateTime.Now.AddDays(1) : endDate;
        }

        /// <summary>
        /// Retrieves the current prices for the specified cryptocurrency symbols.
        /// Fetches the prices from the API and caches the results for one minute to prevent exceeding API rate limits.
        /// If cached data is available, it returns the cached data.
        /// </summary>
        /// <param name="symbols">A collection of cryptocurrency symbols to retrieve prices for.</param>
        /// <returns>A <see cref="Result{T}"/> containing a dictionary of symbols and their corresponding prices, or an error message.</returns>
        public async Task<Result<Dictionary<string, decimal>>> GetCurrentPricesAsync(IEnumerable<string> symbols)
        {
            var cacheKey = $"CurrentPrices_{string.Join("_", symbols.OrderBy(s => s))}";
            
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, decimal>? cachedPrices))
            {
                return Result.Success(cachedPrices!);
            }

            Dictionary<string, decimal> found = new();
            
            lock (_lock)
            {
                if (_cache.TryGetValue(cacheKey, out cachedPrices!))
                {
                    return Result.Success(cachedPrices);
                }
            }

            var result = await _priceHistoryApi.FetchCurrentPriceAsync(symbols, DefaultCurrency).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                foreach (var s in symbols)
                {
                    var priceRecord = result.Value.FirstOrDefault(p => p.CurrencyPair.Split("/")[0] == s);
                    if (priceRecord != null)
                        found.Add(s, priceRecord.ClosePrice);
                }

                lock (_lock)
                {
                    _cache.Set(cacheKey, found, TimeSpan.FromMinutes(1));
                }

                return Result.Success(found);
            }

            return Result.Failure<Dictionary<string, decimal>>(result.Error);
        }
    }
}
