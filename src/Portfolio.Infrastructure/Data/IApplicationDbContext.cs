using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<PriceRecord> PriceHistoryRecords { get; }
    DbSet<CoinInfo> CoinInfos { get; }
    DbSet<HttpRequestLogEntry> HttpRequestLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
