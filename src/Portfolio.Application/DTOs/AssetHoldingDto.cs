using Portfolio.Domain.Entities;

namespace Portfolio.App.DTOs
{
    public class AssetHoldingDto
    {
        public long Id { get; set; }
        public string Asset { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal AverageBoughtPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        
        public static AssetHoldingDto From(AssetHolding holding)
        {
            return new AssetHoldingDto
            {
                Id = holding.Id,
                Asset = holding.Asset,
                Balance = holding.Balance,
                AverageBoughtPrice = holding.AverageBoughtPrice,
                CurrentPrice = holding.CurrentPrice.Amount,
                ErrorMessage = holding.ErrorMessage
            };
        }
    }
}
