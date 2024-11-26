using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Transactions.Importers.Utilities;
using Serilog;

namespace Portfolio.Transactions.Importers.Csv.Kraken
{
    public class KrakenCsvParser
    {
        public static readonly string EXPECTED_FILE_HEADER = "txid,refid,time,type,subtype,aclass,asset,wallet,amount,fee,balance";
        private readonly IEnumerable<KrakenCsvEntry> _csvLines;
        private readonly IEnumerable<string>? _refidsToIgnore = new List<string>();

        public static Result<KrakenCsvParser> Create(StreamReader streamReader, IEnumerable<string>? ignoreRefIds = null)
        {
            var streamValidResult = StreamReaderValidator.ValidateStreamReader(streamReader);
            if (streamValidResult.IsFailure)
                return Result.Failure<KrakenCsvParser>(streamValidResult.Error);

            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };

                using (var csv = new CsvReader(streamReader, config))
                {
                    csv.Context.RegisterClassMap<KrakenCsvLineMap>();
                    var records = csv.GetRecords<KrakenCsvEntry>();
                    return new KrakenCsvParser(records.ToList(), ignoreRefIds);
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<KrakenCsvParser>($"Failed to import transactions from CSV: {ex.Message}");
            }
        }

        private KrakenCsvParser(IEnumerable<KrakenCsvEntry> csvLines, IEnumerable<string>? ignoreRefIds = null)
        {
            _csvLines = csvLines ?? throw new ArgumentNullException(nameof(csvLines));
            _refidsToIgnore = ignoreRefIds;
        }

        public IEnumerable<FinancialTransaction> ExtractTransactions()
        {
            var rawLedger = _csvLines.Where(x => _refidsToIgnore == null || !_refidsToIgnore.Any() || !_refidsToIgnore.Contains(x.ReferenceId)).OrderBy(x => x.Date).ToList();
            var transactions = new List<FinancialTransaction>();

            // Trades
            var trades = ProcessTrades(rawLedger);
            var processedRefIds = trades.SelectMany(x => x.TransactionIds).ToList();
            transactions.AddRange(trades);

            // Staking
            var stakes = ProcessStaking(rawLedger.Where(x => !processedRefIds.Contains(x.ReferenceId)));
            processedRefIds.AddRange(stakes.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(stakes);

            // Deposits
            var deposits = ProcessDeposits(rawLedger.Where(x => !processedRefIds.Contains(x.ReferenceId)));
            processedRefIds.AddRange(deposits.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(deposits);

            // Withdrawals
            var withdrawals = ProcessWithdrawals(rawLedger.Where(x => !processedRefIds.Contains(x.ReferenceId)));
            processedRefIds.AddRange(withdrawals.SelectMany(x => x.TransactionIds).ToList());
            transactions.AddRange(withdrawals);

            var notProcessed = rawLedger.Select(t => t.ReferenceId).Except(processedRefIds);
            foreach (var tx in notProcessed)
            {
                Log.Warning($"Unprocessed transaction with refId: {tx}");
            }

            return transactions.OrderBy(t => t.DateTime);
        }

        private static IEnumerable<FinancialTransaction> ProcessWithdrawals(IEnumerable<KrakenCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
            var processedRefIds = new HashSet<string>();
            var withdrawals = rawLedger.Where(x => x.Type == "withdrawal");

            // Ensure data is valid
            if (!withdrawals.All(x => x.Amount.Amount < 0))
            {
                var invalids = withdrawals.Where(x => x.Amount.Amount > 0);
                Log.Warning($"Positive withdrawals found: {string.Join("|", invalids.Select(x => x.ReferenceId))}");
            }

            foreach (var withdraw in withdrawals)
            {
                var withdrawResult = FinancialTransaction.CreateWithdraw(
                    date: withdraw.Date,
                    sentAmount: withdraw.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: withdraw.Fee.ToAbsoluteAmountMoney(),
                    "kraken",
                    transactionIds: [withdraw.ReferenceId]);

                if (withdrawResult.IsFailure)
                    throw new ArgumentException(withdrawResult.Error);

                withdrawResult.Value.CsvLinesJson = JsonSerializer.Serialize(new KrakenCsvEntry[] { withdraw });

                transactions.Add(withdrawResult.Value);
            }

            return transactions;
        }

        private static IEnumerable<FinancialTransaction> ProcessDeposits(IEnumerable<KrakenCsvEntry> rawLedger)
        {
            var transactions = new List<FinancialTransaction>();
            var deposits = rawLedger.Where(x => x.Type == "deposit");

            // Ensure data is valid
            if (!deposits.All(x => x.Amount.Amount > 0))
            {
                var invalids = deposits.Where(x => x.Amount.Amount < 0);
                Log.Warning($"Negative deposits found: {string.Join("|", invalids.Select(x => x.ReferenceId))}");
            }

            foreach (var deposit in deposits)
            {
                var depositResult = FinancialTransaction.CreateDeposit(
                    date: deposit.Date,
                    receivedAmount: deposit.Amount.ToAbsoluteAmountMoney().Subtract(deposit.Fee.ToAbsoluteAmountMoney()),
                    feeAmount: deposit.Fee.ToAbsoluteAmountMoney(),
                    "kraken",
                    transactionIds: [deposit.ReferenceId]);

                if (depositResult.IsFailure)
                    throw new ArgumentException(depositResult.Error);

                depositResult.Value.CsvLinesJson = JsonSerializer.Serialize(new KrakenCsvEntry[] { deposit });

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
        private static IEnumerable<FinancialTransaction> ProcessStaking(IEnumerable<KrakenCsvEntry> rawLedger)
        {
            var stakes = new List<FinancialTransaction>();

            // Get all trades that are of the same amount, at the same time
            var assetAmountDateGroups = rawLedger
                .GroupBy(x => new { x.Asset, x.Amount, Date = x.Date.Date })
                .Where(group => group.Count() > 1);

            foreach (var group in assetAmountDateGroups)
            {
                Log.Warning($"Skipping staking transactions {string.Join("|", group.Select(x => x.ReferenceId))}, not supported at this time ");
            }

            return stakes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawLedger"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static IEnumerable<FinancialTransaction> ProcessTrades(IEnumerable<KrakenCsvEntry> rawLedger)
        {
            var trades = new List<FinancialTransaction>();
            var processedRefIds = new HashSet<string>();
            var txGroupsByRefid = rawLedger.GroupBy(x => x.ReferenceId).Where(group => group.Count() > 1);
            foreach (var group in txGroupsByRefid)
            {
                var receiveTx = group.Single(l => l.Amount.Amount > 0 && (l.Type == "receive" || l.Type == "trade"));
                if (group.Count() > 2)
                {
                    // Kraken sometimes offer to convert small amounts to fiat currency.
                    // In those cases, we can see multiple "spend" for one "receive".
                    if (receiveTx.Amount.Amount < 1)
                    {
                        var depositResult = FinancialTransaction.CreateDeposit(
                            date: receiveTx.Date,
                            receivedAmount: receiveTx.Amount.ToAbsoluteAmountMoney().Subtract(receiveTx.Fee.ToAbsoluteAmountMoney()),
                            feeAmount: receiveTx.Fee.ToAbsoluteAmountMoney(),
                            "kraken",
                            transactionIds: group.Select(t => t.ReferenceId),
                            "dustsweeping"
                        );

                        if (depositResult.IsFailure)
                            throw new ArgumentException(depositResult.Error);

                        depositResult.Value.CsvLinesJson = JsonSerializer.Serialize(new KrakenCsvEntry[] { receiveTx });
                        trades.Add(depositResult.Value);

                        foreach (var sTx in group.Where(t => t != receiveTx))
                        {
                            var withdrawResult = FinancialTransaction.CreateWithdraw(
                                date: sTx.Date,
                                sentAmount: sTx.Amount.ToAbsoluteAmountMoney(),
                                feeAmount: sTx.Fee.ToAbsoluteAmountMoney(),
                                "kraken",
                                transactionIds: group.Select(t => t.ReferenceId),
                                "dustsweeping"
                                );

                            if (withdrawResult.IsFailure)
                                throw new ArgumentException(withdrawResult.Error);

                            withdrawResult.Value.CsvLinesJson = JsonSerializer.Serialize(new KrakenCsvEntry[] { sTx });
                            trades.Add(withdrawResult.Value);
                        }
                        Log.Warning($"Dust sweeping found, deposit created for {receiveTx.Amount.Amount}{receiveTx.Amount.CurrencyCode}, refid {group.Key} ...");
                        continue;
                    }
                    else
                    {
                        throw new InvalidDataException($"A trade involving more than two currency has been detected. Review the transaction with refid {receiveTx.ReferenceId}");
                    }
                }

                var spendTx = group.Single(l => l.Amount.Amount < 0 && (l.Type == "spend" || l.Type == "trade"));
                decimal receivedAmount = receiveTx.Amount.AbsoluteAmount;
                Money fee = spendTx.Fee.AbsoluteAmount > 0 ? spendTx.Fee.ToAbsoluteAmountMoney() : receiveTx.Fee.ToAbsoluteAmountMoney();

                var isSellTransaction = receiveTx.Amount.IsFiatCurrency && !spendTx.Amount.IsFiatCurrency;
                var isReceiveFee = receiveTx.Amount.CurrencyCode == receiveTx.Fee.CurrencyCode && receiveTx.Fee.Amount > 0;
                if (isReceiveFee/* && isSellTransaction*/)
                    receivedAmount -= fee.AbsoluteAmount;

                var tradeResult = FinancialTransaction.CreateTrade(
                    date: receiveTx.Date,
                    receivedAmount: new Money(receivedAmount, receiveTx.Amount.CurrencyCode),
                    sentAmount: spendTx.Amount.ToAbsoluteAmountMoney(),
                    feeAmount: fee.ToAbsoluteAmountMoney(),
                    "kraken",
                    transactionIds: [spendTx.ReferenceId, receiveTx.ReferenceId]);

                if (tradeResult.IsFailure)
                    throw new ArgumentException(tradeResult.Error);

                tradeResult.Value.CsvLinesJson = JsonSerializer.Serialize(new KrakenCsvEntry[] { receiveTx, spendTx });

                trades.Add(tradeResult.Value);
            }

            return trades;
        }
    }
}