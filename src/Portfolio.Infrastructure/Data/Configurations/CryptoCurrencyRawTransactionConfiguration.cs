using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
       public class CryptoCurrencyRawTransactionConfiguration : IEntityTypeConfiguration<CryptoCurrencyRawTransaction>
       {
              public void Configure(EntityTypeBuilder<CryptoCurrencyRawTransaction> builder)
              {
                     // Define the table name (optional, EF Core will use the class name by default)
                     builder.ToTable("CryptoCurrencyRawTransactions");

                     // Define the primary key
                     builder.HasKey(t => t.Id);

                     // Define properties
                     builder.Property(t => t.DateTime)
                            .IsRequired();

                     builder.Property(t => t.Type)
                            .IsRequired();

                     builder.Property(t => t.Account)
                            .IsRequired()
                            .HasMaxLength(100); // Assuming the account name has a max length, adjust as needed

                     builder.Property(t => t.Note)
                            .HasMaxLength(500); // Optional: define max length for the note field

                     builder.Property(t => t.CsvLinesJson);

                     // Define the relationship to the Wallet (assuming a Wallet entity exists)
                     //      builder.HasOne<Wallet>()
                     //             .WithMany()
                     //             .HasForeignKey(t => t.WalletId)
                     //             .OnDelete(DeleteBehavior.Cascade);

                     // Configure the value objects (Money)
                     builder.OwnsOne(t => t.ReceivedAmount, money =>
                     {
                            money.Property(m => m.Amount)
                        .HasColumnName("ReceivedAmount")
                        .HasColumnType("decimal(18,8)");

                            money.Property(m => m.CurrencyCode)
                        .HasColumnName("ReceivedAmountCurrency")
                        .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.SentAmount, money =>
                     {
                            money.Property(m => m.Amount)
                        .HasColumnName("SentAmount")
                        .HasColumnType("decimal(18,8)");

                            money.Property(m => m.CurrencyCode)
                        .HasColumnName("SentAmountCurrency")
                        .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.FeeAmount, money =>
                     {
                            money.Property(m => m.Amount)
                        .HasColumnName("FeeAmount")
                        .HasColumnType("decimal(18,8)");

                            money.Property(m => m.CurrencyCode)
                        .HasColumnName("FeeAmountCurrency")
                        .HasMaxLength(3);
                     });

                     // TransactionIds - assuming it's a collection of strings that needs to be stored as a JSON or CSV string
                     builder.Property(t => t.TransactionIds)
                            .HasConversion(
                                v => string.Join(',', v),
                                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .HasColumnName("TransactionIds");
              }
       }
}
