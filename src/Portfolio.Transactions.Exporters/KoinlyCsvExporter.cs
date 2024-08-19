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
            var sentAmount = tx.SentAmount;
            if (tx.Type == TransactionType.Withdrawal && tx.FeeAmount.AbsoluteAmount > 0)
            {
                sentAmount = sentAmount?.Add(tx.FeeAmount);
            }

            if (tx.TransactionIds.Contains("FTYhknV-XRJwlulHpL7VST9y73uoLH"))
                ;

            var isCryptoToFiat = tx.ReceivedAmount?.IsFiatCurrency == true && tx.SentAmount?.IsFiatCurrency == false;
            var isReceiveFee = tx.ReceivedAmount?.CurrencyCode == tx.FeeAmount?.CurrencyCode;
            var receiveAmount = isReceiveFee ? tx.ReceivedAmount.ToAbsoluteAmountMoney()?.Add(tx.FeeAmount.ToAbsoluteAmountMoney()) : tx.ReceivedAmount;
            var label = tx.Note;//(tx.Type == TransactionType.Fee) ? "Cost" : tx.Note;        
            return $"{tx.DateTime:yyyy-MM-dd HH:mm:ss UTC},{tx.SentAmount?.AbsoluteAmount},{tx.SentAmount?.CurrencyCode},{receiveAmount?.AbsoluteAmount},{tx.ReceivedAmount?.CurrencyCode},{tx.FeeAmount?.AbsoluteAmount},{tx.FeeAmount?.CurrencyCode},,,{label},{tx.Account},{string.Join("|", tx.TransactionIds.Select(x => x))}";
        }
    }
}
