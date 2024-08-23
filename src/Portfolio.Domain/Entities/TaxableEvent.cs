using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;

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
        public decimal Gain { get; private set; } // Monetary gain or loss

        private TaxableEvent()
        {
        }

        public static Result<TaxableEvent> Create(
            DateTime dateTime,
            string asset,
            decimal averageCost,
            decimal valueAtDisposal,
            decimal amount,
            decimal gain,
            string currency
            )
        {
            if (amount <= 0)
                return Result.Failure<TaxableEvent>("Amount must be greater than zero.");


            return new TaxableEvent()
            {
                DateTime = dateTime,
                DisposedAsset = asset,
                AverageCost = averageCost,
                ValueAtDisposal = valueAtDisposal,
                Amount = amount,
                Currency = currency,
                Gain = gain
            };
        }
    }
}
