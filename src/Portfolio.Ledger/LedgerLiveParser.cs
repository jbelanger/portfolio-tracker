using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Serilog;

namespace Portfolio.LedgerLive
{
    public class LedgerLiveCsvParser
    {        
        private string _filename;

        public LedgerLiveCsvParser(string filename)
        {
            _filename = filename ?? throw new ArgumentNullException("The file path cannot be null or empty.");         
        }

        public IEnumerable<CryptoCurrencyTransaction> ExtractTransactions()
        {
            var csvLines = ReadCsvFile();

            var rawLedger = csvLines.OrderBy(x => x.Date).ToList();
            var transactions = new List<CryptoCurrencyTransaction>();

            // Trades
            var trades = ProcessTrades(rawLedger);
            var processedRefIds = trades.SelectMany(x => x.TransactionIds).ToList();
            transactions.AddRange(trades);

            // Staking
            var stakes = ProcessStaking(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(stakes.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(stakes);

            // Deposits
            var deposits = ProcessDeposits(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(deposits.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(deposits);

            // Withdrawals
            var withdrawals = ProcessWithdrawals(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(withdrawals.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(withdrawals);

            return transactions;
        }

        private List<LedgerLiveCsvEntry> ReadCsvFile()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using (var reader = new StreamReader(_filename))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Context.RegisterClassMap<LedgerLiveCsvLineMap>();
                var records = csv.GetRecords<LedgerLiveCsvEntry>();
                return records.ToList();
            }
        }

        private static IEnumerable<CryptoCurrencyTransaction> ProcessWithdrawals(IEnumerable<LedgerLiveCsvEntry> rawLedger)
        {
            var transactions = new List<CryptoCurrencyTransaction>();
            var processedRefIds = new HashSet<string>();
            var withdrawals = rawLedger.Where(x => x.Type == "OUT");

            foreach (var withdraw in withdrawals)
            {
                transactions.Add(CryptoCurrencyTransaction.CreateWithdrawal(
                    date: withdraw.Date,
                    sentAmount: withdraw.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: withdraw.Fee.ToAbsoluteAmountMoney(),
                    "LedgerLive",
                    transactionIds: [withdraw.TransactionId]
                    ));
            }

            return transactions;
        }

        private static IEnumerable<CryptoCurrencyTransaction> ProcessDeposits(IEnumerable<LedgerLiveCsvEntry> rawLedger)
        {
            var transactions = new List<CryptoCurrencyTransaction>();
            var deposits = rawLedger.Where(x => x.Type == "IN");

            foreach (var deposit in deposits)
            {
                transactions.Add(CryptoCurrencyTransaction.CreateDeposit(
                    date: deposit.Date,
                    receivedAmount: deposit.Amount.ToAbsoluteAmountMoney().Subtract(deposit.Fee.ToAbsoluteAmountMoney()),
                    feeAmount: deposit.Fee.ToAbsoluteAmountMoney(),
                    "LedgerLive",
                    transactionIds: [deposit.TransactionId]
                    ));
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
        private static IEnumerable<CryptoCurrencyTransaction> ProcessStaking(IEnumerable<LedgerLiveCsvEntry> rawLedger)
        {
            var transactions = new List<CryptoCurrencyTransaction>();
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
        private static IEnumerable<CryptoCurrencyTransaction> ProcessTrades(IEnumerable<LedgerLiveCsvEntry> rawLedger)
        {
            var transactions = new List<CryptoCurrencyTransaction>();
            // var trades = rawLedger.Where(x => x.Type == "DELEGATE");

            // foreach (var trade in trades)
            // {
            //     Log.Warning($"Skipping trade transaction {trade.TransactionId}, not supported at this time ");
            // }

            return transactions;
        }
    }
}