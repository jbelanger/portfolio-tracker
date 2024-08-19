using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Transactions.Exporters
{
    /// <summary>
    /// Provides functionality to export cryptocurrency transaction data to a CSV file format compatible with Koinly.
    /// </summary>
    public class KoinlyCsvExporter
    {
        private readonly IEnumerable<CryptoCurrencyTransaction> _transactions;

        /// <summary>
        /// Initializes a new instance of the KoinlyCsvExporter class with the specified transactions.
        /// </summary>
        /// <param name="transactions">A collection of cryptocurrency transactions to export.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided transactions collection is null.</exception>
        public KoinlyCsvExporter(IEnumerable<CryptoCurrencyTransaction> transactions)
        {
            _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        }

        /// <summary>
        /// Generates the lines of the CSV file containing the transaction data.
        /// </summary>
        /// <param name="withHeader">Determines whether to include column headers in the CSV output.</param>
        /// <returns>An IEnumerable of strings, each representing a line in the CSV file.</returns>
        public IEnumerable<string> GetCsvLines(bool withHeader = true)
        {
            var csvLines = new List<string>();
            if (withHeader)
            {
                csvLines.Add("Date,Sent Amount,Sent Currency,Received Amount,Received Currency,Fee Amount,Fee Currency,Net Worth Amount,Net Worth Currency,Label,Description,TxHash");
            }
            csvLines.AddRange(_transactions.Select(t => ToCsvLine(t)));
            return csvLines;
        }

        /// <summary>
        /// Writes the generated CSV lines to a file at the specified path.
        /// </summary>
        /// <param name="filepath">The file path where the CSV data will be written.</param>
        public void WriteToFile(string filepath)
        {
            File.WriteAllLines(filepath, GetCsvLines());
        }

        /// <summary>
        /// Converts a single CryptoCurrencyTransaction into a CSV line formatted for Koinly.
        /// </summary>
        /// <param name="tx">The transaction to convert.</param>
        /// <returns>A string representing the transaction in Koinly CSV format.</returns>
        private string ToCsvLine(CryptoCurrencyTransaction tx)
        {            
            Money? inAmount = null;
            Money? outAmount = null;
            var label = tx.Note;

            switch (tx)
            {
                case CryptoCurrencyDepositTransaction deposit:
                    inAmount = deposit.Amount;
                    outAmount = null;                    
                    break;
                case CryptoCurrencyWithdrawTransaction wihdraw:
                    inAmount = null;
                    outAmount = wihdraw.Amount;                    
                    break;
                case CryptoCurrencyTradeTransaction trade:
                    var isCryptoToFiat = trade.Amount.IsFiatCurrency && trade.TradeAmount.IsFiatCurrency;
                    var isReceiveFee = trade.Amount.CurrencyCode == trade.FeeAmount?.CurrencyCode;
                    var receiveAmount = isReceiveFee ? trade.Amount.ToAbsoluteAmountMoney().Add(trade.FeeAmount.ToAbsoluteAmountMoney()) : trade.Amount;
                    inAmount = receiveAmount;
                    outAmount = trade.TradeAmount;                    
                    break;
            }

            return $"{tx.DateTime:yyyy-MM-dd HH:mm:ss UTC},{outAmount?.AbsoluteAmount},{outAmount?.CurrencyCode},{inAmount?.AbsoluteAmount},{inAmount?.CurrencyCode},{tx.FeeAmount?.AbsoluteAmount},{tx.FeeAmount?.CurrencyCode},,,{label},{tx.Account},{string.Join("|", tx.TransactionIds.Select(x => x))}"; ;
        }
    }
}
