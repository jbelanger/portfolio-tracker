using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
        private ConcurrentDictionary<string, Lazy<Task<ReadOnlyDictionary<DateTime, PriceRecord>>>> _dataStores = new();
        private readonly MemoryCache _cache;


        /// <summary>
        /// Gets or sets the default currency symbol used in price calculations.
        /// </summary>
        public string DefaultCurrency { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriceHistoryService"/> class.
        /// </summary>
        /// <param name="priceHistoryApi">The API used to fetch historical price data.</param>
        /// <param name="priceHistoryStorage">The storage service used to save and load historical price data.</param>
        /// <param name="defaultCurrencySymbol">The default currency symbol (e.g., "USD").</param>
        public PriceHistoryService(
            IPriceHistoryApi priceHistoryApi,
            IPriceHistoryStorageService priceHistoryStorage,
            MemoryCache cache,
            string defaultCurrencySymbol = Strings.CURRENCY_USD)
        {
            _priceHistoryApi = priceHistoryApi ?? throw new ArgumentNullException(nameof(priceHistoryApi));
            _priceHistoryStorage = priceHistoryStorage ?? throw new ArgumentNullException(nameof(priceHistoryStorage));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));  // Assign the cache


            DefaultCurrency = defaultCurrencySymbol ?? throw new ArgumentNullException(nameof(defaultCurrencySymbol));
        }

        /// <summary>
        /// Retrieves the closing price of a specific cryptocurrency symbol at a specified date.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC").</param>
        /// <param name="date">The date and time for which to retrieve the closing price.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        public async Task<Result<decimal>> GetPriceAtCloseTimeAsync(string symbol, DateTime date)
        {
            var dateOnly = date.Date;

            if (symbol == DefaultCurrency)
                return HandleDefaultCurrencyError(symbol);

            // Check if today's data is cached
            if (dateOnly == DateTime.Today)
            {
                var cacheKey = GetCacheKey(symbol, dateOnly);
                if (_cache.Get(cacheKey) is decimal cachedPrice)
                {
                    return Result.Success(cachedPrice);
                }
            }

            var history = await LoadPriceHistoryFromStorageAsync(symbol).ConfigureAwait(false);
            if (history.Any())
            {
                if (history.ContainsKey(dateOnly))
                {
                    var closePrice = history[dateOnly].ClosePrice;

                    if (dateOnly == DateTime.Today)
                    {
                        CacheTodayPrice(symbol, dateOnly, closePrice);
                    }

                    return closePrice;
                }

                if (FiatCurrency.All.Any(f => f == symbol))
                {
                    var handleMissingFiatResult = HandleMissingFiatData(symbol, dateOnly, history);
                    if (handleMissingFiatResult.IsSuccess)
                    {
                        var fiatPrice = handleMissingFiatResult.Value;

                        if (dateOnly == DateTime.Today)
                        {
                            CacheTodayPrice(symbol, dateOnly, fiatPrice);
                        }

                        return fiatPrice;
                    }
                }
            }

            return await FetchAndSavePriceData(symbol, dateOnly, history).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the case where the requested symbol matches the default currency.
        /// </summary>
        /// <param name="symbol">The currency symbol.</param>
        /// <returns>A <see cref="Result{T}"/> indicating a failure due to the same symbol as the default currency.</returns>
        private Result<decimal> HandleDefaultCurrencyError(string symbol)
        {
            Log.ForContext<PriceHistoryService>().Warning("Attempted to fetch price for the same symbol as the default currency ({Symbol}).", symbol);
            return Result.Failure<decimal>(string.Format(Errors.ERR_SAME_SYMBOLS, symbol, DefaultCurrency));
        }

        /// <summary>
        /// Loads the price history from storage for a specific cryptocurrency symbol.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency.</param>
        /// <returns>A task representing the asynchronous operation, with a result of the price history dictionary.</returns>
        private async Task<ReadOnlyDictionary<DateTime, PriceRecord>> LoadPriceHistoryFromStorageAsync(string symbol)
        {
            return await _dataStores.GetOrAddAsync(symbol, async s =>
            {
                var symbolTradingPair = _priceHistoryApi.DetermineTradingPair(symbol, DefaultCurrency);
                var result = await _priceHistoryStorage.LoadHistoryAsync(symbolTradingPair).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    return result.Value.Where(r => r.ClosePrice > 0).ToDictionary(
                        record => record.CloseDate.Date,
                        record => record
                    ).AsReadOnly();
                }
                else
                {
                    // Save an empty file to avoid trying to fetch from the API repeatedly.
                    var emptyDictionary = new Dictionary<DateTime, PriceRecord>();
                    var saveResult = await _priceHistoryStorage.SaveHistoryAsync(symbolTradingPair, emptyDictionary.Values)
                        .TapError(Log.ForContext<PriceHistoryService>().Error).ConfigureAwait(false);
                    return emptyDictionary.AsReadOnly();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the case where fiat currency data is missing by attempting to retrieve the price from previous working days.
        /// </summary>
        /// <param name="symbol">The currency symbol.</param>
        /// <param name="date">The date for which data is missing.</param>
        /// <param name="history">The existing price history.</param>
        /// <returns>A <see cref="Result{T}"/> containing the price or an error message.</returns>
        private Result<decimal> HandleMissingFiatData(string symbol, DateTime date, ReadOnlyDictionary<DateTime, PriceRecord> history)
        {
            Log.ForContext<PriceHistoryService>().Debug("Missing fiat data for {Symbol} on {Date:yyyy-MM-dd}. Trying previous working days...", symbol, date);

            var previousPriceData = GetPreviousWorkingDayPriceData(date, history);
            if (previousPriceData > -1)
            {
                Log.ForContext<PriceHistoryService>().Debug("Fiat data found for {Symbol} on {Date:yyyy-MM-dd}.", symbol, date);
                return previousPriceData;
            }

            Log.ForContext<PriceHistoryService>().Debug("No available fiat data for {Symbol} on previous working days.", symbol);
            return Result.Failure<decimal>($"No price data available for {symbol} on {date:yyyy-MM-dd}");
        }

        /// <summary>
        /// Retrieves the price data from previous working days in the event of missing fiat currency data.
        /// </summary>
        /// <param name="date">The date for which data is missing.</param>
        /// <param name="history">The existing price history.</param>
        /// <returns>The closing price from a previous working day, or -1 if not found.</returns>
        private decimal GetPreviousWorkingDayPriceData(DateTime date, ReadOnlyDictionary<DateTime, PriceRecord> history)
        {
            for (int i = 1; i <= 4; i++)
            {
                var previousDate = date.AddDays(-i);
                if (history.TryGetValue(previousDate, out var priceData))
                    return priceData.ClosePrice;
            }
            return -1;
        }

        /// <summary>
        /// Fetches new price data from the API and saves it to storage.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency.</param>
        /// <param name="date">The date for which data is being fetched.</param>
        /// <param name="history">The existing price history.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        private async Task<Result<decimal>> FetchAndSavePriceData(string symbol, DateTime date, ReadOnlyDictionary<DateTime, PriceRecord> history)
        {
            var symbolTradingPair = _priceHistoryApi.DetermineTradingPair(symbol, DefaultCurrency);
            var endDate = AdjustEndDate(date);
            var result = await _priceHistoryApi.FetchPriceHistoryAsync(symbolTradingPair, date.Date, endDate).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                // Save the newly fetched data.
                var saveResult = await SaveNewPriceHistoryAsync(symbolTradingPair, history, result.Value)
                    .TapError(Log.ForContext<PriceHistoryService>().Error).ConfigureAwait(false);

                history = await UpdateHistoryWithFetchedDataAsync(symbol, history, result.Value).ConfigureAwait(false);

                if (history.ContainsKey(date.Date))
                    return history[date.Date].ClosePrice;
                else if (FiatCurrency.All.Any(f => f == symbol))
                {
                    var handleMissingFiatResult = HandleMissingFiatData(symbol, date, history);
                    if (handleMissingFiatResult.IsSuccess)
                        return handleMissingFiatResult.Value;
                }

                return Result.Failure<decimal>("Unexpected error has occurred.");
            }

            return Result.Failure<decimal>(result.Error);
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
        /// Saves new price records to storage, avoiding duplicates with existing records.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency.</param>
        /// <param name="currentHistory">The current price history.</param>
        /// <param name="newRecords">The new records to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        private async Task<Result> SaveNewPriceHistoryAsync(string symbol, ReadOnlyDictionary<DateTime, PriceRecord> currentHistory, IEnumerable<PriceRecord> newRecords)
        {
            var existingRecords = currentHistory.Values;

            // Filter out records that already exist.
            var recordsToSave = newRecords
                .Where(newRecord => !existingRecords.Any(existingRecord =>
                    existingRecord.CurrencyPair == newRecord.CurrencyPair &&
                    existingRecord.CloseDate == newRecord.CloseDate))
                .ToList();

            if (recordsToSave.Any())
            {
                // Save only the new records.
                return await _priceHistoryStorage.SaveHistoryAsync(symbol, recordsToSave).ConfigureAwait(false);
            }
            else
            {
                Log.Information("No new records to save for {Symbol}.", symbol);
                return Result.Success();
            }
        }

        /// <summary>
        /// Updates the in-memory price history with the newly fetched data.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency.</param>
        /// <param name="history">The current price history.</param>
        /// <param name="records">The new records to be added.</param>
        /// <returns>A task representing the asynchronous operation, with a result of the updated price history dictionary.</returns>
        private async Task<ReadOnlyDictionary<DateTime, PriceRecord>> UpdateHistoryWithFetchedDataAsync(string symbol, ReadOnlyDictionary<DateTime, PriceRecord> history, IEnumerable<PriceRecord> records)
        {
            return await _dataStores.AddOrUpdateAsync(symbol, async _ => await Task.FromResult(history), async (k, v) =>
            {
                var dict = v.ToDictionary();
                foreach (var record in records)
                {
                    var closeDate = record.CloseDate.Date;
                    if (!dict.ContainsKey(closeDate) && record.ClosePrice > 0)
                        dict[closeDate] = record;
                }
                return dict.AsReadOnly();
            }).ConfigureAwait(false);
        }

        private void CacheTodayPrice(string symbol, DateTime date, decimal price)
        {
            var cacheKey = GetCacheKey(symbol, date);
            _cache.Set(cacheKey, price, DateTimeOffset.Now.AddMinutes(1));  // Use the Set method with absolute expiration
        }

        private string GetCacheKey(string symbol, DateTime date)
        {
            return $"{symbol}_{date:yyyyMMdd}";
        }

        public async Task<Result<Dictionary<string, decimal>>> GetCurrentPricesAsync(IEnumerable<string> symbols)
        {
            // Check if the current price is already cached
            Dictionary<string, decimal> found = new();
            List<string> missingSymbols = new();

            foreach (var s in symbols)
            {
                var cacheKey = GetCacheKey(s, DateTime.Today);
                if (_cache.Get(cacheKey) is decimal cachedPrice)
                {
                    found.Add(s, cachedPrice);
                }
                missingSymbols.Add(s);
            }            

            // Fetch the current price from the API            
            var result = await _priceHistoryApi.FetchCurrentPriceAsync(missingSymbols, DefaultCurrency).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                foreach(var s in missingSymbols)
                {
                    var priceRecord = result.Value.FirstOrDefault(p => p.CurrencyPair.Split("-")[0] == s);
                    if(priceRecord != null)
                        found.Add(s, priceRecord.ClosePrice);
                }

                return Result.Success(found);
            }

            return Result.Failure<Dictionary<string, decimal>>(result.Error);
        }
    }
}
