using System.Globalization;
using CSharpFunctionalExtensions;
using CsvHelper;
using Serilog;
using YahooFinanceApi;

namespace Portfolio.App
{
    public interface IPriceHistoryApi
    {
        public string DetermineTradingPair(string symbol);
        public Task<Result<Dictionary<string, CryptoPriceData>>> FetchDataAsync(string symbol, DateTime startDate, DateTime endDate);
    }

    public class YahooFinancePriceHistoryApi : IPriceHistoryApi
    {
        private const string ERR_FETCH_FAILURE = "An error occurred while fetching from Yahoo Finance API.";
        private const string DATE_FORMAT = "yyyy-MM-dd";

        public string DefaultCurrencySymbol { get; }

        public YahooFinancePriceHistoryApi(string defaultCurrencySymbol = "USD")
        {
            DefaultCurrencySymbol = defaultCurrencySymbol ?? throw new ArgumentNullException(nameof(defaultCurrencySymbol));
        }

        /// <summary>
        /// Fetches historical price data from Yahoo Finance for a given symbol and date range.
        /// </summary>
        /// <param name="symbol">The symbol to fetch data for.</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a dictionary of price data or an error message.</returns>
        public async Task<Result<Dictionary<string, CryptoPriceData>>> FetchDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var symbolPair = DetermineTradingPair(symbol);

            try
            {
                Log.Debug($"[YahooFinanceAPI] [{symbolPair}] Fetching data, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...");

                var candles = await Yahoo.GetHistoricalAsync(symbolPair, startDate.Date, endDate.Date, Period.Daily);
                var result = MapCandlesToCryptoPriceData(candles);

                Log.Debug($"[YahooFinanceAPI] [{symbolPair}] {candles.Count()} day(s) of price data has been retrieved.");

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[YahooFinanceAPI] [{symbolPair}] Exception in {nameof(FetchDataAsync)}: {ex.GetBaseException().Message}");
                return Result.Failure<Dictionary<string, CryptoPriceData>>(ERR_FETCH_FAILURE);
            }
        }

        /// <summary>
        /// Determines the appropriate Yahoo Finance trading pair symbol based on input symbols.
        /// </summary>
        /// <param name="symbolFrom">The base currency or coin symbol.</param>
        /// <param name="symbolTo">The quote currency or coin symbol.</param>
        /// <returns>The Yahoo Finance trading pair symbol.</returns>
        public string DetermineTradingPair(string symbol)
        {
            if (FiatCurrencies.Codes.Contains(symbol) && FiatCurrencies.Codes.Contains(DefaultCurrencySymbol))
                return $"{symbol}{DefaultCurrencySymbol}=X";

            return symbol switch
            {
                "IMX" => $"IMX10603-{DefaultCurrencySymbol}",
                "GRT" => $"GRT6719-{DefaultCurrencySymbol}",
                "RNDR" => $"RENDER-{DefaultCurrencySymbol}",
                "UNI" => $"UNI7083-{DefaultCurrencySymbol}",
                "BEAM" => $"BEAM28298-{DefaultCurrencySymbol}",
                _ => $"{symbol}-{DefaultCurrencySymbol}"
            };
        }

        /// <summary>
        /// Maps a collection of <see cref="Candle"/> objects to a dictionary of <see cref="CryptoPriceData"/>.
        /// </summary>
        /// <param name="candles">The collection of candles to map.</param>
        /// <returns>A dictionary containing price data mapped from candles.</returns>
        private static Dictionary<string, CryptoPriceData> MapCandlesToCryptoPriceData(IEnumerable<Candle> candles)
        {
            return candles.ToDictionary(c => c.DateTime.ToString(DATE_FORMAT), ToCryptoPriceData);
        }

        /// <summary>
        /// Converts a <see cref="Candle"/> object to a <see cref="CryptoPriceData"/> object.
        /// </summary>
        /// <param name="candle">The candle to convert.</param>
        /// <returns>The corresponding <see cref="CryptoPriceData"/> object.</returns>
        private static CryptoPriceData ToCryptoPriceData(Candle candle)
        {
            return new CryptoPriceData
            {
                Date = candle.DateTime,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            };
        }
    }


    /// <summary>
    /// Represents a store for historical price data retrieved from Yahoo Finance.
    /// </summary>
    public class YahooFinancePriceHistoryService : IPriceHistoryService
    {
        private const string ERR_GET_PRICE_FAILURE = "An error occurred. Price data for symbol {0} will not be available.";
        private const string ERR_SAVE_LOCATION_REQUIRED = "Save location must be provided.";
        private const string ERR_FETCH_FAILURE = "An error occurred while fetching from Yahoo Finance API.";

        private const string ERR_NO_DATA_AVAILABLE = "No data available for {0} on {1} from Yahoo Finance.";
        private const string ERR_NO_PREVIOUS_DATA = "No data available for previous working days for {0}.";
        private const string DATE_FORMAT = "yyyy-MM-dd";

        private Dictionary<string, CryptoPriceData>? _dataStore;
        private readonly string _csvFileName;
        private DateTime _dateRangeStart;
        private readonly DateTime _dateRangeEnd;
        private readonly IPriceHistoryApi _priceHistoryApi;
        private readonly string _csvFileSaveLocation;
        private readonly string _symbol;
        private readonly bool _isCryptoSymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="YahooFinancePriceHistoryService"/> class.
        /// </summary>
        /// <param name="symbol">The financial symbol used for querying Yahoo Finance.</param>
        /// <param name="csvFileName">The file name where price data will be stored.</param>
        /// <param name="dataStore">A dictionary containing historical price data.</param>
        public YahooFinancePriceHistoryService(
            string symbolFrom,            
            DateTime dateRangeStart,
            DateTime dateRangeEnd,
            IPriceHistoryApi priceHistoryApi,
            string csvFileSaveLocation = "historical_price_data")
        {
            if (csvFileSaveLocation is null)
                throw new ArgumentNullException(nameof(csvFileSaveLocation));

            _symbol = priceHistoryApi.DetermineTradingPair(symbolFrom);
            _csvFileName = $"{csvFileSaveLocation}/{_symbol}_history.csv";
            _dateRangeStart = dateRangeStart;
            _dateRangeEnd = dateRangeEnd;
            _priceHistoryApi = priceHistoryApi;
            _csvFileSaveLocation = csvFileSaveLocation;
            _isCryptoSymbol = !_symbol.EndsWith("=X");
        }

        public async Task<Result<Dictionary<string, CryptoPriceData>>> FetchOrLoadCached()
        {
            if (!File.Exists(_csvFileName))
            {
                Directory.CreateDirectory(_csvFileSaveLocation);

                return await _priceHistoryApi.FetchDataAsync(_symbol, _dateRangeStart, _dateRangeEnd)
                    .Tap(ds => SaveDataToCsv(_csvFileName, ds))                    
                    .MapError(e =>
                    {
                        // Save an empty file to avoid trying to fetch from Yahoo Finance
                        // over and over again.      
                        SaveDataToCsv(_csvFileName, new Dictionary<string, CryptoPriceData>());
                        return string.Format(ERR_GET_PRICE_FAILURE, _symbol);
                    });
            }
            else
            {
                var dataStore = LoadDataFromCsv(_csvFileName);
                if (!dataStore.Any())
                    return Result.Failure<Dictionary<string, CryptoPriceData>>(string.Format(ERR_GET_PRICE_FAILURE, _symbol));

                return dataStore;
            }
        }

        /// <summary>
        /// Retrieves the price data for a specific date, fetching from Yahoo Finance if necessary.
        /// </summary>
        /// <param name="date">The date for which to retrieve price data.</param>
        /// <returns>A <see cref="Result{T}"/> containing the price data or an error message.</returns>
        public async Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date)
        {
            // Ensure datastore is loaded from api or csv
            if(_dataStore == null)
            {   
                if (date < _dateRangeStart)
                    _dateRangeStart = date;

                var result = await FetchOrLoadCached();
                if(result.IsFailure)
                    return Result.Failure<CryptoPriceData>(result.Error);

                _dataStore = result.Value;
            }

            var dateString = date.ToString(DATE_FORMAT);
            if (_dataStore.TryGetValue(dateString, out var priceData))
                return priceData;

            if (!_isCryptoSymbol)
            {
                Log.Debug($"[YahooFinance] [{_symbol}] Missing fiat data for {date:yyyy-MM-dd}. Trying previous working days...");

                var previousPriceData = GetPreviousWorkingDayPriceData(date);
                if (previousPriceData.IsSuccess)
                    return previousPriceData;
            }

            return await _priceHistoryApi.FetchDataAsync(_symbol, date, date)
                .Ensure(d => d.Any(), string.Format(ERR_NO_DATA_AVAILABLE, _symbol, date.ToString(DATE_FORMAT)))
                .Tap(ds =>
                {
                    var firstRecord = ds.First();
                    if (firstRecord.Value != null)
                    {
                        _dataStore[firstRecord.Value.Date.ToString(DATE_FORMAT)] = firstRecord.Value;
                        SaveDataToCsv(_csvFileName, _dataStore);
                    }
                })
                .Map(d => d.First().Value);
        }



        /// <summary>
        /// Fetches historical price data from Yahoo Finance for a given symbol and date range.
        /// </summary>
        /// <param name="symbol">The symbol to fetch data for.</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a dictionary of price data or an error message.</returns>
        // private async Task<Result<Dictionary<string, CryptoPriceData>>> FetchDataAsync(string symbol, DateTime startDate, DateTime endDate)
        // {
        //     try
        //     {
        //         Log.Debug($"[YahooFinance] [{symbol}] Fetching data, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...");

        //         var candles = await Yahoo.GetHistoricalAsync(symbol, startDate.Date, endDate.Date, Period.Daily);
        //         var result = MapCandlesToCryptoPriceData(candles);

        //         Log.Debug($"[YahooFinance] [{symbol}] {candles.Count()} day(s) of price data has been retrieved.");

        //         return result;
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Error($"[YahooFinance] [{symbol}] Exception in {nameof(FetchDataAsync)}: {ex.GetBaseException().Message}");
        //         return Result.Failure<Dictionary<string, CryptoPriceData>>(ERR_FETCH_FAILURE);
        //     }
        // }


        /// <summary>
        /// Loads historical price data from a CSV file into a dictionary.
        /// </summary>
        /// <param name="csvFileName">The path to the CSV file.</param>
        /// <returns>A dictionary containing the loaded price data.</returns>
        private static Dictionary<string, CryptoPriceData> LoadDataFromCsv(string csvFileName)
        {
            using var reader = new StreamReader(csvFileName);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var dataStore = csv.GetRecords<CryptoPriceData>().ToDictionary(
                record => record.Date.ToString(DATE_FORMAT),
                record => record
            );

            return dataStore;
        }

        /// <summary>
        /// Saves historical price data to a CSV file.
        /// </summary>
        /// <param name="csvFileName">The path to the CSV file.</param>
        /// <param name="priceDataDict">The dictionary of price data to save.</param>
        private static void SaveDataToCsv(string csvFileName, Dictionary<string, CryptoPriceData> priceDataDict)
        {
            using var writer = new StreamWriter(csvFileName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteRecords(priceDataDict.Values.Select(data => new
            {
                Date = data.Date.ToString(DATE_FORMAT),
                data.Open,
                data.High,
                data.Low,
                data.Close,
                data.Volume
            }));
        }

        private Result<CryptoPriceData> GetPreviousWorkingDayPriceData(DateTime date)
        {
            for (int i = 1; i <= 4; i++)
            {
                var previousDate = date.AddDays(-i).ToString(DATE_FORMAT);
                if (_dataStore.TryGetValue(previousDate, out var priceData))
                {
                    Log.Debug($"[YahooFinance] [{_symbol}] Fiat data found on {previousDate}.");
                    return priceData;
                }
            }

            return Result.Failure<CryptoPriceData>(string.Format(ERR_NO_PREVIOUS_DATA, _symbol));
        }
    }
}
