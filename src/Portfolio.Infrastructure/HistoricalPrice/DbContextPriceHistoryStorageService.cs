using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Portfolio.App.Common.Interfaces;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Serilog;

namespace Portfolio.Infrastructure.HistoricalPrice
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryStorageService"/> that uses Entity Framework Core for storing and retrieving historical cryptocurrency price data.
    /// </summary>
    public class DbContextPriceHistoryStorageService : IPriceHistoryStorageService
    {
        private readonly IApplicationDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextPriceHistoryStorageService"/> class with the specified DbContext.
        /// </summary>
        /// <param name="dbContext">The DbContext used to access the database.</param>
        public DbContextPriceHistoryStorageService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Retrieves the price record for a specific cryptocurrency symbol on a given date from the database.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="date">The date of the price record.</param>
        /// <returns>A <see cref="Result{T}"/> containing the <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<PriceRecord>> GetPriceAsync(string symbol, DateTime date)
        {
            try
            {
                var record = await _dbContext.PriceHistoryRecords
                    .Where(r => r.CurrencyPair == symbol && r.CloseDate.Date == date.Date)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (record != null)
                {
                    return Result.Success(record);
                }

                return Result.Failure<PriceRecord>($"No record found for {symbol} on {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(DbContextPriceHistoryStorageService)}.{nameof(GetPriceAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure<PriceRecord>("Error retrieving data from the database.");
            }
        }

        /// <summary>
        /// Saves or updates historical price data for a specified cryptocurrency symbol to the database.
        /// Existing records for the same date range will be deleted and replaced with the new data.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="priceHistory">The historical price data to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<PriceRecord> priceHistory)
        {
            try
            {
                var firstRecord = priceHistory.OrderBy(ph => ph.CloseDate).FirstOrDefault();
                var lastRecord = priceHistory.OrderBy(ph => ph.CloseDate).LastOrDefault();

                if (firstRecord == null || lastRecord == null)
                {
                    return Result.Success(); // Nothing to save
                }

                // Delete existing records in the date range
                await _dbContext.PriceHistoryRecords
                    .Where(p => p.CurrencyPair == symbol && p.CloseDate >= firstRecord.CloseDate && p.CloseDate <= lastRecord.CloseDate)
                    .ExecuteDeleteAsync();

                // Insert the new records
                await _dbContext.PriceHistoryRecords.AddRangeAsync(priceHistory);
                await _dbContext.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(DbContextPriceHistoryStorageService)}.{nameof(SaveHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure("Error saving data to the database.");
            }
        }
    }
}
