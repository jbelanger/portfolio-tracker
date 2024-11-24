using CSharpFunctionalExtensions;

namespace Portfolio.Domain.ValueObjects
{
    /// <summary>
    /// Represents a record of a cryptocurrency's price at a specific date.
    /// </summary>
    public class PriceRecord : ValueObject
    {
public long Id { get; set; }

        /// <summary>
        /// Gets or sets the currency pair (e.g., "BTC/USD") associated with this price record.
        /// </summary>
        public string CurrencyPair { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time at which the closing price was recorded.
        /// </summary>
        public DateTime CloseDate { get; set; }

        /// <summary>
        /// Gets or sets the closing price of the cryptocurrency for the given date.
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// Provides the components used for equality comparison between instances of <see cref="PriceRecord"/>.
        /// </summary>
        /// <returns>A collection of components used for equality comparison.</returns>
        protected override IEnumerable<IComparable> GetEqualityComponents()
        {
            yield return CurrencyPair;
            yield return CloseDate;
            yield return ClosePrice;
        }
    }
}
