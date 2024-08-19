using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Events
{
    public class TransactionAddedDomainEvent : BaseEvent
    {
        public CryptoCurrencyHolding Holding { get; }
        public ICryptoCurrencyTransaction Transaction { get; }

        public TransactionAddedDomainEvent(CryptoCurrencyHolding holding, ICryptoCurrencyTransaction transaction)
        {
            Holding = holding;
            Transaction = transaction;
        }
    }
}
