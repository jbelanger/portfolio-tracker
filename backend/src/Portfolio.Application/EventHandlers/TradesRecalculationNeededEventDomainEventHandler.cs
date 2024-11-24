using Portfolio.Domain.Events;
using Portfolio.Domain.Interfaces;

namespace Portfolio.App.DomainEventHandlers
{
    public class TradesRecalculationNeededEventDomainEventHandler : INotificationHandler<TradesRecalculationNeededEvent>
    {
        private readonly IPriceHistoryService _priceHistoryService;

        public TradesRecalculationNeededEventDomainEventHandler(IPriceHistoryService priceHistoryService)
        {
            _priceHistoryService = priceHistoryService;
        }

        public async Task Handle(TradesRecalculationNeededEvent notification, CancellationToken cancellationToken)
        {
            Log.Information("Trade calculations started for portfolio {Id}.", notification.Portfolio.Id);
            await notification.Portfolio.CalculateTradesAsync(_priceHistoryService);            
            Log.Information("Trade calculations completed for portfolio {Id}.", notification.Portfolio.Id);
        }
    }
}