using Portfolio.Domain.Common;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a holding of a financial asset, including balance, purchase history, and pricing information.
    /// This class is applicable to various asset types, including cryptocurrencies, stocks, bonds, and fiat currencies.
    /// </summary>
    public class AssetHolding : BaseAuditableEntity
    {
        /// <summary>
        /// Gets the symbol or identifier of the asset (e.g., BTC, AAPL).
        /// </summary>
        public string Asset { get; private set; }

        /// <summary>
        /// Gets or sets the current balance of the asset.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Gets or sets the average purchase price per unit of the asset.
        /// This is useful for calculating the cost basis for tax purposes.
        /// </summary>
        public decimal AverageBoughtPrice { get; set; }

        /// <summary>
        /// Gets or sets the current market price of the asset.
        /// </summary>
        public Money CurrentPrice { get; set; } = Money.Empty;

        /// <summary>
        /// Gets or sets the type of error associated with the holding, if any.
        /// </summary>
        public ErrorType ErrorType { get; set; } = ErrorType.None;

        /// <summary>
        /// Gets or sets the error message associated with the holding, if any.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of purchase records, each representing an individual purchase of the asset.
        /// </summary>
        public List<PurchaseRecord> PurchaseRecords { get; private set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetHolding"/> class for a specified asset.
        /// </summary>
        /// <param name="asset">The symbol or identifier of the asset.</param>
        public AssetHolding(string asset)
        {
            Asset = asset;
            Balance = 0;
            AverageBoughtPrice = 0;
        }

        /// <summary>
        /// Adds a new purchase record to the holding.
        /// </summary>
        /// <param name="amount">The amount of the asset purchased.</param>
        /// <param name="pricePerUnit">The price per unit of the asset at the time of purchase.</param>
        /// <param name="purchaseDate">The date and time of the purchase.</param>
        public void AddPurchase(decimal amount, decimal pricePerUnit, DateTime purchaseDate)
        {
            PurchaseRecords.Add(new PurchaseRecord(amount, pricePerUnit, purchaseDate));            
        }
    }
}
