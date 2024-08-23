using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class DepositTransactionStrategy : ITransactionStrategy
{
    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!TransactionValidationUtils.EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);

        var receiver = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
        //receiver.Balance += tx.ReceivedAmount.Amount;

        if (tx.ReceivedAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            receiver.AddPurchase(tx.ReceivedAmount.Amount, 1, tx.DateTime);
            tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount, portfolio.DefaultCurrency);
        }
        else
        {
            var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.ReceivedAmount.CurrencyCode, tx.DateTime);
            if (priceResult.IsSuccess)
            {
                decimal price = priceResult.Value;
                tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount * price, portfolio.DefaultCurrency);
                receiver.AddPurchase(tx.ReceivedAmount.Amount, price, tx.DateTime);
            }
            else
            {
                tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                tx.ErrorMessage = $"Could not get price history for {receiver.Asset}. Average price will be incorrect.";
            }
        }

        await FeeHandlingUtils.HandleFeesAsync(tx, portfolio, priceHistoryService);

        return Result.Success();
    }
}
