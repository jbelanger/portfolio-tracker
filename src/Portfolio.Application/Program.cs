using Serilog.Events;
using Serilog.Formatting.Json;
using Portfolio.Transactions.Importers.Csv.Kraken;
using Portfolio.Domain.Entities;
using Portfolio.App.HistoricalPrice;
using Portfolio.App.HistoricalPrice.YahooFinance;

namespace Portfolio.App
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext() // Allows you to add properties to the log context dynamically
                                         //.Enrich.WithCallerInfo() // Automatically includes method and class names
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

            using (var reader = new StreamReader("sample.csv"))
            {
                var processor = KrakenCsvParser.Create(reader).Value;
                var transactions = processor.ExtractTransactions();


                var krakenWalletResult = Wallet.Create("kraken");
                if (krakenWalletResult.IsFailure)
                    throw new Exception(krakenWalletResult.Error);
                var krakenWallet = krakenWalletResult.Value;
                foreach (var t in transactions)
                {
                    krakenWallet.AddTransaction(t);
                }

                //var storage = new FilePriceHistoryStorageService();

                var storage = new SQLitePriceHistoryStorageService("crypto_price_history.db");


                var api = new YahooFinancePriceHistoryApi();
                var svc = new PriceHistoryService(api, storage, Strings.CURRENCY_USD);
                var portfolio = new Portfolio(svc);
                // portfolio.OnDepositAdded += CheckBalance;
                // portfolio.OnWithdrawAdded += CheckBalance;
                // portfolio.OnTradeAdded += CheckBalance;

                portfolio.Wallets.Add(krakenWalletResult.Value);

                var processResult = await portfolio.ProcessAsync().ConfigureAwait(false);
                if (processResult.IsFailure)
                    throw new Exception(processResult.Error);

                //portfolio.CheckForMissingTransactions();

                foreach (var h in portfolio.Holdings.Where(h => h.Balance > 0))
                {
                    Log.Information($"Currency:{h.Asset}    Balance:{h.Balance:F2}     AvgPrice:{h.AverageBoughtPrice:F2}     Cost:{(h.Balance * h.AverageBoughtPrice):F2}     Value:{(h.Balance * h.CurrentPrice?.Amount):F2}");
                }
            }
        }

        // private static void CheckBalance(CryptoCurrencyRawTransaction transaction, CryptoCurrencyHolding holding)
        // {
        //     var lines = transaction.State as KrakenCsvEntry[];
        //     var line = lines.First();

        //     if (line.Balance.AbsoluteAmount != holding.Balance)
        //         Log.Error($"{transaction.Amount.CurrencyCode} balances do not match. Deposit RefId={transaction.TransactionIds}");
        // }

        // private static void CheckBalance(CryptoCurrencyRawTransaction transaction, CryptoCurrencyHolding holding)
        // {
        //     var lines = transaction.State as KrakenCsvEntry[];
        //     var line = lines.First();

        //     if (line.Balance.AbsoluteAmount != holding.Balance)
        //         Log.Error($"{transaction.Amount.CurrencyCode} balances do not match. Withdraw RefId={transaction.TransactionIds}");
        // }

        // private static void CheckBalance(CryptoCurrencyTradeTransaction transaction, CryptoCurrencyHolding holding)
        // {
        //     var lines = transaction.State as KrakenCsvEntry[];
        //     var receiveTx = lines.First();
        //     var spendTx = lines.ElementAtOrDefault(1);

        //     if (transaction.Amount.CurrencyCode == holding.Asset)
        //     {
        //         if (receiveTx.Balance.AbsoluteAmount != holding.Balance)
        //             Log.Error($"{transaction.Amount.CurrencyCode} balances do not match. Withdraw RefId={transaction.TransactionIds}");
        //     }
        //     else if (transaction.TradeAmount.CurrencyCode == holding.Asset)
        //     {
        //         if (spendTx.Balance.AbsoluteAmount != holding.Balance)
        //             Log.Error($"{transaction.Amount.CurrencyCode} balances do not match. Withdraw RefId={transaction.TransactionIds}");
        //     }
        // }
    }
}