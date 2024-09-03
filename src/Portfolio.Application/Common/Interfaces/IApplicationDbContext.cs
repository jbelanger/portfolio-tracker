using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserPortfolio> Portfolios { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<AssetHolding> AssetHoldings { get; }
    DbSet<FinancialTransaction> Transactions { get; }
    DbSet<PriceRecord> PriceHistoryRecords { get; }
    DbSet<CoinInfo> CoinInfos { get; }
    DbSet<HttpRequestLogEntry> HttpRequestLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
