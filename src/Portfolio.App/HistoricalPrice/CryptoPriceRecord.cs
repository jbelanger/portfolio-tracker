using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice;

public class CryptoPriceRecord : ValueObject
{
    public string CurrencyPair { get; set; } = string.Empty;
    public DateTime CloseDate { get; set; }
    public decimal ClosePrice { get; set; }

    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return CurrencyPair;
        yield return CloseDate;
        yield return ClosePrice;
    }
}
