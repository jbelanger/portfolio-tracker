using CSharpFunctionalExtensions;
using Portfolio.Domain.Common;

namespace Portfolio.Domain;

public class FinancialEvent : BaseAuditableEntity
{
    /// <summary>
    /// Gets the date when the financial transaction event occurred.
    /// </summary>
    public DateTime EventDate { get; private set; }

    /// <summary>
    /// Gets the cost basis per unit of the disposed asset.
    /// This represents the original purchase cost for each unit of the disposed asset.
    /// </summary>
    public decimal CostBasisPerUnit { get; private set; }

    /// <summary>
    /// Gets the market price per unit of the disposed asset at the time of the transaction.
    /// This represents the fair market value per unit at the time of the trade or withdrawal.
    /// </summary>
    public decimal MarketPricePerUnit { get; private set; }

    /// <summary>
    /// Gets the total fair market value of the disposed asset at the time of the transaction.
    /// Calculated as MarketPricePerUnit * Amount.
    /// </summary>
    public decimal FairMarketValue => MarketPricePerUnit * Amount;

    /// <summary>
    /// Gets the capital gain or loss of the disposed asset at the time of the transaction.
    /// Calculated as FairMarketValue - (CostBasisPerUnit * Amount).
    /// </summary>
    public decimal CapitalGain => FairMarketValue - (CostBasisPerUnit * Amount);

    /// <summary>
    /// Gets the amount of the asset involved in the transaction event.
    /// This represents the total quantity of the disposed asset.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Gets or sets the symbol of the disposed asset (e.g., AAPL, BTC).
    /// </summary>
    public string AssetSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base currency used for reporting the value of the transaction event (e.g., USD).
    /// </summary>
    public string BaseCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Private constructor to enforce the use of the Create factory method.
    /// </summary>
    private FinancialEvent() { }

    /// <summary>
    /// Factory method to create a new FinancialEvent instance.
    /// This method calculates the FairMarketValue based on the provided MarketPricePerUnit and Amount.
    /// </summary>
    /// <param name="eventDate">The date of the financial transaction event.</param>
    /// <param name="assetSymbol">The symbol of the disposed asset.</param>
    /// <param name="costBasisPerUnit">The cost basis per unit of the disposed asset.</param>
    /// <param name="marketPricePerUnit">The market price per unit of the disposed asset at the time of the event.</param>
    /// <param name="amount">The amount of the asset involved in the event.</param>
    /// <param name="baseCurrency">The base currency used for reporting (e.g., USD).</param>
    /// <returns>A Result object containing the new FinancialEvent instance.</returns>
    public static Result<FinancialEvent> Create(
        DateTime eventDate,
        string assetSymbol,
        decimal costBasisPerUnit,
        decimal marketPricePerUnit,
        decimal amount,
        string baseCurrency)
    {        
        return new FinancialEvent()
        {
            EventDate = eventDate,
            AssetSymbol = assetSymbol,
            CostBasisPerUnit = costBasisPerUnit,
            MarketPricePerUnit = marketPricePerUnit,            
            Amount = amount,
            BaseCurrency = baseCurrency
        };
    }
}
