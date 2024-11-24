using System.Collections.Concurrent;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

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
        private readonly MemoryCache _cache;
        private readonly string StorageLocation = "historical_price_data";
        private readonly ConcurrentDictionary<string, Lazy<Task<IEnumerable<PriceRecord>>>> _loadedFilesCache = new();

        public FilePriceHistoryStorageService(MemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<Result<PriceRecord>> GetPriceAsync(string symbol, DateTime date)
        {
            var cacheKey = $"{symbol}_history";
            var dateKey = $"{symbol}_{date:yyyyMMdd}";

            if (_cache.TryGetValue(dateKey, out PriceRecord? cachedPriceRecord))
            {
                return Result.Success(cachedPriceRecord!);
            }

            var records = await _loadedFilesCache.GetOrAdd(symbol, new Lazy<Task<IEnumerable<PriceRecord>>>(() => LoadFileIntoMemoryAsync(symbol))).Value.ConfigureAwait(false);

            var matchingRecord = records.FirstOrDefault(r => r.CloseDate.Date == date.Date);
            if (matchingRecord == null)
            {
                return Result.Failure<PriceRecord>($"No record found for {symbol} on {date:yyyy-MM-dd}");
            }

            // Cache the found record
            _cache.Set(dateKey, matchingRecord, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return Result.Success(matchingRecord);
        }

        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<PriceRecord> priceHistory)
        {
            var csvFileName = $"{StorageLocation}/{symbol}_history.csv";
            var cacheKey = $"{symbol}_history";

            if (!Directory.Exists(StorageLocation))
                Directory.CreateDirectory(StorageLocation);

            try
            {
                var existingRecords = await LoadFileIntoMemoryAsync(symbol).ConfigureAwait(false);
                var updatedRecords = existingRecords.ToDictionary(r => r.CloseDate.Date);

                // Update or add new records
                foreach (var newRecord in priceHistory)
                {
                    updatedRecords[newRecord.CloseDate.Date] = newRecord;
                }

                // Write updated records back to the file
                using (var writer = new StreamWriter(csvFileName))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<CryptoPriceRecordMap>();
                    csv.WriteRecords(updatedRecords.Values.OrderBy(r => r.CloseDate));
                }

                // Invalidate the cache for this symbol
                _cache.Remove(cacheKey);
                _loadedFilesCache.TryRemove(symbol, out _);

                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(FilePriceHistoryStorageService)}.{nameof(SaveHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure($"Error saving data to CSV.");
            }
        }

        private async Task<IEnumerable<PriceRecord>> LoadFileIntoMemoryAsync(string symbol)
        {
            var csvFileName = $"{StorageLocation}/{symbol}_history.csv";
            if (!File.Exists(csvFileName))
            {
                return Enumerable.Empty<PriceRecord>();
            }

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

                // Cache the entire file contents
                var cacheKey = $"{symbol}_history";
                _cache.Set(cacheKey, records, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return records;
            }
            catch (Exception ex)
            {
                Log.ForContext<FilePriceHistoryStorageService>().Error($"[{nameof(FilePriceHistoryStorageService)}.{nameof(LoadFileIntoMemoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Enumerable.Empty<PriceRecord>();
            }
        }
    }


}
