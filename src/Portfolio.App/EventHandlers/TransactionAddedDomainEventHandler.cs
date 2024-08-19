using Portfolio.Domain.Entities;
using Portfolio.Domain.Events;


namespace Portfolio.App.DomainEventHandlers
{
    public class TransactionAddedDomainEventHandler : INotificationHandler<TransactionAddedDomainEvent>
    {
        public Task Handle(TransactionAddedDomainEvent notification, CancellationToken cancellationToken)
        {
            var holding = notification.Holding;
            var transaction = notification.Transaction;

            if (transaction is CryptoCurrencyDepositTransaction deposit)
            {
                holding.UpdateAverageBoughtPrice(deposit);
            }
            else if (transaction is CryptoCurrencyTradeTransaction trade)
            {
                holding.UpdateAverageBoughtPrice(trade);
            }

            return Task.CompletedTask;
        }
    }
}
