using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Portfolio.Kraken;

namespace Portfolio
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

            var processor = new KrakenCsvParser(filename: "sample.csv");
            var transactions = processor.ExtractTransactions();

            var koinlyExporter = new KoinlyCsvExporter(transactions);
            koinlyExporter.WriteToFile("kraken-koinly.csv");
            
            var cointrackerExporter = new CoinTrackerCsvExporter(transactions);
            cointrackerExporter.WriteToFile("kraken-cointracker.csv");
    
            var cointrackingExporter = new CoinTrackingCsvExporter(transactions);
            cointrackingExporter.WriteToFile("kraken-cointracking.csv");

            var ctcExporter = new CryptoTaxCalculatorCsvExporter(transactions);
            ctcExporter.WriteToFile("kraken-ctc.csv");

            var blockpitExporter = new BlockpitCsvExporter(transactions);
            blockpitExporter.WriteToFile("kraken-blockpit.csv");

            // PortfolioPerformance
            // Date,Type,Value,Transaction Currency,Gross Amount,Currency Gross Amount,Exchange Rate,Fees,Taxes,Shares,ISIN,WKN,Ticker Symbol,Security Name,Note
        }


    }
}


