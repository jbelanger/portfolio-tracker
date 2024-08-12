using System.Globalization;
using System.Net.Http;
using CSharpFunctionalExtensions;
using CsvHelper;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Portfolio.App;

public class MessariPriceHistoryStore : IPriceHistoryStore
{
    private readonly Dictionary<DateTime, CryptoPriceData> _dataStore;
    private readonly string _csvFileName;
    private readonly string _symbol;

    private MessariPriceHistoryStore(string symbol, string csvFileName, Dictionary<DateTime, CryptoPriceData> dataStore)
    {
        _csvFileName = csvFileName;
        _symbol = symbol;
        _dataStore = dataStore;
    }

    public static async Task<Result<MessariPriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {
        if (symbolFrom == symbolTo)
            return Result.Failure<MessariPriceHistoryStore>($"Symbols must be of different currency/coin ({symbolFrom}-{symbolTo}).");

        // Messari requires the base symbol to be the asset ID, not a pair.
        var symbol = symbolFrom.ToLower(); // Use Messari asset IDs like "bitcoin", "ethereum", etc.

        Dictionary<DateTime, CryptoPriceData> dataStore;
        var csvFileName = $"pricedata/{symbol}-{symbolTo}_history.csv";

        if (!File.Exists(csvFileName))
        {
            if (!Directory.Exists("pricedata"))
                Directory.CreateDirectory("pricedata");
            Console.WriteLine($"CSV file not found. Fetching data from Messari API for {symbol}...");
            var candles = await FetchAndSaveDataAsync(csvFileName, symbol, startDate.Date, endDate.Date);
            dataStore = LoadDataFromCandles(candles);
        }
        else
            dataStore = LoadDataFromCsv(csvFileName);

        if (!dataStore.Any())
            return Result.Failure<MessariPriceHistoryStore>($"Could not get historical prices for symbol {symbol}.");

        return new MessariPriceHistoryStore(symbol, csvFileName, dataStore);
    }

    private static async Task<IEnumerable<CryptoPriceData>> FetchAndSaveDataAsync(string csvFileName, string symbol, DateTime startDate, DateTime endDate)
    {
        using (var writer = new StreamWriter(csvFileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            IEnumerable<CryptoPriceData> candles;

            try
            {
                candles = await FetchHistoricalDataAsync(symbol, startDate, endDate);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not fetch historical prices for symbol {symbol} from Messari API.");

                // This will write an empty file for this symbol.
                // Do this to avoid fetching over and over again invalid quotes.
                candles = new List<CryptoPriceData>();
            }

            csv.WriteRecords(candles.Select(candle => new
            {
                Date = candle.Date.ToString("yyyy-MM-dd"),
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            }));

            return candles;
        }
    }

    private static async Task<IEnumerable<CryptoPriceData>> FetchHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        using (var httpClient = new HttpClient())
        {
            string startDateString = startDate.ToString("yyyy-MM-dd");
            string endDateString = endDate.ToString("yyyy-MM-dd");
            string url = $"https://data.messari.io/api/v1/assets/{symbol}/metrics/price/time-series?start={startDateString}&end={endDateString}&interval=1d";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            var jsonData = JObject.Parse(content);
            var priceData = jsonData["data"]["values"];

            return priceData.Select(item => new CryptoPriceData
            {
                Date = DateTime.ParseExact(item[0].ToString(), "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Open = item[1].Value<decimal>(),
                High = item[2].Value<decimal>(),
                Low = item[3].Value<decimal>(),
                Close = item[4].Value<decimal>(),
                Volume = item[5].Value<long>()
            }).ToList();
        }
    }

    private static Dictionary<DateTime, CryptoPriceData> LoadDataFromCandles(IEnumerable<CryptoPriceData> candles)
    {
        var dataStore = new Dictionary<DateTime, CryptoPriceData>();

        foreach (var candle in candles)
        {
            dataStore[candle.Date] = candle;
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

            IEnumerable<CryptoPriceData> candles;
            try
            {
                candles = await FetchHistoricalDataAsync(_symbol, date, date);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not fetch historical prices for symbol {_symbol} from Messari API.");
                candles = new List<CryptoPriceData>();
            }

            var fetchedData = candles.FirstOrDefault();
            if (fetchedData != null)
            {
                priceData = fetchedData;

                // Update the in-memory store and save to CSV
                _dataStore[date] = priceData;
                SaveDataToCsv();

                return priceData;
            }

            return Result.Failure<CryptoPriceData>($"No data available for {_symbol} on {date:yyyy-MM-dd} from Messari API.");
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
