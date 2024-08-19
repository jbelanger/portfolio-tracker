using System.Collections.Concurrent;
using CSharpFunctionalExtensions;
using Serilog;

namespace Portfolio.App.HistoricalPrice;

public class PriceHistoryService : IPriceHistoryService
{
    private readonly IPriceHistoryApi _priceHistoryApi;
    private readonly IPriceHistoryStorageService _priceHistoryStorage;
    private ConcurrentDictionary<string, Dictionary<DateTime, CryptoPriceRecord>> _dataStores = new();

    public string DefaultCurrency { get; set; }

    public PriceHistoryService(
        IPriceHistoryApi priceHistoryApi,
        IPriceHistoryStorageService priceHistoryStorage,
        string defaultCurrencySymbol = Strings.CURRENCY_USD)
    {
        _priceHistoryApi = priceHistoryApi ?? throw new ArgumentNullException(nameof(priceHistoryApi));
        _priceHistoryStorage = priceHistoryStorage ?? throw new ArgumentNullException(nameof(priceHistoryStorage));

        DefaultCurrency = defaultCurrencySymbol ?? throw new ArgumentNullException(nameof(defaultCurrencySymbol));
    }

    public async Task<Result<decimal>> GetPriceAtCloseTimeAsync(string symbol, DateTime date)
    {
        var dateOnly = date.Date;

        if (symbol == DefaultCurrency)
            return HandleDefaultCurrencyError(symbol);

        var history = await LoadPriceHistory(symbol);
        if (history.Any())
        {

            if (history.ContainsKey(dateOnly))
                return history[dateOnly].ClosePrice;

            if (FiatCurrencies.Codes.Contains(symbol))
            {
                var handleMissingFiatResult = HandleMissingFiatData(symbol, dateOnly, history);
                if (handleMissingFiatResult.IsSuccess)
                    return handleMissingFiatResult.Value;
            }
        }

        return await FetchAndSavePriceData(symbol, dateOnly, history);
    }

    private Result<decimal> HandleDefaultCurrencyError(string symbol)
    {
        Log.Warning("Attempted to fetch price for the same symbol as the default currency ({Symbol}).", symbol);
        return Result.Failure<decimal>(string.Format(Errors.ERR_SAME_SYMBOLS, symbol, DefaultCurrency));
    }

    private async Task<Dictionary<DateTime, CryptoPriceRecord>> LoadPriceHistory(string symbol)
    {
        return await LoadPriceHistoryFromStorageAsync(symbol);
    }

    private Result<decimal> HandleMissingFiatData(string symbol, DateTime date, Dictionary<DateTime, CryptoPriceRecord> history)
    {
        Log.Debug("Missing fiat data for {Symbol} on {Date:yyyy-MM-dd}. Trying previous working days...", symbol, date);

        var previousPriceData = GetPreviousWorkingDayPriceData(date, history);
        if (previousPriceData > -1)
        {
            Log.Debug("Fiat data found for {Symbol} on {Date:yyyy-MM-dd}.", symbol, date);
            return previousPriceData;
        }

        Log.Debug("No available fiat data for {Symbol} on previous working days..", symbol);
        return Result.Failure<decimal>($"No price data available for {symbol} on {date:yyyy-MM-dd}");
    }

    private async Task<Result<decimal>> FetchAndSavePriceData(string symbol, DateTime date, Dictionary<DateTime, CryptoPriceRecord> history)
    {
        var symbolTradingPair = _priceHistoryApi.DetermineTradingPair(symbol, DefaultCurrency);
        var endDate = AdjustEndDate(date);
        var result = await _priceHistoryApi.FetchPriceHistoryAsync(symbolTradingPair, date.Date, endDate);

        if (result.IsSuccess)
        {            
            // If save fails, we can still continue however date will be fetched all the time. 
            // Ensure error is logged and proper action is taken.
            var saveResult = await SaveNewPriceHistoryAsync(symbolTradingPair, history, result.Value)
                .TapError(Log.ForContext<PriceHistoryService>().Error); 

            UpdateHistoryWithFetchedData(history, result.Value);

            if (history.ContainsKey(date.Date))
                return history[date.Date].ClosePrice;
            else if (FiatCurrencies.Codes.Contains(symbol))
            {
                var handleMissingFiatResult = HandleMissingFiatData(symbol, date, history);
                if (handleMissingFiatResult.IsSuccess)
                    return handleMissingFiatResult.Value;
            }

            return Result.Failure<decimal>("Unexpected error has occurred. ");
        }

        return Result.Failure<decimal>(result.Error);
    }

    public async Task<Result> SaveNewPriceHistoryAsync(string symbol, Dictionary<DateTime, CryptoPriceRecord> currentHistory, IEnumerable<CryptoPriceRecord> newRecords)
    {
        var existingRecords = currentHistory.Values;

        // Filter out records that already exist
        var recordsToSave = newRecords
            .Where(newRecord => !existingRecords.Any(existingRecord =>
                existingRecord.CurrencyPair == newRecord.CurrencyPair &&
                existingRecord.CloseDate == newRecord.CloseDate))
            .ToList();

        if (recordsToSave.Any())
        {
            // Save only the new records
            return await _priceHistoryStorage.SaveHistoryAsync(symbol, recordsToSave);
        }
        else
        {
            Log.Information("No new records to save for {Symbol}.", symbol);
            return Result.Success();
        }
    }

    private DateTime AdjustEndDate(DateTime date)
    {
        var endDate = date.AddDays(365);
        return endDate > DateTime.Now ? DateTime.Now.AddDays(1) : endDate;
    }

    private void UpdateHistoryWithFetchedData(Dictionary<DateTime, CryptoPriceRecord> history, IEnumerable<CryptoPriceRecord> records)
    {
        foreach (var record in records)
        {
            var closeDate = record.CloseDate.Date;
            if (!history.ContainsKey(closeDate) && record.ClosePrice > 0)
                history[closeDate] = record;
        }
    }

    private async Task<Dictionary<DateTime, CryptoPriceRecord>> LoadPriceHistoryFromStorageAsync(string symbol)
    {
        return await _dataStores.GetOrAddAsync(symbol, async s =>
            {
                var symbolTradingPair = _priceHistoryApi.DetermineTradingPair(symbol, DefaultCurrency);
                var result = await _priceHistoryStorage.LoadHistoryAsync(symbolTradingPair);
                if (result.IsSuccess)
                {
                    return result.Value.Where(r => r.ClosePrice > 0).ToDictionary(
                        record => record.CloseDate.Date,
                        record => record
                    );
                }
                else
                {
                    // Save an empty file to avoid trying to fetch from API
                    // over and over again.      
                    var emptyDictionary = new Dictionary<DateTime, CryptoPriceRecord>();
                    var saveResult = await _priceHistoryStorage.SaveHistoryAsync(symbolTradingPair, emptyDictionary.Values)
                        .TapError(Log.Error);
                    return emptyDictionary;
                }
            });
    }

    private decimal GetPreviousWorkingDayPriceData(DateTime date, Dictionary<DateTime, CryptoPriceRecord> history)
    {
        for (int i = 1; i <= 4; i++)
        {
            var previousDate = date.AddDays(-i);
            if (history.TryGetValue(previousDate, out var priceData))
                return priceData.ClosePrice;
        }
        return -1;
    }
}
