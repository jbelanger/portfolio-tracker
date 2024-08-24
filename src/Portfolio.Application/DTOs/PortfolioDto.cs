using Portfolio.Domain.Entities;

namespace Portfolio.App.DTOs
{
    public class PortfolioDto
    {
        public long Id { get; set; }
        public string DefaultCurrency { get; set; } = "USD";
        public List<WalletDto> Wallets { get; set; } = new();
        public List<AssetHoldingDto> Holdings { get; set; } = new();

        // Factory method to create a PortfolioDto from a UserPortfolio domain model
        public static PortfolioDto From(UserPortfolio portfolio)
        {
            return new PortfolioDto
            {
                Id = portfolio.Id,
                DefaultCurrency = portfolio.DefaultCurrency,
                Wallets = portfolio.Wallets.Select(WalletDto.From).ToList(),
                Holdings = portfolio.Holdings.Select(AssetHoldingDto.From).ToList()
            };
        }
    }
}
