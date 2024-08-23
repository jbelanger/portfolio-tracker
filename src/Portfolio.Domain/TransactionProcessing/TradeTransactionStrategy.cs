using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class TradeTransactionStrategy : ITransactionStrategy
{
    private readonly ITaxCalculationStrategy _taxCalculationStrategy;
    private readonly ICostBasisCalculationStrategy _costBasisCalculationStrategy;

    public TradeTransactionStrategy(ITaxCalculationStrategy taxCalculationStrategy, ICostBasisCalculationStrategy costBasisCalculationStrategy)
    {
        _taxCalculationStrategy = taxCalculationStrategy;
        _costBasisCalculationStrategy = costBasisCalculationStrategy;
    }

    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!TransactionValidationUtils.EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);
        if (!TransactionValidationUtils.EnsureAboveZeroAmount(tx, false)) return Result.Failure(tx.ErrorMessage);

        var sender = portfolio.GetOrCreateHolding(tx.SentAmount.CurrencyCode);
        var receiver = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);

        decimal tradedCostInUsd;

        // Scenario 1: Received amount is in USD (default currency)
        if (tx.ReceivedAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            tradedCostInUsd = tx.ReceivedAmount.Amount;
            tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);
        }
        // Scenario 2: Sent amount is in USD (default currency)
        else if (tx.SentAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            tradedCostInUsd = tx.SentAmount.Amount;
            tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);
        }
        // Scenario 3: Crypto-to-crypto trade where neither currency is the default (USD)
        else
        {
            var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
            if (priceResult.IsSuccess)
            {
                tradedCostInUsd = tx.SentAmount.Amount * priceResult.Value;
                tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);

                // Calculate and add taxable event using the tax calculation strategy and cost basis
                var taxableEvent = _taxCalculationStrategy.CalculateTax(tx, sender, priceResult.Value, portfolio.DefaultCurrency, _costBasisCalculationStrategy);
                if (taxableEvent != null)
                {
                    portfolio.AddTaxableEvent(taxableEvent);
                }
            }
            else
            {
                tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";

                // Fallback: Use the last known price from purchase records or assume 0 if no records exist
                tradedCostInUsd = sender.PurchaseRecords.LastOrDefault()?.PricePerUnit * tx.SentAmount.Amount ?? 0;
                tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);
            }
        }

        // Update balances and purchase records regardless of price history availability
        UpdateSenderBalance(tx, sender);

        // Add to receiver balance and add new purchase record for the acquired asset
        //receiver.Balance += tx.ReceivedAmount.Amount;
        receiver.AddPurchase(tx.ReceivedAmount.Amount, tx.ValueInDefaultCurrency.Amount / tx.ReceivedAmount.Amount, tx.DateTime);

        await FeeHandlingUtils.HandleFeesAsync(tx, portfolio, priceHistoryService);

        return Result.Success();
    }

private static void UpdateSenderBalance(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding sender)
{
    sender.Balance -= tx.SentAmount.Amount;

    var amountToDeduct = tx.SentAmount.Amount;

    for (int i = 0; i < sender.PurchaseRecords.Count && amountToDeduct > 0; i++)
    {
        var record = sender.PurchaseRecords[i];
        
        if (record.Amount > amountToDeduct)
        {
            // Adjust the PricePerUnit for the remaining units to reflect the proportional cost basis
            var remainingAmount = record.Amount - amountToDeduct;
            var newPricePerUnit = ((record.PricePerUnit * record.Amount) - (record.PricePerUnit * amountToDeduct)) / remainingAmount;

            // Update the record with the new amount and adjusted price per unit
            sender.PurchaseRecords[i] = new PurchaseRecord(remainingAmount, newPricePerUnit, record.PurchaseDate);

            amountToDeduct = 0; // All amount has been deducted
        }
        else
        {
            // Deduct the full amount of the record
            amountToDeduct -= record.Amount;
            // Remove the record since it's fully used up
            sender.PurchaseRecords.RemoveAt(i);
            i--; // Adjust the index after removal
        }
    }

    TransactionValidationUtils.EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
}


}
