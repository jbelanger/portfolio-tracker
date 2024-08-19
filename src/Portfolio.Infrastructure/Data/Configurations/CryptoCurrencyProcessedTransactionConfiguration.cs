using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CryptoCurrencyProcessedTransactionConfiguration : IEntityTypeConfiguration<CryptoCurrencyProcessedTransaction>
    {
        public void Configure(EntityTypeBuilder<CryptoCurrencyProcessedTransaction> builder)
        {
            // Define the table name (optional, EF Core will use the class name by default)
            builder.ToTable("CryptoCurrencyProcessedTransactions");

            // Define the primary key
            builder.HasKey(pt => pt.Id);

            // Configure the WalletName property
            builder.Property(pt => pt.WalletName)
                   .IsRequired()
                   .HasMaxLength(100); // Assuming the wallet name has a max length, adjust as needed

            // Configure the Asset property
            builder.Property(pt => pt.Asset)
                   .IsRequired()
                   .HasMaxLength(10); // Assuming the asset code is a short string, adjust the length as needed

            // Configure the Amount property
            builder.Property(pt => pt.Amount)
                   .IsRequired()
                   .HasColumnType("decimal(18,8)"); // Adjust precision and scale based on your requirements

            // Configure the DateTime property
            builder.Property(pt => pt.DateTime)
                   .IsRequired();

            // Configure the AveragePriceAtTime property
            builder.Property(pt => pt.AveragePriceAtTime)
                   .HasColumnType("decimal(18,8)"); // Nullable, with precision and scale for high precision calculations

            // Configure the BalanceAfterTransaction property
            builder.Property(pt => pt.BalanceAfterTransaction)
                   .HasColumnType("decimal(18,8)"); // Nullable, with precision and scale for high precision calculations

            // Additional configurations (indexes, constraints, etc.) can be added here if needed
        }
    }
}
