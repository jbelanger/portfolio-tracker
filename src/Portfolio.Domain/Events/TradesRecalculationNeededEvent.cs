using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Events
{
    public class TradesRecalculationNeededEvent : BaseEvent
    {
        public UserPortfolio Portfolio { get; set; }

        public TradesRecalculationNeededEvent(UserPortfolio portfolio)
        {
            Portfolio = portfolio;
        }
    }
}