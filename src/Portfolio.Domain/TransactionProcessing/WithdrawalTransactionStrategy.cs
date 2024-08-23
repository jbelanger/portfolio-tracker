using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class WithdrawalTransactionStrategy : ITransactionStrategy
{
    private readonly ITaxCalculationStrategy _taxCalculationStrategy;
    private readonly ICostBasisCalculationStrategy _costBasisCalculationStrategy;

    public WithdrawalTransactionStrategy(ITaxCalculationStrategy taxCalculationStrategy, ICostBasisCalculationStrategy costBasisCalculationStrategy)
    {
        _taxCalculationStrategy = taxCalculationStrategy;
        _costBasisCalculationStrategy = costBasisCalculationStrategy;
    }

    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!TransactionValidationUtils.EnsureAboveZeroAmount(tx, false)) return Result.Failure(tx.ErrorMessage);

        var sender = portfolio.GetOrCreateHolding(tx.SentAmount.CurrencyCode);
        var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);

        if (priceResult.IsSuccess)
        {
            decimal price = priceResult.Value;
            tx.ValueInDefaultCurrency = new Money(tx.SentAmount.Amount * price, portfolio.DefaultCurrency);

            // Calculate and add taxable event using the tax calculation strategy and cost basis
            var taxableEvent = _taxCalculationStrategy.CalculateTax(tx, sender, price, portfolio.DefaultCurrency, _costBasisCalculationStrategy);
            if (taxableEvent != null)
            {
                portfolio.AddTaxableEvent(taxableEvent);
            }
        }
        else
        {
            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
            tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";
        }

        UpdateSenderBalance(tx, sender);
        await FeeHandlingUtils.HandleFeesAsync(tx, portfolio, priceHistoryService);

        return Result.Success();
    }

    private static void UpdateSenderBalance(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding sender)
    {
        decimal amountToDeduct = tx.SentAmount.Amount;

        for (int i = 0; i < sender.PurchaseRecords.Count; i++)
        {
            var record = sender.PurchaseRecords[i];

            if (record.Amount >= amountToDeduct)
            {
                sender.PurchaseRecords[i] = new PurchaseRecord(record.Amount - amountToDeduct, record.PricePerUnit, record.PurchaseDate);
                if (sender.PurchaseRecords[i].Amount == 0)
                {
                    sender.PurchaseRecords.RemoveAt(i);
                }
                amountToDeduct = 0;
                break;
            }
            else
            {
                amountToDeduct -= record.Amount;
                sender.PurchaseRecords.RemoveAt(i);
                i--; // Adjust index after removal
            }
        }

        sender.Balance -= tx.SentAmount.Amount;

        if (sender.Balance == 0)
        {
            sender.PurchaseRecords.Clear();
        }

        TransactionValidationUtils.EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
    }
}
