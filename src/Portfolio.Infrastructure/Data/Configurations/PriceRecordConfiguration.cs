using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class PriceRecordConfiguration : IEntityTypeConfiguration<PriceRecord>
    {
        public void Configure(EntityTypeBuilder<PriceRecord> builder)
        {            
            builder.ToTable("PriceHistoryRecords");
         
            builder.HasKey(h => h.Id);
            
            builder.Property(h => h.CurrencyPair)
                   .IsRequired()
                   .HasMaxLength(20);
            
            builder.Property(h => h.ClosePrice)
                   .IsRequired()
                   .HasColumnType("decimal(18,8)");

            builder.Property(t => t.CloseDate);            
        }
    }
}
