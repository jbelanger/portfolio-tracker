using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Portfolio.Coinbase
{
    class Program
    {
        static void Main(string[] args)
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

            var processor = new CoinbaseCsvParser(filename: "sample.csv");
            var transactions = processor.ExtractTransactions();

            var koinlyExporter = new KoinlyCsvExporter(transactions);
            koinlyExporter.WriteToFile("coinbase-koinly.csv");
            
            var cointrackerExporter = new CoinTrackerCsvExporter(transactions);
            cointrackerExporter.WriteToFile("coinbase-cointracker.csv");
    
            var cointrackingExporter = new CoinTrackingCsvExporter(transactions);
            cointrackingExporter.WriteToFile("coinbase-cointracking.csv");
            // PortfolioPerformance
            // Date,Type,Value,Transaction Currency,Gross Amount,Currency Gross Amount,Exchange Rate,Fees,Taxes,Shares,ISIN,WKN,Ticker Symbol,Security Name,Note
        }


    }
}


