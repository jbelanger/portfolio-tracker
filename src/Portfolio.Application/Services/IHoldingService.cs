using Portfolio.App.DTOs;
using Portfolio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Portfolio.App.Services
{
    public interface IHoldingService
    {
        Task<Result<AssetHoldingDto>> GetHoldingAsync(long portfolioId, long holdingId);
        Task<Result<IEnumerable<AssetHoldingDto>>> GetHoldingsAsync(long portfolioId);
    }
}
