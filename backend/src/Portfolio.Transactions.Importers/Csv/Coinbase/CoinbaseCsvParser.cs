using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain.Entities;
using Portfolio.Transactions.Importers.Utilities;
using Serilog;

namespace Portfolio.Transactions.Importers.Csv.Coinbase
{
    public class CoinbaseCsvParser
    {
        public static readonly string EXPECTED_FILE_HEADER = "ID,Timestamp,Transaction Type,Asset,Quantity Transacted,Price Currency,Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes";
        private readonly IEnumerable<CoinbaseCsvEntry> _csvLines;
        private readonly IEnumerable<string>? _refidsToIgnore = new List<string>();

        public static Result<CoinbaseCsvParser> Create(StreamReader streamReader, IEnumerable<string>? ignoreRefIds = null)
        {
            var streamValidResult = StreamReaderValidator.ValidateStreamReader(streamReader);
            if (streamValidResult.IsFailure)
                return Result.Failure<CoinbaseCsvParser>(streamValidResult.Error);

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    IgnoreBlankLines = true,
                    PrepareHeaderForMatch = args => args.Header.Trim().ToLower()
                };

                // Skip lines until the header is reached
                //var headerResult = SkipToHeader(streamReader, EXPECTED_FILE_HEADER);
                //if (headerResult.IsFailure)
                //    return Result.Failure<CoinbaseCsvParser>(headerResult.Error);

                using (var csv = new CsvReader(streamReader, config))
                {
                    // Skip lines until the expected header is found
                    bool headerFound = false;
                    while (csv.Read())
                    {
                        if (csv.Parser.Record != null && string.Join(",", csv.Parser.Record).Trim().ToLower().StartsWith(EXPECTED_FILE_HEADER.ToLower()))
                        {
                            headerFound = true;
                            break;
                        }
                    }

                    if (!headerFound)
                    {
                        return Result.Failure<CoinbaseCsvParser>("The expected header was not found in the CSV file.");
                    }
                    csv.ReadHeader();
                    csv.Context.RegisterClassMap<CoinbaseCsvLineMap>();
                    var records = csv.GetRecords<CoinbaseCsvEntry>();
                    return new CoinbaseCsvParser(records.ToList(), ignoreRefIds);
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<CoinbaseCsvParser>($"Failed to import transactions from CSV: {ex.Message}");
            }
        }

        private CoinbaseCsvParser(IEnumerable<CoinbaseCsvEntry> csvLines, IEnumerable<string>? ignoreRefIds = null)
        {
            _csvLines = csvLines?.OrderBy(x => x.Date) ?? throw new ArgumentNullException(nameof(csvLines));
            _refidsToIgnore = ignoreRefIds;
        }

        public IEnumerable<FinancialTransaction> ExtractTransactions()
        {
            var rawLedger = _csvLines.Where(x => _refidsToIgnore == null || !_refidsToIgnore.Any() || !_refidsToIgnore.Contains(x.TransactionId)).OrderBy(x => x.Date).ToList();
            var transactions = new List<FinancialTransaction>();

            // Deposits
            var deposits = ProcessDeposits(rawLedger);
            var processedRefIds = deposits.SelectMany(x => x.TransactionIds).ToList();
            transactions.AddRange(deposits);

            // Withdrawals
            var withdrawals = ProcessWithdrawals(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(withdrawals.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(withdrawals);

            // Trades
            var trades = ProcessTrades(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(trades.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(trades);

            // Staking
            var stakes = ProcessStaking(rawLedger.Where(x => !processedRefIds.Contains(x.TransactionId)));
            processedRefIds.AddRange(stakes.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(stakes);

            return transactions;
        }

        private static IEnumerable<FinancialTransaction> ProcessWithdrawals(IEnumerable<CoinbaseCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
            var processedRefIds = new HashSet<string>();
            var sentMoneyTxs = rawLedger.Where(x => x.Type == "Send");

            foreach (var withdraw in sentMoneyTxs)
            {
                var withdrawResult = FinancialTransaction.CreateWithdraw(
                    date: withdraw.Date,
                    sentAmount: withdraw.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: withdraw.Fee.ToAbsoluteAmountMoney(),
                    "coinbase",
                    transactionIds: [withdraw.TransactionId]);

                if (withdrawResult.IsFailure)
                    throw new ArgumentException(withdrawResult.Error);

                withdrawResult.Value.CsvLinesJson = JsonSerializer.Serialize(new CoinbaseCsvEntry[] { withdraw });

                transactions.Add(withdrawResult.Value);
            }

            var withdrawals = rawLedger.Where(x => x.Type == "Withdrawal");
            foreach (var withdraw in withdrawals)
            {
                var withdrawResult = FinancialTransaction.CreateWithdraw(
                    date: withdraw.Date,
                    sentAmount: withdraw.Subtotal.ToAbsoluteAmountMoney(),
                    feeAmount: withdraw.Fee.ToAbsoluteAmountMoney(),
                    "coinbase",
                    transactionIds: [withdraw.TransactionId]);

                if (withdrawResult.IsFailure)
                    throw new ArgumentException(withdrawResult.Error);

                withdrawResult.Value.CsvLinesJson = JsonSerializer.Serialize(new CoinbaseCsvEntry[] { withdraw });

                transactions.Add(withdrawResult.Value);
            }

            return transactions;
        }

        private static IEnumerable<FinancialTransaction> ProcessDeposits(IEnumerable<CoinbaseCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
            var deposits = rawLedger.Where(x => x.Type == "Deposit");

            foreach (var deposit in deposits)
            {
                var depositResult = FinancialTransaction.CreateDeposit(
                    date: deposit.Date,
                    receivedAmount: deposit.Subtotal.ToAbsoluteAmountMoney(),
                    feeAmount: deposit.Fee.ToAbsoluteAmountMoney(),
                    "coinbase",
                    transactionIds: [deposit.TransactionId]);

                if (depositResult.IsFailure)
                    throw new ArgumentException(depositResult.Error);

                depositResult.Value.CsvLinesJson = JsonSerializer.Serialize(new CoinbaseCsvEntry[] { deposit });

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
        private static IEnumerable<FinancialTransaction> ProcessStaking(IEnumerable<CoinbaseCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
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
        private static IEnumerable<FinancialTransaction> ProcessTrades(IEnumerable<CoinbaseCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
            var trades = rawLedger.Where(x => x.Type == "Buy");

            foreach (var trade in trades)
            {
                var tradeResult = FinancialTransaction.CreateTrade(
                    date: trade.Date,
                    receivedAmount: trade.Amount.ToAbsoluteAmountMoney(),
                    sentAmount: trade.Subtotal.ToAbsoluteAmountMoney(),
                    feeAmount: trade.Fee.ToAbsoluteAmountMoney(),
                    "coinbase",
                    transactionIds: [trade.TransactionId]);

                if (tradeResult.IsFailure)
                    throw new ArgumentException(tradeResult.Error);

                tradeResult.Value.CsvLinesJson = JsonSerializer.Serialize(new CoinbaseCsvEntry[] { trade });

                transactions.Add(tradeResult.Value);
            }

            var advTrades = rawLedger.Where(x => x.Type == "Advance Trade Sell");
            foreach (var trade in advTrades)
            {
                var tradeResult = FinancialTransaction.CreateTrade(
                    date: trade.Date,
                    receivedAmount: trade.Subtotal.ToAbsoluteAmountMoney(),
                    sentAmount: trade.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: trade.Fee.ToAbsoluteAmountMoney(),
                    "coinbase",
                    transactionIds: [trade.TransactionId]
                    );

                if (tradeResult.IsFailure)
                    throw new ArgumentException(tradeResult.Error);

                tradeResult.Value.CsvLinesJson = JsonSerializer.Serialize(new CoinbaseCsvEntry[] { trade });

                transactions.Add(tradeResult.Value);
            }

            return transactions;
        }

        private static Result SkipToHeader(StreamReader streamReader, string expectedHeader)
        {
            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                if (line.Trim().Equals(expectedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Success();
                }
            }

            return Result.Failure("The expected header was not found in the CSV file.");
        }
    }
}