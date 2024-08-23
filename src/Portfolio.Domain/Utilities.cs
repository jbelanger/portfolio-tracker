using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public static class TransactionValidationUtils
{
    public static bool EnsureAboveZeroAmount(CryptoCurrencyRawTransaction tx, bool incoming = true)
    {
        if (incoming && tx.ReceivedAmount.Amount <= 0)
        {
            tx.ErrorMessage = $"Received amount is zero or negative in transaction: {tx.TransactionIds}";
            tx.ErrorType = ErrorType.InvalidCurrency;
            return false;
        }

        if (!incoming && tx.SentAmount.Amount <= 0)
        {
            tx.ErrorMessage = $"Sent amount is zero or negative in transaction: {tx.TransactionIds}";
            tx.ErrorType = ErrorType.InvalidCurrency;
            return false;
        }

        if (tx.FeeAmount.Amount < 0)
        {
            tx.ErrorMessage = $"Fee amount is negative in transaction: {tx.TransactionIds}";
            tx.ErrorType = ErrorType.InvalidCurrency;
            return false;
        }

        return true;
    }

    public static void EnsureBalanceNotNegative(CryptoCurrencyRawTransaction tx, string asset, decimal balance)
    {
        if (balance < 0)
        {
            tx.ErrorType = ErrorType.InsufficientFunds;
            tx.ErrorMessage = $"{asset} balance is under zero: {balance}";
        }
    }
}


public static class FeeHandlingUtils
{
    public static async Task HandleFeesAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        if (tx.FeeAmount == Money.Empty)
            return;

        var feeHolding = portfolio.GetOrCreateHolding(tx.FeeAmount.CurrencyCode);

        // Determine if fees should be deducted from the balance
        bool shouldDeductFeesFromBalance = tx.FeeAmount.CurrencyCode != tx.ReceivedAmount.CurrencyCode;
        if (shouldDeductFeesFromBalance)
        {
            DeductFeeFromHolding(tx.FeeAmount, feeHolding);
        }

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
                tx.ErrorMessage = $"Could not get price history for {feeHolding.Asset} fees. Fees calculations will be incorrect.";
            }
        }

        TransactionValidationUtils.EnsureBalanceNotNegative(tx, feeHolding.Asset, feeHolding.Balance);
    }

    private static void DeductFeeFromHolding(Money feeAmount, CryptoCurrencyHolding holding)
    {
        decimal feeAmountToDeduct = feeAmount.Amount;

        for (int i = 0; i < holding.PurchaseRecords.Count; i++)
        {
            var record = holding.PurchaseRecords[i];

            if (record.Amount >= feeAmountToDeduct)
            {
                holding.PurchaseRecords[i] = new PurchaseRecord(record.Amount - feeAmountToDeduct, record.PricePerUnit, record.PurchaseDate);
                if (holding.PurchaseRecords[i].Amount == 0)
                {
                    holding.PurchaseRecords.RemoveAt(i);
                }
                feeAmountToDeduct = 0;
                break;
            }
            else
            {
                feeAmountToDeduct -= record.Amount;
                holding.PurchaseRecords.RemoveAt(i);
                i--; // Adjust index after removal
            }
        }

        holding.Balance -= feeAmount.Amount;

        if (holding.Balance == 0)
        {
            holding.PurchaseRecords.Clear();
        }
    }
}
