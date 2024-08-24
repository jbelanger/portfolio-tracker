using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    public class TaxableEvent : BaseAuditableEntity
    {
        public DateTime DateTime { get; private set; }
        public decimal AverageCost { get; private set; }
        public decimal ValueAtDisposal { get; private set; }
        public decimal Quantity { get; private set; }
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
            decimal quantity,
            string currency)
        {
            return new TaxableEvent()
            {
                DateTime = dateTime,
                DisposedAsset = asset,
                AverageCost = averageCost,
                ValueAtDisposal = valueAtDisposal,
                Quantity = quantity,
                Currency = currency            
            };
        }
    }
}
