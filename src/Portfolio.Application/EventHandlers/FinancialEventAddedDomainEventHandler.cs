using Portfolio.Domain.Events;

namespace Portfolio.App.DomainEventHandlers
{
    public class FinancialEventAddedDomainEventHandler : INotificationHandler<FinancialEventAdded>
    {
        public Task Handle(FinancialEventAdded notification, CancellationToken cancellationToken)
        {
            var holding = notification.Holding;
            var transaction = notification.Transaction;

            if(string.IsNullOrWhiteSpace(notification.ErrorMessage))
            {
                Log.Information("Financial event created.");
            }
            else
            {
                Log.Error("Financial event could not be created: {ErrorMessage}", notification.ErrorMessage);
            }

            return Task.CompletedTask;
        }
    }
}