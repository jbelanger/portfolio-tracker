using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data.Configurations
{
    public class HttpRequestLogEntryConfiguration : IEntityTypeConfiguration<HttpRequestLogEntry>
    {
        public void Configure(EntityTypeBuilder<HttpRequestLogEntry> builder)
        {
            // Define the table name (optional, EF Core will use the class name by default)
            builder.ToTable("HttpRequestLogEntries");

            builder.HasKey(h => h.Id);
        
            builder.Property(h => h.RequestDate)
                   .IsRequired();                   

            builder.Property(t => t.RequestUri).IsRequired();            
        }
    }
}
