using Portfolio.Domain.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class CryptoCurrencyHolding : BaseAuditableEntity
    {
        public string Asset { get; private set; }
        public decimal Balance { get; set; }
        //public decimal Fees { get; set; }

        public decimal AverageBoughtPrice { get; set; }
        public Money CurrentPrice { get; set; }

        public CryptoCurrencyHolding(string asset)
        {
            Asset = asset;
            Balance = 0;
            AverageBoughtPrice = 0;
        }
    }
}
