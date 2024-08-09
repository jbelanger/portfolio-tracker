using CsvHelper.Configuration;

namespace Portfolio.LedgerLive;

/// <summary>
/// Defines the mapping configuration for a CSV line specific to LedgerLive transaction data.
/// </summary>
public class LedgerLiveCsvLineMap : ClassMap<LedgerLiveCsvEntry>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerLiveCsvLineMap"/> class with predefined mappings.
    /// </summary>
    public LedgerLiveCsvLineMap()
    {
        Map(m => m.TransactionId).Name("Operation Hash");
        Map(m => m.Date).Name("Operation Date").Convert(args => DateTime.Parse(args.Row.GetField("Operation Date"), null, System.Globalization.DateTimeStyles.RoundtripKind));
        Map(m => m.Type).Name("Operation Type");
        Map(m => m.Asset).Name("Currency Ticker");
        Map(m => m.WalletName).Name("Account Name");
        Map(m => m.Amount).Name("Operation Amount").Convert(args => new Money(ToDecimal(args.Row.GetField("Operation Amount")), args.Row.GetField("Currency Ticker")));
        Map(m => m.Fee).Name("Operation Fees").Convert(args => new Money(ToDecimal(args.Row.GetField("Operation Fees")), args.Row.GetField("Currency Ticker")));
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
/// Represents a line in a CSV file containing transaction data from LedgerLive.
/// </summary>
public class LedgerLiveCsvEntry
{
    public string TransactionId { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Type { get; set; } = null!;
    public string Asset { get; set; } = null!;
    public string WalletName { get; set; } = null!;
    public Money Amount { get; set; } = null!;
    public Money Fee { get; set; } = null!;
}
