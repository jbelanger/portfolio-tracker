namespace Portfolio;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using YahooFinanceApi;

public class CryptoPriceData
{
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class HistoricalPriceDataStore
{
    private readonly Dictionary<DateTime, CryptoPriceData> _dataStore;

    public HistoricalPriceDataStore(string csvFileName, string symbol, DateTime startDate, DateTime endDate)
    {
        if (!File.Exists(csvFileName))
        {
            Console.WriteLine($"CSV file not found. Fetching data from Yahoo Finance for {symbol}...");
            var candles = FetchAndSaveDataAsync(csvFileName, symbol, startDate, endDate).Result;
            _dataStore = LoadDataFromCandles(candles);
        }
        else
        {
            _dataStore = LoadDataFromCsv(csvFileName);
        }
    }

    private async Task<IEnumerable<Candle>> FetchAndSaveDataAsync(string csvFileName, string symbol, DateTime startDate, DateTime endDate)
    {
        var candles = await Yahoo.GetHistoricalAsync(symbol, startDate.AddDays(-1), endDate, Period.Daily);

        using (var writer = new StreamWriter(csvFileName))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(candles.Select(candle => new
            {
                Date = candle.DateTime.ToString("yyyy-MM-dd"),
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            }));
        }

        return candles;
    }

    private Dictionary<DateTime, CryptoPriceData> LoadDataFromCandles(IEnumerable<Candle> candles)
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

    private Dictionary<DateTime, CryptoPriceData> LoadDataFromCsv(string csvFileName)
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

    public CryptoPriceData GetPriceData(DateTime date)
    {
        if (_dataStore.TryGetValue(date, out var priceData))
        {
            return priceData;
        }
        else
        {
            return null; // Or handle the case where the data is not found
        }
    }
}
