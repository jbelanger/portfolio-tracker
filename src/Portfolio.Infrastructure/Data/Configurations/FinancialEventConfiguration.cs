using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class FinancialEventConfiguration : IEntityTypeConfiguration<FinancialEvent>
    {
        public void Configure(EntityTypeBuilder<FinancialEvent> builder)
        {
            // Configure the table name if not the same as the class name
            builder.ToTable("FinancialEvents");

            // Configure the primary key
            builder.HasKey(te => te.Id);

            // Configure properties
            builder.Property(te => te.EventDate)
                .IsRequired();

            builder.Property(te => te.CostBasisPerUnit)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.MarketPricePerUnit)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.Amount)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.BaseCurrency)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(te => te.AssetSymbol)
                .HasMaxLength(10)
                .IsRequired();
        }
    }
}
