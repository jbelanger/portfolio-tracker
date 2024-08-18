using System.Globalization;
using CsvHelper;
using CSharpFunctionalExtensions;
using Serilog;
using CsvHelper.Configuration;

namespace Portfolio.App.HistoricalPrice;

public sealed class CryptoPriceRecordMap : ClassMap<CryptoPriceRecord>
{
    public CryptoPriceRecordMap()
    {
        AutoMap(CultureInfo.InvariantCulture);
        Map(m => m.CurrencyPair).Ignore();
    }
}

public class LocalFilePriceHistoryStorageService : IPriceHistoryStorageService
{
    public string StorageLocation { get; set; } = "historical_price_data";

    public async Task<Result<IEnumerable<CryptoPriceRecord>>> LoadHistoryAsync(string symbol)
    {
        var csvFileName = $"{StorageLocation}/{symbol}_history.csv";

        if (!File.Exists(csvFileName))
            return Result.Failure<IEnumerable<CryptoPriceRecord>>($"File not found: {csvFileName}");

        try
        {
            using var reader = new StreamReader(csvFileName);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<CryptoPriceRecordMap>();
            return await Task.FromResult(csv.GetRecords<CryptoPriceRecord>().ToList());  
        }
        catch (Exception ex)
        {
            Log.ForContext<LocalFilePriceHistoryStorageService>().Error($"[{nameof(LocalFilePriceHistoryStorageService)}.{nameof(LoadHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
            
            // File might be corrupt. Delete it.
            File.Delete(csvFileName);
            
            return Result.Failure<IEnumerable<CryptoPriceRecord>>($"Error loading data from CSV.");
        }
    }

    public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<CryptoPriceRecord> priceHistory)
    {
        var csvFileName = $"{StorageLocation}/{symbol}_history.csv";

        if(!Directory.Exists(StorageLocation))
            Directory.CreateDirectory(StorageLocation);

        try
        {
            using (var writer = new StreamWriter(csvFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<CryptoPriceRecordMap>();
                csv.WriteRecords(priceHistory.Select(data => new
                {
                    CloseDate = data.CloseDate.ToString(Strings.DATE_FORMAT),
                    data.ClosePrice
                }));
            }

            return await Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            Log.Error($"[{nameof(LocalFilePriceHistoryStorageService)}.{nameof(SaveHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
            return Result.Failure($"Error saving data to CSV.");
        }
    }
}
