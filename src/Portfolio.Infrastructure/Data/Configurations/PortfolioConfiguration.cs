using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio.Domain.Entities.UserPortfolio>
    {
        public void Configure(EntityTypeBuilder<Portfolio.Domain.Entities.UserPortfolio> builder)
        {
            // Define the table name (optional, EF Core will use the class name by default)
            builder.ToTable("UserPortfolios");

            // Define the primary key
            builder.HasKey(p => p.Id);

            // Configure the relationship with Wallets
            builder.HasMany(p => p.Wallets)
                   .WithOne()
                   .HasForeignKey(f => f.PortfolioId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with Holdings
            builder.HasMany(p => p.Holdings)
                   .WithOne()
                   .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with ProcessedTransactions
            builder.HasMany(p => p.ProcessedTransactions)
                   .WithOne()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
