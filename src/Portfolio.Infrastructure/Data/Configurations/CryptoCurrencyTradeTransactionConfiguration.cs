using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CryptoCurrencyTradeTransactionConfiguration : IEntityTypeConfiguration<CryptoCurrencyTradeTransaction>
    {
        public void Configure(EntityTypeBuilder<CryptoCurrencyTradeTransaction> builder)
        {
            builder.OwnsOne(t => t.TradeAmount);
        }
    }
}
