using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    private YahooFinancePriceHistoryStore(string symbol, string csvFileName, Dictionary<DateTime, CryptoPriceData> dataStore)
    {
        _csvFileName = csvFileName;
        _symbol = symbol;
        _dataStore = dataStore;
    }

    public static async Task<Result<YahooFinancePriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {
        if (symbolFrom == symbolTo)
            return Result.Failure<YahooFinancePriceHistoryStore>("Symbols must be of different currency/coin.");

        var symbol = $"{symbolFrom}-{symbolTo}";
        if (FiatCurrencies.Codes.Contains(symbolFrom) && FiatCurrencies.Codes.Contains(symbolTo))
            symbol = $"{symbolFrom}{symbolTo}=X";
        else if (symbolFrom == "IMX")
            symbol = $"IMX10603-{symbolTo}";

        Dictionary<DateTime, CryptoPriceData> dataStore;
        var csvFileName = $"pricedata/{symbol}_history.csv";

        if (!File.Exists(csvFileName))
        {
            Console.WriteLine($"CSV file not found. Fetching data from Yahoo Finance for {symbol}...");
            var candles = await FetchAndSaveDataAsync(csvFileName, symbol, startDate, endDate);
            dataStore = LoadDataFromCandles(candles);
        }
        else
            dataStore = LoadDataFromCsv(csvFileName);

        if(!dataStore.Any())
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

    public async Task<CryptoPriceData> GetPriceDataAsync(DateTime date)
    {
        if (_dataStore.TryGetValue(date, out var priceData))
        {
            return priceData;
        }
        else
        {
            Console.WriteLine($"No data found for {date:yyyy-MM-dd}. Fetching missing data...");

            var candle = await Yahoo.GetHistoricalAsync(_symbol, date, date, Period.Daily);
            var fetchedData = candle.FirstOrDefault();

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
            else
            {
                Console.WriteLine($"No data available for {date:yyyy-MM-dd} from Yahoo Finance.");
                return null;
            }
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