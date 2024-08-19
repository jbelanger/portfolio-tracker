using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CryptoCurrencyTransactionConfiguration : IEntityTypeConfiguration<CryptoCurrencyTransaction>
    {
        public void Configure(EntityTypeBuilder<CryptoCurrencyTransaction> builder)
        {
            builder.HasKey(t => t.Id); // Assuming you have an Id property, add it if not present

            builder.Property(t => t.DateTime).IsRequired();
            builder.Property(t => t.Type).IsRequired();
            builder.Property(t => t.Account).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Note).HasMaxLength(500);

            builder.HasDiscriminator<TransactionType>("TransactionType")
                .HasValue<CryptoCurrencyTransaction>(TransactionType.Undefined)
                .HasValue<CryptoCurrencyDepositTransaction>(TransactionType.Deposit)
                .HasValue<CryptoCurrencyWithdrawTransaction>(TransactionType.Withdrawal)
                .HasValue<CryptoCurrencyTradeTransaction>(TransactionType.Trade);

            builder.Ignore(t => t.State);
            builder.Ignore(t => t.UnitValue);
            builder.OwnsOne(t => t.Amount);
            builder.OwnsOne(t => t.FeeAmount);
        }
    }
}
