using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Portfolio.Kraken;
using Portfolio;

internal class Program
{
    private static async Task Main(string[] args)
    {
            Log.Logger = new LoggerConfiguration()
                // add console as logging target
                .WriteTo.Console()
                // add a logging target for warnings and higher severity  logs
                // structured in JSON format
                .WriteTo.File(new JsonFormatter(), "important.json", restrictedToMinimumLevel: LogEventLevel.Warning)
                // add a rolling file for all logs
                .WriteTo.File("all-.logs", rollingInterval: RollingInterval.Day)
                // set default minimum level
                .MinimumLevel.Debug()
                .CreateLogger();

            LoadCryptoDataStore();


            var processor = new KrakenCsvParser(filename: "sample.csv");
            var transactions = processor.ExtractTransactions();
            transactions = transactions.OrderBy(t => t.DateTime);

            var portfolio = new Portfolio.Portfolio();
            portfolio.AddTransactions(transactions);

            var holdings = await portfolio.GetHoldings();
            portfolio.CheckForMissingTransactions();

            foreach(var h in holdings)
            {
                Log.Information($"{h.Asset},{h.Balance},{h.AverageBoughtPrice}");
            }
    }

        static void LoadCryptoDataStore()
    {
        string symbol = "CADUSD=X";
        string csvFileName = $"{symbol}_history.csv";
        DateTime startDate = new DateTime(2023, 1, 1);
        DateTime endDate = DateTime.Now;

        var dataStore = new HistoricalPriceDataStore(csvFileName, symbol, startDate, endDate);

        DateTime queryDate = new DateTime(2023, 1, 1);
        var priceData = dataStore.GetPriceData(queryDate);

        if (priceData != null)
        {
            Console.WriteLine($"{queryDate:yyyy-MM-dd}: Open: {priceData.Open}, High: {priceData.High}, Low: {priceData.Low}, Close: {priceData.Close}, Volume: {priceData.Volume}");
        }
        else
        {
            Console.WriteLine("No data found for the specified date.");
        }
    }
}