using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Transactions.Exporters
{
    /// <summary>
    /// Provides functionality to export cryptocurrency transaction data to a CSV file format compatible with CoinTracker.
    /// </summary>
    public class CryptoTaxCalculatorCsvExporter
    {
        private readonly IEnumerable<CryptoCurrencyTransaction> _transactions;

        /// <summary>
        /// Initializes a new instance of the CoinTrackerCsvExporter class with the specified transactions.
        /// </summary>
        /// <param name="transactions">A collection of cryptocurrency transactions to export.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided transactions collection is null.</exception>
        public CryptoTaxCalculatorCsvExporter(IEnumerable<CryptoCurrencyTransaction> transactions)
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
                csvLines.Add("Timestamp (UTC),Type,Base Currency,Base Amount,Quote Currency (Optional),Quote Amount (Optional),Fee Currency (Optional),Fee Amount (Optional),From (Optional),To (Optional),Blockchain (Optional),ID (Optional),Description (Optional),Reference Price Per Unit (Optional),Reference Price Currency (Optional)");
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
            return "";
            // string txType = string.Empty;
            // Money? baseMoney = tx.ReceivedAmount;
            // Money? quoteMoney = tx.SentAmount;
            // switch (tx.Type)
            // {
            //     case TransactionType.Trade:
            //         if (tx.TransactionIds.Contains("TS5PSRF-EJ4SC-FQMLN2"))
            //             ;

            //         if (tx.SentAmount?.IsFiatCurrency == true)
            //         {
            //             txType = "buy";
            //             //if(tx.ReceivedAmount?.IsFiatCurrency == true)
            //             {
            //                 if (tx.SentAmount?.CurrencyCode == tx.FeeAmount?.CurrencyCode)
            //                 {
            //                     //quoteMoney = tx.SentAmount.Subtract(tx.FeeAmount);
            //                 }
            //                 else if (tx.ReceivedAmount?.CurrencyCode == tx.FeeAmount?.CurrencyCode)
            //                 {
            //                     baseMoney = tx.ReceivedAmount?.Subtract(tx.FeeAmount);
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             txType = "sell";
            //             baseMoney = tx.SentAmount;
            //             quoteMoney = tx.ReceivedAmount;
            //         }


            //         break;
            //     case TransactionType.Deposit:
            //         if (tx.ReceivedAmount?.IsFiatCurrency == true)
            //         {
            //             txType = "fiat-deposit";
            //             baseMoney = tx.ReceivedAmount.Add(tx.FeeAmount);
            //         }
            //         else
            //         {
            //             txType = "receive";
            //         }
            //         break;
            //     case TransactionType.Withdrawal:
            //         baseMoney = tx.SentAmount;
            //         quoteMoney = tx.ReceivedAmount;
            //         if (tx.SentAmount?.IsFiatCurrency == true)
            //         {
            //             txType = "fiat-withdrawal";
            //         }
            //         else
            //         {
            //             txType = "send";
            //         }
            //         break;
            //     default:
            //         break;
            // }

            // return $"{tx.DateTime:yyyy-MM-dd HH:mm:ss},{txType},{baseMoney?.CurrencyCode},{baseMoney?.AbsoluteAmount},{quoteMoney?.CurrencyCode},{quoteMoney?.AbsoluteAmount},{tx.FeeAmount?.CurrencyCode},{tx.FeeAmount?.AbsoluteAmount},,,,,,,";
        }
    }
}
