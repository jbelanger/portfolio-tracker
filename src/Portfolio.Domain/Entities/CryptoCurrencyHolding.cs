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

        public CryptoCurrencyHolding(string asset)
        {
            Asset = asset;
            Balance = 0;
            AverageBoughtPrice = 0;
        }
    }
}
