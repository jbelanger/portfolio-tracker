using Portfolio.App.Common.Interfaces;
using Portfolio.App.DTOs;
using Portfolio.Domain.Interfaces;

namespace Portfolio.App.Services
{
    public class HoldingService : IHoldingService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IPriceHistoryService _priceHistoryService;

        public HoldingService(IApplicationDbContext dbContext, IPriceHistoryService priceHistoryService)
        {
            _dbContext = dbContext;
            _priceHistoryService = priceHistoryService;
        }

        public async Task<Result<AssetHoldingDto>> GetHoldingAsync(long portfolioId, long holdingId)
        {
            var holding = await _dbContext.AssetHoldings
                .FirstOrDefaultAsync(h => h.Id == holdingId && h.UserPortfolioId == portfolioId);

            if (holding == null)
                return Result.Failure<AssetHoldingDto>($"Holding with ID {holdingId} not found in Portfolio {portfolioId}.");

            return await Result.Success(AssetHoldingDto.From(holding)).Tap(SetCurrentPriceAsync);
        }

        public async Task<Result<IEnumerable<AssetHoldingDto>>> GetHoldingsAsync(long portfolioId)
        {
            var holdings = await _dbContext.AssetHoldings
                .Where(h => h.UserPortfolioId == portfolioId)
                .ToListAsync();

            var holdingDtos = holdings.Select(AssetHoldingDto.From).ToList();

            return await Result.Success<IEnumerable<AssetHoldingDto>>(holdingDtos).Tap(SetCurrentPricesAsync);
        }

        private async Task SetCurrentPricesAsync(IEnumerable<AssetHoldingDto> holdings)
        {
            var currentPriceResult = await _priceHistoryService.GetCurrentPricesAsync(holdings.Select(h => h.Asset));
            if (currentPriceResult.IsSuccess)
            {
                var dict = currentPriceResult.Value;
                foreach (var h in holdings)
                {
                    if (dict.ContainsKey(h.Asset))
                    {
                        h.CurrentPrice = dict[h.Asset];
                    }
                    else
                    {
                        h.CurrentPrice = 0;
                        Log.Warning("Failed to fetch current price for {Symbol} on {Date}. Error: {Error}",
                                    h.Asset, DateTime.Today.ToString("yyyy-MM-dd"));
                    }
                }
            }
        }

        private async Task SetCurrentPriceAsync(AssetHoldingDto holding)
        {
            await SetCurrentPricesAsync([holding]);
        }
    }
}
