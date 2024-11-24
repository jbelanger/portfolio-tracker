using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Name)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasMany<FinancialTransaction>("Transactions")
                   .WithOne()                   
                   .HasForeignKey(t => t.WalletId)
                   .OnDelete(DeleteBehavior.Cascade);

            //builder.Metadata.FindNavigation("Transactions")?.SetPropertyAccessMode(PropertyAccessMode.Field);
                //.UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
