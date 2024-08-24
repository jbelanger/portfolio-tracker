using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain;
using Portfolio.Domain.Constants;

namespace Portfolio.App.HistoricalPrice
{
    public sealed class CryptoPriceRecordMap : ClassMap<PriceRecord>
    {
        public CryptoPriceRecordMap()
        {
            AutoMap(CultureInfo.InvariantCulture);
            Map(m => m.CurrencyPair).Ignore();
        }
    }

    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryStorageService"/> that uses CSV files for storing and retrieving historical cryptocurrency price data.
    /// </summary>
    public class FilePriceHistoryStorageService : IPriceHistoryStorageService
    {
        /// <summary>
        /// Gets or sets the location where the CSV files containing historical price data are stored.
        /// </summary>
        public string StorageLocation { get; set; } = "historical_price_data";

        /// <summary>
        /// Loads historical price data for a specified cryptocurrency symbol from a CSV file.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> LoadHistoryAsync(string symbol)
        {
            var csvFileName = $"{StorageLocation}/{symbol}_history.csv";

            if (!File.Exists(csvFileName))
                return Result.Failure<IEnumerable<PriceRecord>>($"File not found: {csvFileName}");

            try
            {
                using var reader = new StreamReader(csvFileName);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<CryptoPriceRecordMap>();

                var records = new List<PriceRecord>();
                await foreach (var record in csv.GetRecordsAsync<PriceRecord>().ConfigureAwait(false))
                {
                    records.Add(record);
                }

                return Result.Success<IEnumerable<PriceRecord>>(records);
            }
            catch (Exception ex)
            {
                Log.ForContext<FilePriceHistoryStorageService>().Error($"[{nameof(FilePriceHistoryStorageService)}.{nameof(LoadHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");

                // File might be corrupt. Delete it.
                File.Delete(csvFileName);

                return Result.Failure<IEnumerable<PriceRecord>>($"Error loading data from CSV.");
            }
        }

        /// <summary>
        /// Saves historical price data for a specified cryptocurrency symbol to a CSV file.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="priceHistory">The historical price data to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<PriceRecord> priceHistory)
        {
            var csvFileName = $"{StorageLocation}/{symbol}_history.csv";

            if (!Directory.Exists(StorageLocation))
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

                return await Task.FromResult(Result.Success()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(FilePriceHistoryStorageService)}.{nameof(SaveHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure($"Error saving data to CSV.");
            }
        }
    }
}
