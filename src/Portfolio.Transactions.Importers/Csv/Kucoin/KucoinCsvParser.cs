using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Serilog;

namespace Portfolio.Transactions.Importers.Csv.Kucoin
{
    public class KucoinCsvParser
    {
        string TRADING_CSV_HEADERS = "UID,Account Type,Order ID,Order Time(UTC),Symbol,Side,Order Type,Order Price,Order Amount,Avg. Filled Price,Filled Amount,Filled Volume,Filled Volume (USDT),Filled Time(UTC),Fee,Fee Currency,Tax,Status";
        string WITHDRAWAL_CSV_HEADERS = "UID,Account Type,Time(UTC),Remarks,Status,Fee,Amount,Coin,Transfer Network,Withdrawal Address/Account";
        string DEPOSIT_CSV_HEADERS = "UID,Account Type,Time(UTC),Remarks,Status,Fee,Amount,Coin,Transfer Network";

        private string _folderPath;
        private List<KucoinFilledOrderCsvEntry> _tradesCsvLines = new();
        private List<KucoinDepositCsvEntry> _depositCsvLines = new();
        private List<KucoinWithdrawCsvEntry> _withdrawCsvLines = new();

        public KucoinCsvParser(string folderPath)
        {
            _folderPath = folderPath ?? throw new ArgumentNullException("The folder path cannot be null or empty.");
        }

        public IEnumerable<ICryptoCurrencyTransaction> ExtractTransactions()
        {
            List<ICryptoCurrencyTransaction> transactions = new();

            ReadCsvFile();

            var deposits = ProcessDeposits(_depositCsvLines);
            transactions.AddRange(deposits);

            // Withdrawals
            var withdrawals = ProcessWithdrawals(_withdrawCsvLines);
            transactions.AddRange(withdrawals);

            // Trades
            var trades = ProcessTrades(_tradesCsvLines);
            transactions.AddRange(trades);

            // // Staking
            // var stakes = ProcessStaking(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            // processedRefIds.AddRange(stakes.SelectMany(x => x.TransactionIds).ToList());
            // transactions.AddRange(stakes);

            return transactions.OrderBy(tx => tx.DateTime);
        }

        private void ReadCsvFile()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            string[] csvFiles = Directory.GetFiles(_folderPath, "*.csv");

            foreach (string file in csvFiles)
            {
                using (var reader = new StreamReader(file))
                {
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        var headers = csv.HeaderRecord; // Getting the header record
                        string headerString = string.Join(",", headers);
                        if (csv.ColumnCount == 18 && headerString == TRADING_CSV_HEADERS)
                        {
                            csv.Context.RegisterClassMap<KucoinFilledOrderCsvLineMap>();
                            _tradesCsvLines.AddRange(csv.GetRecords<KucoinFilledOrderCsvEntry>().ToList());
                        }
                        else if (csv.ColumnCount == 10 && headerString == WITHDRAWAL_CSV_HEADERS)
                        {
                            csv.Context.RegisterClassMap<KucoinWithdrawCsvLineMap>();
                            _withdrawCsvLines.AddRange(csv.GetRecords<KucoinWithdrawCsvEntry>().ToList());
                        }
                        else if (csv.ColumnCount == 9 && headerString == DEPOSIT_CSV_HEADERS)
                        {
                            csv.Context.RegisterClassMap<KucoinDepositCsvLineMap>();
                            _depositCsvLines.AddRange(csv.GetRecords<KucoinDepositCsvEntry>().ToList());
                        }
                        else
                        {
                            Log.Error($"Unrecognized file {file} with headers: {headerString}");
                        }
                    }
                }
            }
        }

        private static IEnumerable<ICryptoCurrencyTransaction> ProcessWithdrawals(IEnumerable<KucoinWithdrawCsvEntry> withdrawals)
        {
            var transactions = new List<ICryptoCurrencyTransaction>();

            foreach (var withdraw in withdrawals)
            {
                var withdrawResult = CryptoCurrencyWithdrawTransaction.Create(
                    date: withdraw.Date,
                    amount: withdraw.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: withdraw.Fee.ToAbsoluteAmountMoney(),
                    "kucoin",
                    transactionIds: [withdraw.TransactionId],
                    note: withdraw.Remark);

                if (withdrawResult.IsFailure)
                    throw new ArgumentException(withdrawResult.Error);

                withdrawResult.Value.State = new KucoinWithdrawCsvEntry[] { withdraw };

                transactions.Add(withdrawResult.Value);
            }

            return transactions;
        }

        private static IEnumerable<ICryptoCurrencyTransaction> ProcessDeposits(IEnumerable<KucoinDepositCsvEntry> deposits)
        {
            var transactions = new List<ICryptoCurrencyTransaction>();

            foreach (var deposit in deposits)
            {
                var depositResult = CryptoCurrencyDepositTransaction.Create(
                    date: deposit.Date,
                    receivedAmount: deposit.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: deposit.Fee.ToAbsoluteAmountMoney(),
                    "kucoin",
                    transactionIds: [deposit.TransactionId],
                    note: deposit.Remark);

                if (depositResult.IsFailure)
                    throw new ArgumentException(depositResult.Error);

                depositResult.Value.State = new KucoinDepositCsvEntry[] { deposit };


                transactions.Add(depositResult.Value);
            }

            return transactions;
        }

        /// <summary>
        /// One of the best article on taxes and staking:
        /// https://www.mondaq.com/canada/income-tax/1259402/the-canadian-income-tax-and-cryptocurrency-staking-knowing-the-income-tax-consequences-for-canadians-in-the-validation-of-cryptocurrency-transactions-in-proof-of-stake-blockchain
        /// Let's consider an example where you verify transactions for a cryptocurrency platform that uses the proof-of-stake mechanism and stake sufficient units of the native cryptocurrency of the platform. You are rewarded with 
        /// new 2 units of the platform's cryptocurrency as a staking reward for doing this. Those 2 units have a $400 value at the time of issuance. You must record the $400 as business income or investment income in accordance with 
        /// subsection 9(1) of the Income Tax Act of Canada (depending on the appropriate tax characterization). Your tax cost for the staking-reward units is $400 in accordance with subsection 52(1). Your taxable income will be determined 
        /// by the $400 tax cost when you eventually sell the staking-reward units. For instance, your $400 tax cost means that you would generate a profit of $6,600, which you must declare as income or capital gains if you later sell the 
        /// staking-reward units for $7,000 (or trade them for other cryptocurrency tokens worth $7,000).
        /// </summary>
        /// <param name="rawLedger"></param>
        /// <param name="depotTransactions"></param>
        /// <param name="accountTransactions"></param>
        /// <returns></returns>
        private static IEnumerable<ICryptoCurrencyTransaction> ProcessStaking(IEnumerable<KucoinFilledOrderCsvEntry> rawLedger)
        {
            var transactions = new List<ICryptoCurrencyTransaction>();
            var stakes = rawLedger.Where(x => x.Type == "DELEGATE");

            foreach (var stake in stakes)
            {
                Log.Warning($"Skipping staking transaction {stake.TransactionId}, not supported at this time ");
            }

            return transactions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawLedger"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static IEnumerable<ICryptoCurrencyTransaction> ProcessTrades(IEnumerable<KucoinFilledOrderCsvEntry> rawLedger)
        {
            var transactions = new List<ICryptoCurrencyTransaction>();
            var trades = rawLedger.Where(x => x.Type == "BUY");

            foreach (var trade in trades)
            {
                var tradeResult = CryptoCurrencyTradeTransaction.Create(
                    date: trade.Date,
                    receivedAmount: trade.OrderAmount.ToAbsoluteAmountMoney(),
                    sentAmount: trade.FilledVolume.ToAbsoluteAmountMoney(),
                    feeAmount: trade.Fee.ToAbsoluteAmountMoney(),
                    "kucoin",
                    transactionIds: [trade.TransactionId]
                    );

                if (tradeResult.IsFailure)
                    throw new ArgumentException(tradeResult.Error);

                tradeResult.Value.State = new KucoinFilledOrderCsvEntry[] { trade };

                transactions.Add(tradeResult.Value);
            }

            return transactions;
        }
    }
}