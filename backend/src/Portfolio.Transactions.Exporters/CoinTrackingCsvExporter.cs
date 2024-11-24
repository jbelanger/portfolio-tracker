using Portfolio.Domain.Entities;

namespace Portfolio.Transactions.Exporters
{
    /// <summary>
    /// Provides functionality to export cryptocurrency transaction data to a CSV file format compatible with CoinTracking.
    /// </summary>
    public class CoinTrackingCsvExporter
    {
        /// <summary>
        /// Provides a dictionary mapping between standard currency abbreviations to CoinTracking-specific asset names.
        /// </summary>
        public static Dictionary<string, string> CoinTrackingSecificAssetNamesDict { get; } = new Dictionary<string, string>
        {
            {"UNI", "UNI2"},
            {"SOL", "SOL2"},
            {"DOT", "DOT2"},
            {"STX", "STX2"},
            {"ATOM", "ATOM2"},
            {"IMX", "IMX2"},
            {"ARB", "ARB5"},
            {"STRK", "STRK2"},
            {"TIA", "TIA3"},
            {"TAO", "TAO6"}
        };

        private readonly IEnumerable<CryptoCurrencyTransaction> _transactions;

        /// <summary>
        /// Initializes a new instance of the CoinTrackingCsvExporter class with the specified transactions.
        /// </summary>
        /// <param name="transactions">A collection of cryptocurrency transactions to export.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided transactions collection is null.</exception>
        public CoinTrackingCsvExporter(IEnumerable<CryptoCurrencyTransaction> transactions)
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
                csvLines.Add("Type,Buy Amount,Buy Currency,Sell Amount,Sell Currency,Fee,Fee Currency,Exchange,Trade-Group,Comment,Date");
            }
            foreach (var tx in _transactions)
            {
                // if(tx.FeeType == FeeType.Network)
                // {
                //     // Cointracking expects network fees to be entered as Other Fee
                //     var otherFee = $"Other Fee,,,{tx.FeeAmount?.AbsoluteAmount},{ConvertToCoinTrackingSymbol(tx.FeeAmount?.CurrencyCode)},,,{tx.Account},,{tx.Note},{tx.DateTime:dd-MM-yyyy hh:mm:ss}";
                //     csvLines.Add(otherFee);
                // }
                csvLines.Add(ToCsvLine(tx));
            }
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
        /// Converts a single CryptoCurrencyTransaction into a CSV line formatted for CoinTracking.
        /// </summary>
        /// <param name="tx">The transaction to convert.</param>
        /// <returns>A string representing the transaction in CoinTracking CSV format.</returns>
        private string ToCsvLine(CryptoCurrencyTransaction tx)
        {
            return "";
            // var received = tx.ReceivedAmount;
            // var sent = tx.SentAmount;
            // var isSendingFee = tx.SentAmount?.CurrencyCode == tx.FeeAmount?.CurrencyCode;
            // if (tx.FeeAmount.AbsoluteAmount > 0 && (tx.Type == TransactionType.Withdrawal || tx.Type == TransactionType.Trade && isSendingFee))
            // {
            //     sent = sent?.Add(tx.FeeAmount);
            // }
            // return $"{tx.Type.ToString()},{received?.AbsoluteAmount},{ConvertToCoinTrackingSymbol(received?.CurrencyCode)},{sent?.AbsoluteAmount},{ConvertToCoinTrackingSymbol(sent?.CurrencyCode)},{tx.FeeAmount?.AbsoluteAmount},{ConvertToCoinTrackingSymbol(tx.FeeAmount?.CurrencyCode)},{tx.Account},{string.Join("|", tx.TransactionIds.Select(x => x))},{tx.Note},{tx.DateTime:dd-MM-yyyy HH:mm:ss UTC}";
        }

        /// <summary>
        /// Converts a standard currency symbol to a CoinTracking-specific asset name, if a mapping exists.
        /// </summary>
        /// <param name="symbol">The currency symbol to convert.</param>
        /// <returns>The CoinTracking-specific asset name or the original symbol if no mapping exists.</returns>
        private string? ConvertToCoinTrackingSymbol(string? symbol)
        {
            if (!string.IsNullOrEmpty(symbol) && CoinTrackingSecificAssetNamesDict.ContainsKey(symbol)) return CoinTrackingSecificAssetNamesDict[symbol];
            return symbol;
        }
    }
}
