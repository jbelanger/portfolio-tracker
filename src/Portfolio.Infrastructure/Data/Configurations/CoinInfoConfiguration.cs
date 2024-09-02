using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class CoinInfoConfiguration : IEntityTypeConfiguration<CoinInfo>
    {
        public void Configure(EntityTypeBuilder<CoinInfo> builder)
        {            
            builder.ToTable("CoinInfos");
         
            builder.HasKey(h => h.CoinId);

            builder.HasIndex(h => new { h.Symbol });
        }
    }
}
