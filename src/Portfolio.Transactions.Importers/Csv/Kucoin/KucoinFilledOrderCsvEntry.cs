using CsvHelper.Configuration;
using Portfolio.Shared;

namespace Portfolio.Transactions.Importers.Csv.Kucoin
{
    /// <summary>
    /// Defines the mapping configuration for a CSV line specific to Kucoin transaction data.
    /// </summary>
    public class KucoinFilledOrderCsvLineMap : ClassMap<KucoinFilledOrderCsvEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KucoinFilledOrderCsvLineMap"/> class with predefined mappings.
        /// </summary>
        public KucoinFilledOrderCsvLineMap()
        {
            Map(m => m.TransactionId).Name("Order ID");
            Map(m => m.Date).Name("Filled Time(UTC)").Convert(args => DateTime.Parse(args.Row.GetField("Filled Time(UTC)"), null, System.Globalization.DateTimeStyles.RoundtripKind));
            Map(m => m.Type).Name("Side");
            Map(m => m.OrderAmount).Name("Filled Amount").Convert(args => new Money(ToDecimal(args.Row.GetField("Filled Amount")), GetTradingPairPart(args.Row.GetField("Symbol"), 0)));
            Map(m => m.FilledVolume).Name("Filled Volume").Convert(args => new Money(ToDecimal(args.Row.GetField("Filled Volume")), GetTradingPairPart(args.Row.GetField("Symbol"), 1)));
            Map(m => m.Fee).Name("Fee").Convert(args => new Money(ToDecimal(args.Row.GetField("Fee")), args.Row.GetField("Fee Currency")));
        }

        private string GetTradingPairPart(string tradingPair, int partNumber)
        {
            string[] parts = tradingPair.Split("-");
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid trading pair {tradingPair}");
            }
            if (partNumber > 1)
            {
                throw new InvalidOperationException($"Invalid part number {partNumber}");
            }
            return parts[partNumber];
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
    public class KucoinFilledOrderCsvEntry
    {
        public string TransactionId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Type { get; set; } = null!;
        public Money OrderAmount { get; set; } = null!;
        public Money FilledVolume { get; set; } = null!;
        public Money Fee { get; set; } = null!;
    }
}
