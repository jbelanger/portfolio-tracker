using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class WithdrawalTransactionStrategy : ITransactionStrategy
{
    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!EnsureAboveZeroAmount(tx, false)) return Result.Failure(tx.ErrorMessage);

        var sender = portfolio.GetOrCreateHolding(tx.SentAmount.CurrencyCode);
        var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
        if (priceResult.IsSuccess)
        {
            decimal price = priceResult.Value;
            tx.ValueInDefaultCurrency = new Money(tx.SentAmount.Amount * price, portfolio.DefaultCurrency);

            var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, price, tx.SentAmount.Amount, portfolio.DefaultCurrency);
            if (taxableEventResult.IsSuccess)
            {
                portfolio.AddTaxableEvent(taxableEventResult.Value);
            }
            else
            {
                tx.ErrorMessage = "Could not create taxable event for this transaction.";
                tx.ErrorType = ErrorType.TaxEventNotCreated;
            }
        }
        else
        {
            tx.ErrorType = ErrorType.PriceHistoryUnavailable;
            tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";
        }

        sender.Balance -= tx.SentAmount.Amount;
        if (sender.Balance == 0)
            sender.AverageBoughtPrice = 0m;

        EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);

        // Handle Fees
        if (tx.FeeAmount != Money.Empty)
        {
            var fees = portfolio.GetOrCreateHolding(tx.FeeAmount.CurrencyCode);
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

    private static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx, bool incoming = true)
    {
        if (tx.SentAmount.Amount <= 0)
        {
            tx.ErrorMessage = $"Sent amount is zero or negative in withdrawal transaction: {tx.TransactionIds}";
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
