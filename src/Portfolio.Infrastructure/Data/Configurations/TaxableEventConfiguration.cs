using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations
{
    public class TaxableEventConfiguration : IEntityTypeConfiguration<TaxableEvent>
    {
        public void Configure(EntityTypeBuilder<TaxableEvent> builder)
        {
            // Configure the table name if not the same as the class name
            builder.ToTable("TaxableEvents");

            // Configure the primary key
            builder.HasKey(te => te.Id);

            // Configure properties
            builder.Property(te => te.DateTime)
                .IsRequired();

            builder.Property(te => te.AverageCost)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.ValueAtDisposal)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.Quantity)
                .HasColumnType("decimal(18,8)")
                .IsRequired();

            builder.Property(te => te.Currency)
                .HasMaxLength(5)
                .IsRequired();
        }
    }
}
