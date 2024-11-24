using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain
{
    public static class TransactionValidationUtils
    {
        public static bool EnsureAboveZeroAmount(FinancialTransaction tx, bool incoming = true)
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

        public static void EnsureBalanceNotNegative(FinancialTransaction tx, string asset, decimal balance)
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
        public static async Task HandleFeesAsync(FinancialTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
        {
            if (tx.FeeAmount == Money.Empty) return;

            var fees = portfolio.GetOrCreateHolding(tx.FeeAmount.CurrencyCode);

            bool shouldDeductFeesFromBalance = ShouldDeductFeesFromBalance(tx);
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

            TransactionValidationUtils.EnsureBalanceNotNegative(tx, fees.Asset, fees.Balance);
        }

        private static bool ShouldDeductFeesFromBalance(FinancialTransaction tx)
        {
            // For deposits and trades, fees might not be deducted from the balance if paid in the same currency as the received amount.
            return tx.FeeAmount.CurrencyCode != tx.ReceivedAmount.CurrencyCode || tx.Type == TransactionType.Withdrawal;
        }
    }
}
