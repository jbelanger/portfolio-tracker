using System.Globalization;
using System.Net.Http;
using CSharpFunctionalExtensions;
using CsvHelper;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Portfolio.App;

public class CoinGeckoPriceHistoryStore : IPriceHistoryStore
{
    private readonly Dictionary<string, CryptoPriceData> _dataStore;
    private readonly string _csvFileName;
    private readonly string _symbol;

    private CoinGeckoPriceHistoryStore(string symbol, string csvFileName, Dictionary<string, CryptoPriceData> dataStore)
    {
        _csvFileName = csvFileName;
        _symbol = symbol;
        _dataStore = dataStore;
    }

    public static async Task<Result<CoinGeckoPriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {
        if (symbolFrom == symbolTo)
            return Result.Failure<CoinGeckoPriceHistoryStore>($"Symbols must be of different currency/coin ({symbolFrom}-{symbolTo}).");

        var symbol = symbolFrom.ToLower(); // CoinGecko uses lowercase symbols like "bitcoin", "ethereum", etc.
        if (symbolFrom == "RNDR")
            symbol = $"render";
        else if (symbolFrom == "UNI")
            symbol = $"uniswap";
        else if (symbolFrom == "BEAM")
            symbol = $"beam-2";
        else if (symbolFrom == "GRT")
            symbol = "the-graph";

        Dictionary<string, CryptoPriceData> dataStore;
        var csvFileName = $"pricedata/{symbol}-{symbolTo}_history.csv";

        if (!File.Exists(csvFileName))
        {
            if (!Directory.Exists("pricedata"))
                Directory.CreateDirectory("pricedata");
            Console.WriteLine($"CSV file not found. Fetching data from CoinGecko API for {symbol}...");
            var candles = await FetchAndSaveDataAsync(csvFileName, symbol, symbolTo.ToLower(), startDate.Date, endDate.Date);
            dataStore = LoadDataFromCandles(candles);
        }
        else
            dataStore = LoadDataFromCsv(csvFileName);

        if (!dataStore.Any())
            return Result.Failure<CoinGeckoPriceHistoryStore>($"Could not get historical prices for symbol {symbol}.");

        return new CoinGeckoPriceHistoryStore(symbol, csvFileName, dataStore);
    }

    private static async Task<IEnumerable<CryptoPriceData>> FetchAndSaveDataAsync(string csvFileName, string symbol, string vsCurrency, DateTime startDate, DateTime endDate)
    {
        using (var writer = new StreamWriter(csvFileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            IEnumerable<CryptoPriceData> candles;

            try
            {
                candles = await FetchHistoricalDataAsync(symbol, vsCurrency, startDate, endDate);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not fetch historical prices for symbol {symbol} from CoinGecko.");

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

    private static async Task<IEnumerable<CryptoPriceData>> FetchHistoricalDataAsync(string symbol, string vsCurrency, DateTime startDate, DateTime endDate)
    {
        using (var httpClient = new HttpClient())
        {
            string url = $"https://api.coingecko.com/api/v3/coins/{symbol}/market_chart/range?vs_currency={vsCurrency}&from={new DateTimeOffset(startDate).ToUnixTimeSeconds()}&to={new DateTimeOffset(endDate).ToUnixTimeSeconds()}";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            var jsonData = JObject.Parse(content);
            var prices = jsonData["prices"];

            var candles = new List<CryptoPriceData>();

            foreach (var price in prices)
            {
                var date = DateTimeOffset.FromUnixTimeMilliseconds(price[0].Value<long>()).DateTime;
                if (date < startDate || date > endDate)
                    continue;

                candles.Add(new CryptoPriceData
                {
                    Date = date,
                    Open = price[1].Value<decimal>(), // CoinGecko only provides closing price in their public API
                    High = price[1].Value<decimal>(),
                    Low = price[1].Value<decimal>(),
                    Close = price[1].Value<decimal>(),
                    Volume = 0 // CoinGecko's public API doesn't provide volume in this endpoint
                });
            }

            return candles;
        }
    }

    private static Dictionary<string, CryptoPriceData> LoadDataFromCandles(IEnumerable<CryptoPriceData> candles)
    {
        var dataStore = new Dictionary<string, CryptoPriceData>();

        foreach (var candle in candles)
        {
            dataStore[candle.Date.ToString("yyyy-MM-dd")] = candle;
        }

        return dataStore;
    }

    private static Dictionary<string, CryptoPriceData> LoadDataFromCsv(string csvFileName)
    {
        var dataStore = new Dictionary<string, CryptoPriceData>();

        using (var reader = new StreamReader(csvFileName))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<CryptoPriceData>();
            foreach (var record in records)
            {
                dataStore[record.Date.ToString("yyyy-MM-dd")] = record;
            }
        }

        return dataStore;
    }

    public async Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date)
    {
        if (_dataStore.TryGetValue(date.ToString("yyyy-MM-dd"), out var priceData))
        {
            return priceData;
        }
        else
        {
            Console.WriteLine($"No data found for {date:yyyy-MM-dd}. Fetching missing data...");

            IEnumerable<CryptoPriceData> candles;
            try
            {
                candles = await FetchHistoricalDataAsync(_symbol, "usd", date, date);
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException()?.Message ?? string.Empty);
                Log.Warning($"Could not fetch historical prices for symbol {_symbol} from CoinGecko.");
                candles = new List<CryptoPriceData>();
            }

            var fetchedData = candles.FirstOrDefault();
            if (fetchedData != null)
            {
                priceData = fetchedData;

                // Update the in-memory store and save to CSV
                _dataStore[date.ToString("yyyy-MM-dd")] = priceData;
                SaveDataToCsv();

                return priceData;
            }

            return Result.Failure<CryptoPriceData>($"No data available for {_symbol} on {date:yyyy-MM-dd} from CoinGecko.");
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
