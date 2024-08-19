using Microsoft.Data.Sqlite;
using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice
{
    public class SQLitePriceHistoryStorageService : IPriceHistoryStorageService
    {
        private readonly string _connectionString;

        public SQLitePriceHistoryStorageService(string databaseFilePath)
        {
            _connectionString = $"Data Source={databaseFilePath};";
            InitializeDatabase();
        }

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

        public async Task<Result<IEnumerable<CryptoPriceRecord>>> LoadHistoryAsync(string symbol)
        {
            var priceHistory = new List<CryptoPriceRecord>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                string query = "SELECT CurrencyPair, CloseDate, ClosePrice FROM CryptoPriceRecords WHERE CurrencyPair = @CurrencyPair ORDER BY CloseDate;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@CurrencyPair", symbol);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var currencyPair = reader.GetString(0);
                    var closeDate = DateTime.Parse(reader.GetString(1));
                    var closePrice = reader.GetDecimal(2);

                    priceHistory.Add(new CryptoPriceRecord
                    {
                        CurrencyPair = currencyPair,
                        CloseDate = closeDate,
                        ClosePrice = closePrice
                    });
                }

                return Result.Success<IEnumerable<CryptoPriceRecord>>(priceHistory);
            }
            catch (Exception ex)
            {
                Log.Error($"[{nameof(SQLitePriceHistoryStorageService)}.{nameof(LoadHistoryAsync)}] An error occurred: {ex.GetBaseException().Message}");
                return Result.Failure<IEnumerable<CryptoPriceRecord>>("Error loading data from SQLite.");
            }
        }


        public async Task<Result> SaveHistoryAsync(string symbol, IEnumerable<CryptoPriceRecord> priceHistory)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                string upsertQuery = @"
                    INSERT OR REPLACE INTO CryptoPriceRecords (CurrencyPair, CloseDate, ClosePrice) 
                    VALUES (@CurrencyPair, @CloseDate, @ClosePrice);";

                using var command = new SqliteCommand(upsertQuery, connection, transaction); // Associate the command with the transaction

                foreach (var record in priceHistory)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@CurrencyPair", record.CurrencyPair);
                    command.Parameters.AddWithValue("@CloseDate", record.CloseDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@ClosePrice", record.ClosePrice);

                    await command.ExecuteNonQueryAsync(); // Command is now correctly associated with the transaction
                }

                transaction.Commit(); // Commit the transaction after all commands are executed

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
