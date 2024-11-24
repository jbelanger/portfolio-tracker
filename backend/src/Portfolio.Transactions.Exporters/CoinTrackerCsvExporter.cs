using Portfolio.Domain.Entities;

namespace Portfolio.Transactions.Exporters
{
    /// <summary>
    /// Provides functionality to export cryptocurrency transaction data to a CSV file format compatible with CoinTracker.
    /// </summary>
    public class CoinTrackerCsvExporter
    {
        private readonly IEnumerable<CryptoCurrencyTransaction> _transactions;
    
        /// <summary>
        /// Initializes a new instance of the CoinTrackerCsvExporter class with the specified transactions.
        /// </summary>
        /// <param name="transactions">A collection of cryptocurrency transactions to export.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided transactions collection is null.</exception>
        public CoinTrackerCsvExporter(IEnumerable<CryptoCurrencyTransaction> transactions)
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
                csvLines.Add("Date,Received Quantity,Received Currency,Sent Quantity,Sent Currency,Fee Amount,Fee Currency,Tag");
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
        /// Converts a single CryptoCurrencyTransaction into a CSV line formatted for CoinTracker.
        /// </summary>
        /// <param name="tx">The transaction to convert.</param>
        /// <returns>A string representing the transaction in CoinTracker CSV format.</returns>
        private string ToCsvLine(CryptoCurrencyTransaction tx)
        {
            // MM/DD/YYYY HH:MM:SS. For example, "06/14/2017 20:57:35".
            return "";// $"{tx.DateTime:MM/dd/yyyy HH:mm:ss UTC},{tx.ReceivedAmount?.AbsoluteAmount},{tx.ReceivedAmount?.CurrencyCode},{tx.SentAmount?.AbsoluteAmount},{tx.SentAmount?.CurrencyCode},{tx.FeeAmount?.AbsoluteAmount},{tx.FeeAmount?.CurrencyCode},";
        }
    }
}
