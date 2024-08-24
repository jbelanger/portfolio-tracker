using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Portfolio.Domain.Tests.Helpers
{
    public static class TestHelper
    {
        public static AssetHolding CreateAssetHolding(string asset, List<(decimal Amount, decimal Price, DateTime Date)> purchases)
        {
            var holding = new AssetHolding(asset);

            foreach (var (amount, price, date) in purchases)
            {
                holding.AddPurchase(amount, price, date);
            }

            return holding;
        }

        public static FinancialTransaction CreateFinancialTransaction(TransactionType type, string asset, decimal amount, DateTime dateTime)
        {
            return type switch
            {
                TransactionType.Withdrawal => FinancialTransaction.CreateWithdraw(
                    dateTime,
                    new Money(amount, asset),
                    Money.Empty,
                    "TestAccount",
                    new List<string> { "TestId" }
                ).Value,
                
                TransactionType.Deposit => FinancialTransaction.CreateDeposit(
                    dateTime,
                    new Money(amount, asset),
                    Money.Empty,
                    "TestAccount",
                    new List<string> { "TestId" }
                ).Value,

                TransactionType.Trade => FinancialTransaction.CreateTrade(
                    dateTime,
                    new Money(amount, asset),
                    new Money(amount, asset),
                    Money.Empty,
                    "TestAccount",
                    new List<string> { "TestId" }
                ).Value,

                _ => throw new NotSupportedException($"Transaction type {type} is not supported.")
            };
        }
    }
}
