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

                     builder.Property(t => t.ErrorType);
                     builder.Property(t => t.ErrorMessage).HasMaxLength(250);

                     // Configure the value objects (Money)
                     builder.OwnsOne(t => t.ReceivedAmount, money =>
                     {
                            money.Property(m => m.Amount)
                                   .HasColumnName("ReceivedAmount")
                                   .HasColumnType("decimal(18,8)");
                            //.HasConversion(new EmptyMoneyAmountConverter());

                            money.Property(m => m.CurrencyCode)
                                   .HasColumnName("ReceivedCurrency")
                                   .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.SentAmount, money =>
                     {
                            money.Property(m => m.Amount)
                                   .HasColumnName("SentAmount")
                                   .HasColumnType("decimal(18,8)");
                            //.HasConversion(new EmptyMoneyAmountConverter());

                            money.Property(m => m.CurrencyCode)
                                   .HasColumnName("SentCurrency")
                                   .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.FeeAmount, money =>
                     {
                            var x = money.Property(m => m.Amount)
                                   .HasColumnName("FeeAmount")
                                   .HasColumnType("decimal(18,8)");
                            //.HasField("_amount");
                            //.UsePropertyAccessMode(PropertyAccessMode.Field);
                            //.IsRequired(false)
                            //.HasConversion(new EmptyMoneyAmountConverter());

                            // x.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
                            // x.Metadata.SetField("_amount");                                                               
                            //x.Metadata.SetValueGeneratorFactory()

                            //money.Metadata?.PrincipalToDependent?.SetField("_settings");

                            money.Property(m => m.CurrencyCode)
                                   .HasColumnName("FeeCurrency")
                                   .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.ValueInDefaultCurrency, money =>
                     {
                            money.Property(m => m.Amount)
                                   //.HasColumnName("ValueInDefaultCurrency")
                                   .HasColumnType("decimal(18,8)");
                            //.HasConversion(new EmptyMoneyAmountConverter());

                            money.Property(m => m.CurrencyCode)
                                   //.HasColumnName("ValueInDefaultCurrency")
                                   .HasMaxLength(3);
                     });

                     builder.OwnsOne(t => t.FeeValueInDefaultCurrency, money =>
                     {
                            money.Property(m => m.Amount)
                                   //.HasColumnName("SentAmount")
                                   .HasColumnType("decimal(18,8)");
                            //.HasConversion(new EmptyMoneyAmountConverter());

                            money.Property(m => m.CurrencyCode)
                                   //.HasColumnName("SentCurrency")
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
