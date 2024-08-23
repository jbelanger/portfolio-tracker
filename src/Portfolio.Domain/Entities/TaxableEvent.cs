using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    public class TaxableEvent : BaseAuditableEntity
    {
        public DateTime DateTime { get; private set; }
        public decimal AverageCost { get; private set; }
        public decimal ValueAtDisposal { get; private set; }
        public decimal Amount { get; private set; }
        public string DisposedAsset { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;

        private TaxableEvent()
        {            
        }

        public static Result<TaxableEvent> Create(
            DateTime dateTime,  
            string asset,          
            decimal averageCost,
            decimal valueAtDisposal,
            decimal amount,
            string currency)
        {
            return new TaxableEvent()
            {
                DateTime = dateTime,
                DisposedAsset = asset,
                AverageCost = averageCost,
                ValueAtDisposal = valueAtDisposal,
                Amount = amount,
                Currency = currency
            };
        }
    }
}
