using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.ValueObjects;
using Portfolio.App.Common.Interfaces;
using Portfolio.Infrastructure.Data.Configurations;

public class ApplicationDbContextInMemory : DbContext, IApplicationDbContext
{
    public ApplicationDbContextInMemory(DbContextOptions<ApplicationDbContextInMemory> options)
        : base(options)
    {
    }

    public DbSet<PriceRecord> PriceHistoryRecords { get; set; }
    public DbSet<CoinInfo> CoinInfos { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new FinancialTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AssetHoldingConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new PortfolioConfiguration());
        modelBuilder.ApplyConfiguration(new FinancialEventConfiguration());
        modelBuilder.ApplyConfiguration(new PriceRecordConfiguration());
        modelBuilder.ApplyConfiguration(new CoinInfoConfiguration());

    }
}

// public class ApplicationDbContextFixture : IDisposable
// {
//     public IApplicationDbContext Context { get; private set; }

//     public ApplicationDbContextFixture()
//     {
//         var options = new DbContextOptionsBuilder<ApplicationDbContextInMemory>()
//             .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use a unique name for each test
//             .Options;

//         Context = new ApplicationDbContextInMemory(options);

//         // Seed initial data if needed
//         SeedDatabase();
//     }

//     private void SeedDatabase()
//     {
//         // Example of seeding initial data
//         Context.PriceHistoryRecords.Add(new PriceRecord { /* Initialize properties */ });
//         Context.CoinInfos.Add(new CoinInfo { /* Initialize properties */ });
//         Context.SaveChangesAsync().GetAwaiter().GetResult();
//     }

//     public void Dispose()
//     {
//         // Clean up the in-memory database
//         (Context as DbContext)?.Dispose();
//     }
// }
