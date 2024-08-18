using System.Collections.Concurrent;
using CSharpFunctionalExtensions;
using Serilog;

namespace Portfolio.App.HistoricalPrice;

public class YahooFinancePriceHistoryService : IPriceHistoryService
{
    private readonly IPriceHistoryApi _priceHistoryApi;
    private readonly IPriceHistoryStorageService _priceHistoryStorage;
    private ConcurrentDictionary<string, Dictionary<DateTime, CryptoPriceRecord>> _dataStores = new();

    public string DefaultCurrency { get; set; }

    public YahooFinancePriceHistoryService(
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
        Log.ForContext<YahooFinancePriceHistoryService>().Information("Retrieving closing price for {Symbol} on {Date:yyyy-MM-dd}.", symbol, date);

        if (symbol == DefaultCurrency)
        {
            Log.Warning("Attempted to fetch price for the same symbol as the default currency ({Symbol}).", symbol);
            return Result.Failure<decimal>(string.Format(Errors.ERR_SAME_SYMBOLS, symbol, DefaultCurrency));
        }

        var dateOnly = date.Date;
        var history = await LoadPriceHistoryFromStorageAsync(symbol);

        if (history.ContainsKey(dateOnly))
            return history[dateOnly].ClosePrice;
        else
        {
            var isFiatSymbol = FiatCurrencies.Codes.Contains(symbol);
            if (isFiatSymbol)
            {
                Log.Debug($"[YahooFinance] [{symbol}] Missing fiat data for {dateOnly:yyyy-MM-dd}. Trying previous working days...");

                var previousPriceData = GetPreviousWorkingDayPriceData(dateOnly, history);
                if (previousPriceData > -1)
                    return previousPriceData;

                Log.Debug($"[YahooFinance] [{symbol}] No available data on previous working days.");
            }

            // If storage does not have any data yet, fetch 
            var symbolTradingPair = _priceHistoryApi.DetermineTradingPair(symbol, DefaultCurrency);

            // Fetch data along with the next 365 days or until today...
            // TODO: This should be controllable from the API service instead.
            var endDate = dateOnly.AddDays(365);
            if (endDate > DateTime.Now)
                endDate = DateTime.Now;

            var result = await _priceHistoryApi.FetchDataAsync(symbolTradingPair, dateOnly, endDate);
            if (result.IsSuccess)
            {
                foreach (var record in result.Value)
                {
                    var closeDate = record.CloseDate.Date;
                    if (!history.ContainsKey(closeDate))
                        history[closeDate] = record;
                }

                await _priceHistoryStorage.SaveHistoryAsync(symbolTradingPair, history.Values);

                return history[dateOnly].ClosePrice;
            }
            else
                return Result.Failure<decimal>(result.Error);
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
                    return result.Value.ToDictionary(
                        record => record.CloseDate.Date,
                        record => record
                    );
                }
                else
                {
                    // Save an empty file to avoid trying to fetch from Yahoo Finance
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
