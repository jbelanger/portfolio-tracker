using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Portfolio.Kraken;

internal class Program
{
    private static void Main(string[] args)
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

            var processor = new KrakenCsvParser(filename: "sample.csv");
            var transactions = processor.ExtractTransactions();

            var portfolio = new Portfolio.Portfolio();
            portfolio.AddTransactions(transactions);

            var holdings = portfolio.GetHoldings();
    }
}