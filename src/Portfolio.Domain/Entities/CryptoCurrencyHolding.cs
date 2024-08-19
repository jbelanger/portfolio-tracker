using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    public class CryptoCurrencyHolding : BaseAuditableEntity
    {
        public string Asset { get; private set; }
        public decimal Balance { get; set; }
        public decimal? AverageBoughtPrice { get; set; }

        public CryptoCurrencyHolding(string asset)
        {
            Asset = asset;
            Balance = 0;
            AverageBoughtPrice = null;
        }
    }
}
