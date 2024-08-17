using System.Globalization;
using CSharpFunctionalExtensions;
using CsvHelper;
using Serilog;
using YahooFinanceApi;

namespace Portfolio.App
{
    /// <summary>
    /// Represents a store for historical price data retrieved from Yahoo Finance.
    /// </summary>
    public class YahooFinancePriceHistoryStore : IPriceHistoryStore
    {
        private const string ERR_GET_PRICE_FAILURE = "An error occurred. Price data for symbol {0} will not be available.";
        private const string ERR_SAME_SYMBOLS = "Symbols must be of different currency/coin ({0}-{1}).";
        private const string ERR_SAVE_LOCATION_REQUIRED = "Save location must be provided.";
        private const string ERR_FETCH_FAILURE = "An error occurred while fetching from Yahoo Finance API.";
        private const string ERR_NO_DATA_AVAILABLE = "No data available for {0} on {1} from Yahoo Finance.";
        private const string ERR_NO_PREVIOUS_DATA = "No data available for previous working days for {0}.";
        private const string DATE_FORMAT = "yyyy-MM-dd";

        private readonly Dictionary<string, CryptoPriceData> _dataStore;
        private readonly string _csvFileName;
        private readonly string _symbol;
        private readonly bool _isCryptoSymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="YahooFinancePriceHistoryStore"/> class.
        /// </summary>
        /// <param name="symbol">The financial symbol used for querying Yahoo Finance.</param>
        /// <param name="csvFileName">The file name where price data will be stored.</param>
        /// <param name="dataStore">A dictionary containing historical price data.</param>
        private YahooFinancePriceHistoryStore(string symbol, string csvFileName, Dictionary<string, CryptoPriceData> dataStore)
        {
            _csvFileName = csvFileName;
            _symbol = symbol;
            _dataStore = dataStore;
            _isCryptoSymbol = !_symbol.EndsWith("=X");
        }

        /// <summary>
        /// Creates an instance of <see cref="YahooFinancePriceHistoryStore"/> by fetching or loading historical price data.
        /// </summary>
        /// <param name="symbolFrom">The base currency or coin symbol.</param>
        /// <param name="symbolTo">The quote currency or coin symbol.</param>
        /// <param name="startDate">The start date for fetching historical data.</param>
        /// <param name="endDate">The end date for fetching historical data.</param>
        /// <param name="csvFileSaveLocation">The directory where the CSV file will be saved.</param>
        /// <returns>A <see cref="Result{T}"/> containing the created store or an error message.</returns>
        public static async Task<Result<YahooFinancePriceHistoryStore>> Create(
            string symbolFrom, 
            string symbolTo, 
            DateTime startDate, 
            DateTime endDate, 
            string csvFileSaveLocation = "historical_price_data")
        {
            if (symbolFrom == symbolTo)
                return Result.Failure<YahooFinancePriceHistoryStore>(string.Format(ERR_SAME_SYMBOLS, symbolFrom, symbolTo));

            if (string.IsNullOrEmpty(csvFileSaveLocation))
                return Result.Failure<YahooFinancePriceHistoryStore>(ERR_SAVE_LOCATION_REQUIRED);

            var symbol = DetermineYahooFinanceTradingPair(symbolFrom, symbolTo);
            var csvFileName = $"{csvFileSaveLocation}/{symbol}_history.csv";

            if (!File.Exists(csvFileName))
            {
                Directory.CreateDirectory(csvFileSaveLocation);

                return await FetchDataAsync(symbol, startDate, endDate)
                    .Tap(ds => SaveDataToCsv(csvFileName, ds))
                    .TapError(_ => SaveDataToCsv(csvFileName, new Dictionary<string, CryptoPriceData>()))
                    .Map(ds => new YahooFinancePriceHistoryStore(symbol, csvFileName, ds))
                    .MapError(e => string.Format(ERR_GET_PRICE_FAILURE, symbol));
            }
            else
            {
                var dataStore = LoadDataFromCsv(csvFileName);
                if (!dataStore.Any())
                    return Result.Failure<YahooFinancePriceHistoryStore>(string.Format(ERR_GET_PRICE_FAILURE, symbol));

                return new YahooFinancePriceHistoryStore(symbol, csvFileName, dataStore);
            }
        }

        /// <summary>
        /// Retrieves the price data for a specific date, fetching from Yahoo Finance if necessary.
        /// </summary>
        /// <param name="date">The date for which to retrieve price data.</param>
        /// <returns>A <see cref="Result{T}"/> containing the price data or an error message.</returns>
        public async Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date)
        {
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

            return await FetchDataAsync(_symbol, date, date)
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
        /// Determines the appropriate Yahoo Finance trading pair symbol based on input symbols.
        /// </summary>
        /// <param name="symbolFrom">The base currency or coin symbol.</param>
        /// <param name="symbolTo">The quote currency or coin symbol.</param>
        /// <returns>The Yahoo Finance trading pair symbol.</returns>
        private static string DetermineYahooFinanceTradingPair(string symbolFrom, string symbolTo)
        {
            if (FiatCurrencies.Codes.Contains(symbolFrom) && FiatCurrencies.Codes.Contains(symbolTo))
                return $"{symbolFrom}{symbolTo}=X";

            return symbolFrom switch
            {
                "IMX" => $"IMX10603-{symbolTo}",
                "GRT" => $"GRT6719-{symbolTo}",
                "RNDR" => $"RENDER-{symbolTo}",
                "UNI" => $"UNI7083-{symbolTo}",
                "BEAM" => $"BEAM28298-{symbolTo}",
                _ => $"{symbolFrom}-{symbolTo}"
            };
        }

        /// <summary>
        /// Fetches historical price data from Yahoo Finance for a given symbol and date range.
        /// </summary>
        /// <param name="symbol">The symbol to fetch data for.</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a dictionary of price data or an error message.</returns>
        private static async Task<Result<Dictionary<string, CryptoPriceData>>> FetchDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            try
            {
                Log.Debug($"[YahooFinance] [{symbol}] Fetching data, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...");

                var candles = await Yahoo.GetHistoricalAsync(symbol, startDate.Date, endDate.Date, Period.Daily);
                var result = MapCandlesToCryptoPriceData(candles);

                Log.Debug($"[YahooFinance] [{symbol}] {candles.Count()} day(s) of price data has been retrieved.");

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[YahooFinance] [{symbol}] Exception in {nameof(FetchDataAsync)}: {ex.GetBaseException().Message}");
                return Result.Failure<Dictionary<string, CryptoPriceData>>(ERR_FETCH_FAILURE);
            }
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

        /// <summary>
        /// Retrieves the price data from the most recent working day before the given date.
        /// </summary>
        /// <param name="date">The date to look back from.</param>
        /// <returns>A <see cref="Result{T}"/> containing the price data or an error message.</returns>
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
