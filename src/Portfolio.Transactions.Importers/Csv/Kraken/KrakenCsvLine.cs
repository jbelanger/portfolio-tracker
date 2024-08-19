using CsvHelper.Configuration;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Transactions.Importers.Csv.Kraken
{
    /// <summary>
    /// Defines the mapping configuration for a CSV line specific to Kraken transaction data.
    /// </summary>
    public class KrakenCsvLineMap : ClassMap<KrakenCsvEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KrakenCsvLineMap"/> class with predefined mappings.
        /// </summary>
        public KrakenCsvLineMap()
        {
            Map(m => m.TransactionId).Name("txid");
            Map(m => m.ReferenceId).Name("refid");
            Map(m => m.Date).Name("time").Convert(args => DateTime.Parse(args.Row.GetField("time"), null, System.Globalization.DateTimeStyles.RoundtripKind));
            Map(m => m.Type).Name("type");
            Map(m => m.SubType).Name("subtype");
            Map(m => m.AClass).Name("aclass");
            Map(m => m.Asset).Name("asset").Convert(args => NormalizeCurrencyAbbreviation(args.Row.GetField("asset")));
            Map(m => m.WalletName).Name("wallet");
            Map(m => m.Amount).Name("amount").Convert(args => new Money(ToDecimal(args.Row.GetField("amount")), NormalizeCurrencyAbbreviation(args.Row.GetField("asset"))));
            Map(m => m.Fee).Name("fee").Convert(args => new Money(ToDecimal(args.Row.GetField("fee")), NormalizeCurrencyAbbreviation(args.Row.GetField("asset"))));
            Map(m => m.Balance).Name("balance").Convert(args => new Money(ToDecimal(args.Row.GetField("balance")), NormalizeCurrencyAbbreviation(args.Row.GetField("asset"))));
        }

        /// <summary>
        /// Normalizes currency abbreviations according to predefined Kraken-specific names.
        /// </summary>
        /// <param name="currency">The original currency abbreviation.</param>
        /// <returns>The normalized currency abbreviation.</returns>
        private string NormalizeCurrencyAbbreviation(string currency)
        {
            // If the currency is in the Kraken-specific dictionary, return the normalized name.
            if (KrakenSpecificAssetNamesDict.TryGetValue(currency, out string? normalized))
            {
                return normalized ?? string.Empty;
            }
            // If the currency contains a period, split it and return the part before the period.
            int dotIndex = currency.IndexOf('.');
            return dotIndex > -1 ? currency.Substring(0, dotIndex) : currency;
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

        /// <summary>
        /// Provides a dictionary mapping Kraken-specific asset names to standard currency abbreviations.
        /// </summary>
        public static Dictionary<string, string> KrakenSpecificAssetNamesDict { get; } = new Dictionary<string, string>
        {
            {"KFEE", "FEE"},
            {"XETC", "ETC"},
            {"XETH", "ETH"},
            {"XLTC", "LTC"},
            {"XMLN", "MLN"},
            {"XREP", "REP"},
            {"XBT", "BTC"},
            {"XXBT", "BTC"},
            {"XXDG", "XDG"},
            {"XXLM", "XLM"},
            {"XXMR", "XMR"},
            {"XXRP", "XRP"},
            {"XZEC", "ZEC"},
            {"ZAUD", "AUD"},
            {"ZCAD", "CAD"},
            {"ZEUR", "EUR"},
            {"ZGBP", "GBP"},
            {"ZJPY", "JPY"},
            {"ZUSD", "USD"},
        };
    }

    /// <summary>
    /// Represents a line in a CSV file containing transaction data from Kraken.
    /// </summary>
    public class KrakenCsvEntry
    {
        public string TransactionId { get; set; } = null!;
        public string ReferenceId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }
        public string AClass { get; set; } = null!;
        public string Asset { get; set; } = null!;
        public string WalletName { get; set; } = null!;
        public Money Amount { get; set; } = null!;
        public Money Fee { get; set; } = null!;
        public Money Balance { get; set; } = null!;
    }
}
