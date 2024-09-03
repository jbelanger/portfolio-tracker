using Portfolio.App.Common.Interfaces;
using Portfolio.App.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.App.Services
{
    public class WalletService : IWalletService
    {
        private readonly IApplicationDbContext _dbContext;

        public WalletService(IApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<long>> CreateWalletAsync(long portfolioId, WalletDto walletDto)
        {
            UserPortfolio? portfolio = await _dbContext.Portfolios
                .Include(p => p.Wallets)
                .FirstOrDefaultAsync(p => p.Id == portfolioId)
                .ConfigureAwait(false);

            if (portfolio == null)
            {
                return Result.Failure<long>($"Portfolio with ID {portfolioId} not found.");
            }

            return await Wallet.Create(walletDto.Name)
                .Check(w => portfolio.AddWallet(w))
                .Tap(async () => await _dbContext.SaveChangesAsync().ConfigureAwait(false))
                .Map(w => w.Id);
        }

        public async Task<Result> DeleteWalletAsync(long portfolioId, long walletId)
        {
            Wallet? wallet = await _dbContext.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
            {
                return Result.Failure($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");
            }

            _dbContext.Wallets.Remove(wallet);
            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<WalletDto>> GetWalletAsync(long portfolioId, long walletId)
        {
            var wallet = await _dbContext.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
            {
                return Result.Failure<WalletDto>($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");
            }

            var walletDto = WalletDto.From(wallet);
            
            return Result.Success(walletDto);
        }

        public async Task<Result<IEnumerable<WalletDto>>> GetWalletsAsync(long portfolioId)
        {
            var wallets = await _dbContext.Wallets
                .Where(w => w.PortfolioId == portfolioId)
                .Include(w => w.Transactions)
                .ToListAsync();

            var walletDtos = wallets.Select(w => WalletDto.From(w)).ToList();

            return Result.Success<IEnumerable<WalletDto>>(walletDtos);
        }
    }
}
