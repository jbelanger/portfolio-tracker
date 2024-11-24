using Portfolio.App.DTOs;

namespace Portfolio.App.Services
{
    public interface IHoldingService
    {
        Task<Result<AssetHoldingDto>> GetHoldingAsync(long portfolioId, long holdingId);
        Task<Result<IEnumerable<AssetHoldingDto>>> GetHoldingsAsync(long portfolioId);
    }
}
