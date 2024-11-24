using Portfolio.Domain.Common;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Events
{
    public class FinancialEventAdded : BaseEvent
    {
        public FinancialTransaction Transaction { get; }
        public AssetHolding Holding { get; }
        public decimal MarketPricePerUnit { get; }
        public string ErrorMessage { get; }

        public FinancialEventAdded(
            FinancialTransaction transaction,
            AssetHolding holding,
            decimal marketPricePerUnit,
            string errorMessage = "")
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            Holding = holding ?? throw new ArgumentNullException(nameof(holding));
            MarketPricePerUnit = marketPricePerUnit;
            ErrorMessage = errorMessage ?? string.Empty;
        }

    }
}
