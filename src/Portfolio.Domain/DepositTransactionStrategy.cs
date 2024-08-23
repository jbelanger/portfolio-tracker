using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class DepositTransactionStrategy : ITransactionStrategy
{
    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);

        var receiver = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
        receiver.Balance += tx.ReceivedAmount.Amount;

        if (tx.ReceivedAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            receiver.AverageBoughtPrice = 1;
            tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount, portfolio.DefaultCurrency);
        }
        else
        {
            var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.ReceivedAmount.CurrencyCode, tx.DateTime);
            if (priceResult.IsSuccess)
            {
                decimal price = priceResult.Value;
                tx.ValueInDefaultCurrency = new Money(tx.ReceivedAmount.Amount * price, portfolio.DefaultCurrency);
                receiver.AverageBoughtPrice = (receiver.AverageBoughtPrice * (receiver.Balance - tx.ReceivedAmount.Amount) + tx.ValueInDefaultCurrency.Amount) / receiver.Balance;
            }
            else
            {
                tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                tx.ErrorMessage = $"Could not get price history for {receiver.Asset}. Average price will be incorrect.";
            }
        }

        // Handle Fees
        if (tx.FeeAmount != Money.Empty)
        {
            var fees = portfolio.GetOrCreateHolding(tx.FeeAmount.CurrencyCode);
            bool shouldDeductFeesFromBalance = tx.FeeAmount.CurrencyCode != tx.ReceivedAmount.CurrencyCode;

            if (shouldDeductFeesFromBalance)
                fees.Balance -= tx.FeeAmount.Amount;

            if (tx.FeeAmount.CurrencyCode == portfolio.DefaultCurrency)
            {
                tx.FeeValueInDefaultCurrency = tx.FeeAmount;
            }
            else
            {
                var feePriceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.FeeAmount.CurrencyCode, tx.DateTime);
                if (feePriceResult.IsSuccess)
                {
                    decimal feePrice = feePriceResult.Value;
                    tx.FeeValueInDefaultCurrency = new Money(tx.FeeAmount.Amount * feePrice, portfolio.DefaultCurrency);
                }
                else
                {
                    tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                    tx.ErrorMessage = $"Could not get price history for {fees.Asset} fees. Fees calculations will be incorrect.";
                }
            }

            EnsureBalanceNotNegative(tx, fees.Asset, fees.Balance);
        }

        return Result.Success();
    }

    private static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx)
    {
        if (tx.ReceivedAmount.Amount <= 0)
        {
            tx.ErrorMessage = $"Received amount is zero or negative in deposit transaction: {tx.TransactionIds}";
            tx.ErrorType = ErrorType.InvalidCurrency;
            return false;
        }
        return true;
    }

    private static void EnsureBalanceNotNegative(CryptoCurrencyRawTransaction tx, string asset, decimal balance)
    {
        if (balance < 0)
        {
            tx.ErrorType = ErrorType.InsufficientFunds;
            tx.ErrorMessage = $"{asset} balance is under zero: {balance}";
        }
    }
}
