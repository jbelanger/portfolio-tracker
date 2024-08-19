using CsvHelper.Configuration;
using Portfolio.Shared;

namespace Portfolio.Transactions.Importers.Csv.Coinbase
{
    /// <summary>
    /// Defines the mapping configuration for a CSV line specific to Coinbase transaction data.
    /// </summary>
    public class CoinbaseCsvLineMap : ClassMap<CoinbaseCsvEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoinbaseCsvLineMap"/> class with predefined mappings.
        /// </summary>
        public CoinbaseCsvLineMap()
        {
            Map(m => m.TransactionId).Name("ID");
            Map(m => m.Date).Name("Timestamp").Convert(args => DateTime.Parse(args.Row.GetField("Timestamp").Replace(" UTC", ""), null, System.Globalization.DateTimeStyles.RoundtripKind));
            Map(m => m.Type).Name("Transaction Type");
            Map(m => m.Asset).Name("Asset");
            Map(m => m.Amount).Name("Quantity Transacted").Convert(args => new Money(ToDecimal(args.Row.GetField("Quantity Transacted")), args.Row.GetField("Asset")));
            Map(m => m.PriceCurrency).Name("Price Currency");
            Map(m => m.Subtotal).Name("Subtotal").Convert(args => new Money(ToDecimal(args.Row.GetField("Subtotal")), args.Row.GetField("Price Currency")));
            Map(m => m.Fee).Name("Fees and/or Spread").Convert(args => new Money(ToDecimal(args.Row.GetField("Fees and/or Spread")), args.Row.GetField("Price Currency")));
            Map(m => m.Notes).Name("Notes");
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
    /// Represents a line in a CSV file containing transaction data from Coinbase.
    /// </summary>
    public class CoinbaseCsvEntry
    {
        public string TransactionId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Type { get; set; } = null!;
        public string Asset { get; set; } = null!;
        public Money Amount { get; set; } = null!;
        public string PriceCurrency { get; set; } = null!;
        public Money Subtotal { get; set; } = null!;
        public Money Fee { get; set; } = null!;
        public string Notes { get; set; } = string.Empty;
    }
}
