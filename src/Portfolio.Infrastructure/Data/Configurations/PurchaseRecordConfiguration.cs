using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations
{
    public class PurchaseRecordConfiguration : IEntityTypeConfiguration<PurchaseRecord>
    {
        public void Configure(EntityTypeBuilder<PurchaseRecord> builder)
        {
            // Configure the table name if not the same as the class name
            builder.ToTable("TaxableEvents");

            // Configure the primary key
            builder.HasKey(te => te.Id);

            // Configure properties
            builder.Property(te => te.PurchaseDate)
                .IsRequired();

            builder.Property(te => te.Amount)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.PricePerUnit)
                .HasColumnType("decimal(18,8)")
                .IsRequired();
        }
    }
}
