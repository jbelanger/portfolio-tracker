using Microsoft.Data.Sqlite;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.HistoricalPrice
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryStorageService"/> that uses SQLite for storing and retrieving historical cryptocurrency price data.
    /// </summary>
    public class SQLitePriceHistoryStorageService : IPriceHistoryStorageService
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLitePriceHistoryStorageService"/> class with the specified database file path.
        /// </summary>
        /// <param name="databaseFilePath">The file path to the SQLite database.</param>
        public SQLitePriceHistoryStorageService(string databaseFilePath)
        {
            _connectionString = $"Data Source={databaseFilePath};";
            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the SQLite database by creating the required table if it does not already exist.
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS CryptoPriceRecords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CurrencyPair TEXT NOT NULL,
                    CloseDate TEXT NOT NULL,
                    ClosePrice REAL NOT NULL,
                    UNIQUE(CurrencyPair, CloseDate)
                );";

            using var command = new SqliteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves the price record for a specific cryptocurrency symbol on a given date from the SQLite database.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="date">The date of the price record.</param>
        /// <returns>A <see cref="Result{T}"/> containing the <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<PriceRecord>> GetPriceAsync(string symbol, DateTime date)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                string query = @"
                    SELECT CurrencyPair, CloseDate, ClosePrice 
                    FROM CryptoPriceRecords 
                    WHERE CurrencyPair = @CurrencyPair AND CloseDate = @CloseDate;";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@CurrencyPair", symbol);
                command.Parameters.AddWithValue("@CloseDate", date.ToString("yyyy-MM-dd"));

                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var priceRecord = new PriceRecord
                    {
                        CurrencyPair = reader.GetString(0),
                        CloseDate = DateTime.Parse(reader.GetString(1)),
                        ClosePrice = reader.GetDecimal(2)
                    };

                    return Result.Success(priceRecord);
                }

                return Result.Failure<PriceRecord>($"No record found for {symbol} on {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(SQLitePriceHistoryStorageService)}.{nameof(GetPriceAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure<PriceRecord>($"Error retrieving data from SQLite.");
            }
        }

        /// <summary>
        /// Saves or updates historical price data for a specified cryptocurrency symbol to the SQLite database.
        /// Existing records for the same date will be updated.
        /// </summary>
        /// <param name="symbol">The symbol of the cryptocurrency (e.g., "BTC/USD").</param>
        /// <param name="priceHistory">The historical price data to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<PriceRecord> priceHistory)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync().ConfigureAwait(false);

                using var transaction = connection.BeginTransaction();

                string upsertQuery = @"
                    INSERT OR REPLACE INTO CryptoPriceRecords (CurrencyPair, CloseDate, ClosePrice) 
                    VALUES (@CurrencyPair, @CloseDate, @ClosePrice);";

                using var command = new SqliteCommand(upsertQuery, connection, transaction);

                foreach (var record in priceHistory)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@CurrencyPair", symbol);
                    command.Parameters.AddWithValue("@CloseDate", record.CloseDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@ClosePrice", record.ClosePrice);

                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                transaction.Commit();

                return Result.Success();
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(SQLitePriceHistoryStorageService)}.{nameof(SaveHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure("Error saving data to SQLite.");
            }
        }
    }
}
