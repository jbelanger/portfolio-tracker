using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.App.Common.Interfaces;
using Portfolio.Domain;
using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data.Configurations;
using Portfolio.Infrastructure.Identity;

namespace Portfolio.Infrastructure
{
    public class PortfolioDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        private readonly IMediator _mediator;

        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options, IMediator mediator)
            : base(options)
        {
            _mediator = mediator;
        }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<CryptoCurrencyHolding> Holdings { get; set; }
        public DbSet<CryptoCurrencyTransaction> Transactions { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Dispatch domain events before saving changes
            var entitiesWithEvents = ChangeTracker.Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                var domainEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();

                foreach (var domainEvent in domainEvents)
                {
                    await _mediator.Publish(domainEvent, cancellationToken);
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CryptoCurrencyTransactionConfiguration());            
            modelBuilder.ApplyConfiguration(new CryptoCurrencyTradeTransactionConfiguration());
            modelBuilder.ApplyConfiguration(new CryptoCurrencyHoldingConfiguration());
            modelBuilder.ApplyConfiguration(new WalletConfiguration());
        }
    }
}
