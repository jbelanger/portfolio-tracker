using System.Globalization;
using CSharpFunctionalExtensions;
using CsvHelper;
using Serilog;
using YahooFinanceApi;

namespace Portfolio.App;

public class YahooFinancePriceHistoryStore : IPriceHistoryStore
{
    private readonly Dictionary<DateTime, CryptoPriceData> _dataStore;
    private readonly string _csvFileName;
    private readonly string _symbol;
    private readonly bool _isCryptoSymbol;

    private YahooFinancePriceHistoryStore(string symbol, string csvFileName, Dictionary<DateTime, CryptoPriceData> dataStore)
    {
        _csvFileName = csvFileName;
        _symbol = symbol;
        _dataStore = dataStore;

        if (!_symbol.EndsWith("=X"))
            _isCryptoSymbol = true;
    }

    public static async Task<Result<YahooFinancePriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {
        if (symbolFrom == symbolTo)
            return Result.Failure<YahooFinancePriceHistoryStore>($"Symbols must be of different currency/coin ({symbolFrom}-{symbolTo}).");

        var symbol = $"{symbolFrom}-{symbolTo}";
        if (FiatCurrencies.Codes.Contains(symbolFrom) && FiatCurrencies.Codes.Contains(symbolTo))
            symbol = $"{symbolFrom}{symbolTo}=X";
        else if (symbolFrom == "IMX")
            symbol = $"IMX10603-{symbolTo}";
        else if (symbolFrom == "GRT")
            symbol = $"GRT6719-{symbolTo}";
        else if (symbolFrom == "RNDR")
            symbol = $"RENDER-{symbolTo}";
        else if (symbolFrom == "UNI")
            symbol = $"UNI7083-{symbolTo}";
        else if (symbolFrom == "BEAM")
            symbol = $"BEAM28298-{symbolTo}";

        Dictionary<DateTime, CryptoPriceData> dataStore;
        var csvFileName = $"pricedata/{symbol}_history.csv";

        if (!File.Exists(csvFileName))
        {
            if (!Directory.Exists("pricedata"))
                Directory.CreateDirectory("pricedata");
            Console.WriteLine($"CSV file not found. Fetching data from Yahoo Finance for {symbol}...");
            var candles = await FetchAndSaveDataAsync(csvFileName, symbol, startDate.Date, endDate.Date);
            dataStore = LoadDataFromCandles(candles);
        }
        else
            dataStore = LoadDataFromCsv(csvFileName);

        if (!dataStore.Any())
            return Result.Failure<YahooFinancePriceHistoryStore>($"Could not get historical prices for symbol {symbol}.");

        return new YahooFinancePriceHistoryStore(symbol, csvFileName, dataStore);
    }

    private static async Task<IEnumerable<Candle>> FetchAndSaveDataAsync(string csvFileName, string symbol, DateTime startDate, DateTime endDate)
    {
        using (var writer = new StreamWriter(csvFileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            IReadOnlyList<Candle> candles;

            try
            {
                candles = await Yahoo.GetHistoricalAsync(symbol, startDate, endDate, Period.Daily);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not fetch historical prices for symbol {symbol}.");

                // This will write an empty file for this symbol. 
                // Do this to avoid fetching over and over again invalid quotes.
                candles = new List<Candle>();
            }

            csv.WriteRecords(candles.Select(candle => new
            {
                Date = candle.DateTime.ToString("yyyy-MM-dd"),
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            }));

            return candles;
        }
    }

    private static Dictionary<DateTime, CryptoPriceData> LoadDataFromCandles(IEnumerable<Candle> candles)
    {
        var dataStore = new Dictionary<DateTime, CryptoPriceData>();

        foreach (var candle in candles)
        {
            dataStore[candle.DateTime] = new CryptoPriceData
            {
                Date = candle.DateTime,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            };
        }

        return dataStore;
    }

    private static Dictionary<DateTime, CryptoPriceData> LoadDataFromCsv(string csvFileName)
    {
        var dataStore = new Dictionary<DateTime, CryptoPriceData>();

        using (var reader = new StreamReader(csvFileName))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<CryptoPriceData>();
            foreach (var record in records)
            {
                dataStore[record.Date] = record;
            }
        }

        return dataStore;
    }

    public async Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date)
    {                
        if (_dataStore.TryGetValue(date.Date, out var priceData))
        {
            return priceData;
        }
        else
        {
            Console.WriteLine($"No data found for {date:yyyy-MM-dd}. Fetching missing data...");


            if (!_isCryptoSymbol && (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday))
            {
                Console.WriteLine($"Missing data is for a fiat currency and on a weekend. Try to get data for Friday before...");

                DateTime lastWeekDay;
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    lastWeekDay = date.AddDays(-1);
                else
                    lastWeekDay = date.AddDays(-2);

                if (_dataStore.TryGetValue(lastWeekDay.Date, out var priceData2))
                {
                    return priceData2;
                }
            }

            IReadOnlyList<Candle> candles;
            try
            {
                DateTime dt = date; // Since the Yahoo lib manipulates the date object passed to GetHistoricalAsync
                candles = await Yahoo.GetHistoricalAsync(_symbol, dt, dt, Period.Daily);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not fetch historical prices for symbol {_symbol}.");
                candles = new List<Candle>();
            }

            var fetchedData = candles.FirstOrDefault();
            if (fetchedData != null)
            {
                priceData = new CryptoPriceData
                {
                    Date = fetchedData.DateTime,
                    Open = fetchedData.Open,
                    High = fetchedData.High,
                    Low = fetchedData.Low,
                    Close = fetchedData.Close,
                    Volume = fetchedData.Volume
                };

                // Update the in-memory store and save to CSV
                _dataStore[date] = priceData;
                SaveDataToCsv();

                return priceData;
            }

            return Result.Failure<CryptoPriceData>($"No data available for {_symbol} on {date:yyyy-MM-dd} from Yahoo Finance.");
        }
    }

    private void SaveDataToCsv()
    {
        using (var writer = new StreamWriter(_csvFileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(_dataStore.Values.Select(data => new
            {
                Date = data.Date.ToString("yyyy-MM-dd"),
                Open = data.Open,
                High = data.High,
                Low = data.Low,
                Close = data.Close,
                Volume = data.Volume
            }));
        }
    }
}