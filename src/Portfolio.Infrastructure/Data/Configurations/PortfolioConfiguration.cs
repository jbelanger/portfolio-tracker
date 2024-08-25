using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class PortfolioConfiguration : IEntityTypeConfiguration<UserPortfolio>
    {
        public void Configure(EntityTypeBuilder<UserPortfolio> builder)
        {
            // Define the table name (optional, EF Core will use the class name by default)
            builder.ToTable("UserPortfolios");

            // Define the primary key
            builder.HasKey(p => p.Id);

            builder.Property(p => p.DefaultCurrency);

            // Configure the relationship with Wallets
            builder.HasMany(p => p.Wallets)
                   .WithOne()
                   .HasForeignKey(f => f.PortfolioId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with Holdings
            builder.HasMany(p => p.Holdings)
                   .WithOne()
                   .HasForeignKey(h => h.UserPortfolioId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with ProcessedTransactions
            builder.HasMany(p => p.FinancialEvents)
                   .WithOne()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
