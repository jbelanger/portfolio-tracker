using Portfolio.App.DTOs;

namespace Portfolio.App.Services
{
    public interface IWalletService
    {
        Task<Result<long>> CreateWalletAsync(long portfolioId, WalletDto walletDto);
        Task<Result> DeleteWalletAsync(long portfolioId, long walletId);
        Task<Result<WalletDto>> GetWalletAsync(long portfolioId, long walletId);
        Task<Result<IEnumerable<WalletDto>>> GetWalletsAsync(long portfolioId);
    }
}
