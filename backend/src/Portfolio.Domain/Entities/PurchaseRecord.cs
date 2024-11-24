using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents an individual purchase record of an asset, including the amount purchased,
    /// the price per unit, and the date of purchase. This class is used to support LIFO (Last In, First Out)
    /// and FIFO (First In, First Out) tax strategies by tracking the details of each purchase.
    /// The information stored in this class is essential for calculating capital gains or losses 
    /// when the asset is disposed of.
    /// </summary>
    public class PurchaseRecord : BaseAuditableEntity
    {
        /// <summary>
        /// Gets the amount of the asset purchased in this record.
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Gets the price per unit of the asset at the time of purchase.
        /// </summary>
        public decimal PricePerUnit { get; private set; }

        /// <summary>
        /// Gets the date and time when the asset was purchased.
        /// </summary>
        public DateTime PurchaseDate { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PurchaseRecord"/> class.
        /// </summary>
        /// <param name="amount">The amount of the asset purchased.</param>
        /// <param name="pricePerUnit">The price per unit of the asset at the time of purchase.</param>
        /// <param name="purchaseDate">The date and time when the asset was purchased.</param>
        public PurchaseRecord(decimal amount, decimal pricePerUnit, DateTime purchaseDate)
        {
            Amount = amount;
            PricePerUnit = pricePerUnit;
            PurchaseDate = purchaseDate;
        }
    }
}
