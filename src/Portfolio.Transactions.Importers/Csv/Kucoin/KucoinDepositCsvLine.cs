using CsvHelper.Configuration;
using Portfolio.Shared;

namespace Portfolio.Transactions.Importers.Csv.Kucoin
{
    /// <summary>
    /// Defines the mapping configuration for a CSV line specific to Kucoin transaction data.
    /// </summary>
    public class KucoinDepositCsvLineMap : ClassMap<KucoinDepositCsvEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KucoinDepositCsvLineMap"/> class with predefined mappings.
        /// </summary>
        public KucoinDepositCsvLineMap()
        {
            Map(m => m.TransactionId).Name("UID");
            Map(m => m.Date).Name("Time(UTC)").Convert(args => DateTime.Parse(args.Row.GetField("Time(UTC)"), null, System.Globalization.DateTimeStyles.RoundtripKind));
            Map(m => m.Asset).Name("Coin");
            Map(m => m.Amount).Name("Amount").Convert(args => new Money(ToDecimal(args.Row.GetField("Amount")), args.Row.GetField("Coin")));
            Map(m => m.Fee).Name("Fee").Convert(args => new Money(ToDecimal(args.Row.GetField("Fee")), args.Row.GetField("Coin")));
            Map(m => m.Remark).Name("Remarks");
            Map(m => m.Status).Name("Status");
        }

        /// <summary>
        /// Converts a string representation of an amount to a decimal.
        /// </summary>
        /// <param name="originalAmount">The original string amount.</param>
        /// <returns>The decimal representation of the amount.</returns>
        /// <exception cref="ArgumentException">Thrown when the amount cannot be recognized or parsed.</exception>
        private decimal ToDecimal(string originalAmount)
        {
            // Validate the input and parse it to decimal.
            if (string.IsNullOrWhiteSpace(originalAmount))
            {
                return 0;
            }
            if (decimal.TryParse(originalAmount, out decimal amount))
            {
                return amount;
            }
            throw new ArgumentException("Unrecognized amount.");
        }
    }

    /// <summary>
    /// Represents a line in a CSV file containing transaction data from Kucoin.
    /// </summary>
    public class KucoinDepositCsvEntry
    {
        public string TransactionId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Asset { get; set; } = null!;
        public Money Amount { get; set; } = null!;
        public Money Fee { get; set; } = null!;
        public string Remark { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
