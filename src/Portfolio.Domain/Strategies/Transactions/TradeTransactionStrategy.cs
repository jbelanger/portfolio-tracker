using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Events;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Strategies.Transactions
{
    public class TradeTransactionStrategy : ITransactionStrategy
    {
        public async Task<Result> ProcessTransactionAsync(FinancialTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
        {
            if (!EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);
            if (!EnsureAboveZeroAmount(tx, false)) return Result.Failure(tx.ErrorMessage);

            var receiver = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);
            var sender = portfolio.GetOrCreateHolding(tx.SentAmount.CurrencyCode);
            var priceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(tx.SentAmount.CurrencyCode, tx.DateTime);

            decimal price;
            if (priceResult.IsSuccess)
            {
                price = priceResult.Value;
            }
            else
            {
                tx.ErrorType = ErrorType.PriceHistoryUnavailable;
                tx.ErrorMessage = $"Could not get price history for {sender.Asset}. Average price will be incorrect.";
                price = sender.AverageBoughtPrice;// Fallback to average bought price
            }

            var tradedCostInUsd = CalculateTradedCostInUsd(tx, portfolio, price);
            tx.ValueInDefaultCurrency = new Money(tradedCostInUsd, portfolio.DefaultCurrency);

            UpdateReceiverAverageBoughtPrice(tx, receiver);

            portfolio.RecordFinancialEvent(tx, sender, price);

            UpdateBalances(tx, receiver, sender);
            await HandleFees(tx, portfolio, priceHistoryService);

            return Result.Success();
        }

        private static bool EnsureAboveZeroAmount(FinancialTransaction tx, bool incoming = true)
        {
            if (incoming && tx.ReceivedAmount.Amount <= 0 || !incoming && tx.SentAmount.Amount <= 0)
            {
                tx.ErrorMessage = $"Amount is zero or negative in trade transaction: {tx.TransactionIds}";
                tx.ErrorType = ErrorType.InvalidCurrency;
                return false;
            }
            return true;
        }

        private decimal CalculateTradedCostInUsd(FinancialTransaction tx, UserPortfolio portfolio, decimal price)
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
                return tx.SentAmount.Amount * price;
            }
        }

        private static void UpdateReceiverAverageBoughtPrice(FinancialTransaction tx, AssetHolding receiver)
        {
            receiver.AverageBoughtPrice = (receiver.AverageBoughtPrice * receiver.Balance + tx.ValueInDefaultCurrency.Amount) / (receiver.Balance + tx.ReceivedAmount.Amount);
        }

        private static void UpdateBalances(FinancialTransaction tx, AssetHolding receiver, AssetHolding sender)
        {
            receiver.Balance += tx.ReceivedAmount.Amount;
            sender.Balance -= tx.SentAmount.Amount;

            if (sender.Balance == 0)
                sender.AverageBoughtPrice = 0m;

            EnsureBalanceNotNegative(tx, sender.Asset, sender.Balance);
        }

        private async Task HandleFees(FinancialTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
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

        private static void EnsureBalanceNotNegative(FinancialTransaction tx, string asset, decimal balance)
        {
            if (balance < 0)
            {
                tx.ErrorType = ErrorType.InsufficientFunds;
                tx.ErrorMessage = $"{asset} balance is under zero: {balance}";
            }
        }
    }
}
