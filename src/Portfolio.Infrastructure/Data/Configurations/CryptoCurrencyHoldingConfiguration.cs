using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CryptoCurrencyHoldingConfiguration : IEntityTypeConfiguration<CryptoCurrencyHolding>
    {
        public void Configure(EntityTypeBuilder<CryptoCurrencyHolding> builder)
        {
            builder.HasKey(h => h.Asset);
            builder.Property(h => h.Asset)
                   .IsRequired()
                   .HasMaxLength(50);
            builder.Property(h => h.AverageBoughtPrice).IsRequired();
            builder.Property(h => h.Balance).IsRequired();

            builder.HasMany(h => h.Transactions)
                   .WithOne()
                   .HasForeignKey("HoldingAsset")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
