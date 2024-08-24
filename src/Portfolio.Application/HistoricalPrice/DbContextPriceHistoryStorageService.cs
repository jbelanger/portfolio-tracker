using Microsoft.Data.Sqlite;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.Infrastructure;

namespace Portfolio.App.HistoricalPrice
{
    public class DbContextPriceHistoryStorageService : IPriceHistoryStorageService
    {
        private readonly PortfolioDbContext _dbContext;

        public DbContextPriceHistoryStorageService(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Loads historical price data for a specified cryptocurrency symbol from the database.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> LoadHistoryAsync(string symbol)
        {
            var priceHistory = new List<PriceRecord>();
            var historyRecords = await _dbContext.PriceHistoryRecords.Where(P => P.CurrencyPair == symbol).ToListAsync();
            return Result.Success<IEnumerable<PriceRecord>>(priceHistory);
        }

        /// <summary>
        /// Saves historical price data for a specified cryptocurrency symbol to the SQLite database.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="priceHistory">The historical price data to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<PriceRecord> priceHistory)
        {            
            await _dbContext.PriceHistoryRecords.AddRangeAsync(priceHistory);
            await _dbContext.SaveChangesAsync();
            
            return Result.Success();
        }
    }
}
