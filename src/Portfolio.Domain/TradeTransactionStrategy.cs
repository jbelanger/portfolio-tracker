using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;
public class TradeTransactionStrategy : ITransactionStrategy
{
    public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (!EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);
        if (!EnsureAboveZeroAmount(tx, false)) return Result.Failure(tx.ErrorMessage);

        var receiver = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
        var sender = portfolio.GetOrCreateHolding(tx.SentAmount.CurrencyCode);

        var tradedCostInUsd = await CalculateTradedCostInUsdAsync(tx, sender, portfolio, priceHistoryService);
        tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);

        UpdateReceiverAverageBoughtPrice(tx, receiver);

        CreateTaxableEvent(tx, sender, portfolio, tradedCostInUsd);

        UpdateBalances(tx, receiver, sender);
        HandleFees(tx, portfolio, priceHistoryService);

        return Result.Success();
    }

    private static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx, bool incoming = true)
    {
        if ((incoming && tx.ReceivedAmount.Amount <= 0) || (!incoming && tx.SentAmount.Amount <= 0))
        {
            tx.ErrorMessage = $"Amount is zero or negative in trade transaction: {tx.TransactionIds}";
            tx.ErrorType = ErrorType.InvalidCurrency;
            return false;
        }
        return true;
    }

    private async Task<decimal> CalculateTradedCostInUsdAsync(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding sender, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (tx.ReceivedAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            return tx.ReceivedAmount.Amount;
        }
        else if (tx.SentAmount.CurrencyCode == portfolio.DefaultCurrency)
        {
            return tx.SentAmount.Amount;
        }
        else
        {
            var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);
            if (priceResult.IsSuccess)
            {
                return tx.SentAmount.Amount * priceResult.Value;
            }
            else
            {
                tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";
                return sender.AverageBoughtPrice * tx.SentAmount.Amount; // Fallback to average bought price
            }
        }
    }

    private static void UpdateReceiverAverageBoughtPrice(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding receiver)
    {
        receiver.AverageBoughtPrice = (receiver.AverageBoughtPrice * receiver.Balance + tx.ValueInDefaultCurrency.Amount) / (receiver.Balance + tx.ReceivedAmount.Amount);
    }

    private void CreateTaxableEvent(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding sender, UserPortfolio portfolio, decimal tradedCostInUsd)
    {
        var taxableEventResult = TaxableEvent.Create(tx.DateTime, tx.SentAmount.CurrencyCode, sender.AverageBoughtPrice, tradedCostInUsd / tx.SentAmount.Amount, tx.SentAmount.Amount, portfolio.DefaultCurrency);
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

    private static void UpdateBalances(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding receiver, CryptoCurrencyHolding sender)
    {
        receiver.Balance += tx.ReceivedAmount.Amount;
        sender.Balance -= tx.SentAmount.Amount;

        if (sender.Balance == 0)
            sender.AverageBoughtPrice = 0m;

        EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
    }

    private async void HandleFees(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (tx.FeeAmount == Money.Empty) return;

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

    private static void EnsureBalanceNotNegative(CryptoCurrencyRawTransaction tx, string asset, decimal balance)
    {
        if (balance < 0)
        {
            tx.ErrorType = ErrorType.InsufficientFunds;
            tx.ErrorMessage = $"{asset} balance is under zero: {balance}";
        }
    }
}
