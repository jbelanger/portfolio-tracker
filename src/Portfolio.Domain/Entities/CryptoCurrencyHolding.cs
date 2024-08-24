using Portfolio.Domain.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class CryptoCurrencyHolding : BaseAuditableEntity
    {
        public string Asset { get; private set; }
        public decimal Balance { get; set; }
        public decimal AverageBoughtPrice { get; set; }
        public Money CurrentPrice { get; set; } = Money.Empty;
        public ErrorType ErrorType { get; set; } = ErrorType.None;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<PurchaseRecord> PurchaseRecords { get; private set; } = new();

        public CryptoCurrencyHolding(string asset)
        {
            Asset = asset;
            Balance = 0;
            AverageBoughtPrice = 0;
        }

        public void AddPurchase(decimal amount, decimal pricePerUnit, DateTime purchaseDate)
        {
            PurchaseRecords.Add(new PurchaseRecord(amount, pricePerUnit, purchaseDate));            
        }

        public void RemovePurchase(decimal amount)
        {
            //Balance -= amount;
            // Logic for removing or updating specific purchase records
            // For simplicity, we are not implementing specific record adjustments here
        }
    }
}
