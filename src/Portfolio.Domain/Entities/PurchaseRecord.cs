using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Used for LIFO and FIFO tax strategies. 
    /// This is used through the TaxableEvent class to calculate gains acoording to tax strategy.
    /// </summary>
    public class PurchaseRecord : BaseAuditableEntity
    {
        public decimal Amount { get; private set; }
        public decimal PricePerUnit { get; private set; }
        public DateTime PurchaseDate { get; private set; }

        public PurchaseRecord(decimal amount, decimal pricePerUnit, DateTime purchaseDate)
        {
            Amount = amount;
            PricePerUnit = pricePerUnit;
            PurchaseDate = purchaseDate;
        }
    }
}
