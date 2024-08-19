using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CryptoCurrencyHoldingConfiguration : IEntityTypeConfiguration<CryptoCurrencyHolding>
    {
        public void Configure(EntityTypeBuilder<CryptoCurrencyHolding> builder)
        {
            // Define the table name (optional, EF Core will use the class name by default)
            builder.ToTable("CryptoCurrencyHoldings");

            // Define the primary key
            builder.HasKey(h => h.Id);

            // Configure the Asset property
            builder.Property(h => h.Asset)
                   .IsRequired()
                   .HasMaxLength(10); // Assuming the asset code is a short string, adjust the length as needed

            // Configure the Balance property
            builder.Property(h => h.Balance)
                   .IsRequired()
                   .HasColumnType("decimal(18,8)"); // Adjust precision and scale based on your requirements

            // Configure the AverageBoughtPrice property
            builder.Property(h => h.AverageBoughtPrice)
                   .HasColumnType("decimal(18,8)"); // Nullable, with precision and scale for high precision calculations

            // Additional configurations (indexes, constraints, etc.) can be added here if needed
        }
    }
}
