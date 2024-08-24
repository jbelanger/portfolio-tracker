using FluentAssertions;
using NUnit.Framework;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Strategies.CostBasis;
using Portfolio.Domain.ValueObjects;
using System;

namespace Portfolio.Tests
{
    [TestFixture]
    public class FifoCostBasisCalculationStrategyTests
    {
        private FifoCostBasisCalculationStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _strategy = new FifoCostBasisCalculationStrategy();
        }

        [Test]
        public void CalculateCostBasis_ShouldReturnCorrectCostBasis_WhenSinglePurchaseRecordExists()
        {
            // Arrange
            var holding = new AssetHolding("BTC");
            holding.AddPurchase(1, 10000, DateTime.UtcNow.AddDays(-10));
            var transaction = FinancialTransaction.CreateWithdraw(DateTime.UtcNow, new Money(0.5m, "BTC"), null, "TestAccount", null).Value;

            // Act
            var result = _strategy.CalculateCostBasis(holding, transaction);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(5000);
            transaction.ErrorType.Should().Be(ErrorType.None);
            transaction.ErrorMessage.Should().BeEmpty();
        }

        [Test]
        public void CalculateCostBasis_ShouldReturnCorrectCostBasis_WhenMultiplePurchaseRecordsExist()
        {
            // Arrange
            var holding = new AssetHolding("BTC");
            holding.AddPurchase(1, 10000, DateTime.UtcNow.AddDays(-10));
            holding.AddPurchase(1, 15000, DateTime.UtcNow.AddDays(-5));
            var transaction = FinancialTransaction.CreateWithdraw(DateTime.UtcNow, new Money(1.5m, "BTC"), null, "TestAccount", null).Value;

            // Act
            var result = _strategy.CalculateCostBasis(holding, transaction);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(17500); // (1 * 10000) + (0.5 * 15000)
            transaction.ErrorType.Should().Be(ErrorType.None);
            transaction.ErrorMessage.Should().BeEmpty();
        }

        [Test]
        public void CalculateCostBasis_ShouldReturnPartialCostBasis_WhenTransactionAmountIsLessThanHoldingAmount()
        {
            // Arrange
            var holding = new AssetHolding("BTC");
            holding.AddPurchase(2, 10000, DateTime.UtcNow.AddDays(-10));
            var transaction = FinancialTransaction.CreateWithdraw(DateTime.UtcNow, new Money(1m, "BTC"), null, "TestAccount", null).Value;

            // Act
            var result = _strategy.CalculateCostBasis(holding, transaction);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(10000); // 1 BTC at 10000 USD
            transaction.ErrorType.Should().Be(ErrorType.None);
            transaction.ErrorMessage.Should().BeEmpty();
        }

        [Test]
        public void CalculateCostBasis_ShouldReturnFailureAndLogError_WhenTransactionAmountExceedsHoldingAmount()
        {
            // Arrange
            var holding = new AssetHolding("BTC");
            holding.AddPurchase(1, 10000, DateTime.UtcNow.AddDays(-10));
            holding.AddPurchase(1, 15000, DateTime.UtcNow.AddDays(-5));
            var transaction = FinancialTransaction.CreateWithdraw(DateTime.UtcNow, new Money(2.5m, "BTC"), null, "TestAccount", null).Value;

            // Act
            var result = _strategy.CalculateCostBasis(holding, transaction);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Insufficient holdings to match the transaction amount. Unable to match 0.5 BTC.");
            transaction.ErrorType.Should().Be(ErrorType.InsufficientFunds);
            transaction.ErrorMessage.Should().Be("Insufficient holdings to match the transaction amount. Unable to match 0.5 BTC.");
        }

        [Test]
        public void CalculateCostBasis_ShouldReturnZeroAndLogError_WhenNoPurchaseRecordsExist()
        {
            // Arrange
            var holding = new AssetHolding("BTC");
            var transaction = FinancialTransaction.CreateWithdraw(DateTime.UtcNow, new Money(1m, "BTC"), null, "TestAccount", null).Value;

            // Act
            var result = _strategy.CalculateCostBasis(holding, transaction);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Insufficient holdings to match the transaction amount. Unable to match 1 BTC.");
            transaction.ErrorType.Should().Be(ErrorType.InsufficientFunds);
            transaction.ErrorMessage.Should().Be("Insufficient holdings to match the transaction amount. Unable to match 1 BTC.");
        }
    }
}
