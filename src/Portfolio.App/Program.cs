using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Portfolio.Kraken;

namespace Portfolio.App;

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


            // 1, Get list of all holdings, with the dates of the first and last transactions for each one.


            // 2. From those holding and dates, fetch all quotes from Yahoo Finance

            // 3. 



            var processor = new KrakenCsvParser(filename: "sample.csv");
            var transactions = processor.ExtractTransactions();

            var krakenWalletResult = Wallet.Create("kraken", transactions);
            if(krakenWalletResult.IsFailure)
                throw new Exception(krakenWalletResult.Error);

            var portfolio = new Portfolio(new YahooFinancePriceHistoryStoreFactory());
            var addWalletResult = portfolio.AddWallet(krakenWalletResult.Value);
            if(addWalletResult.IsFailure)
                throw new Exception(addWalletResult.Error);
            
            var processResult = await portfolio.Process();
            if(processResult.IsFailure)
                throw new Exception(processResult.Error);
                           
            portfolio.CheckForMissingTransactions();

            foreach(var h in portfolio.Holdings)
            {
                Log.Information($"{h.Asset},{h.Balance},{h.AverageBoughtPrice}");
            }
    }
}