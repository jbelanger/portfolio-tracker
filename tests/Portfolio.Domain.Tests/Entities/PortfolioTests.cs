using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.Entities
{
    [TestFixture]
    public class PortfolioTests
    {
        private Mock<IPriceHistoryService> _priceHistoryServiceMock;
        private UserPortfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            _priceHistoryServiceMock = new Mock<IPriceHistoryService>();
            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Failure<decimal>("Price not found"));

            var taxCalculationStrategy = new GenericTaxCalculationStrategy();
            var costBasisStrategy = new AcbCostBasisCalculationStrategy();

            var transactionStrategies = new Dictionary<TransactionType, ITransactionStrategy>
            {
                { TransactionType.Deposit, new DepositTransactionStrategy() },
                { TransactionType.Withdrawal, new WithdrawalTransactionStrategy(taxCalculationStrategy, costBasisStrategy) },
                { TransactionType.Trade, new TradeTransactionStrategy(taxCalculationStrategy, costBasisStrategy) }
            };

            var transactionProcessor = new TransactionProcessor(transactionStrategies);
            _portfolio = new UserPortfolio(transactionProcessor);
        }

        [Test]
        public async Task ProcessAsync_Should_UpdateBalanceAndAveragePrice_OnDeposit()
        {
            // Arrange
            var transactionDate = new DateTime(2024, 8, 22);
            var depositAmount = new Money(1m, "BTC");
            var feeAmount = new Money(0.1m, "BTC");

            var transaction = CryptoCurrencyRawTransaction.CreateDeposit(
                transactionDate,
                depositAmount,
                feeAmount,
                "a",
                Array.Empty<string>(),
                string.Empty).Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(transaction);

            _portfolio.AddWallet(wallet);

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
                .ReturnsAsync(Result.Success(25000m));

            // Act
            var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(1);

            var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            holding.Balance.Should().Be(1m);

            // The new implementation no longer stores the average bought price in the holding
            var purchaseRecord = holding.PurchaseRecords.Should().ContainSingle().Subject;
            purchaseRecord.PricePerUnit.Should().Be(25000m);
            purchaseRecord.Amount.Should().Be(1m);

            transaction.ValueInDefaultCurrency.Amount.Should().Be(depositAmount.Amount * 25000m);
            transaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            transaction.FeeValueInDefaultCurrency.Amount.Should().Be(feeAmount.Amount * 25000m);
            transaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate), Times.AtLeast(1));
        }

        [Test]
        public async Task ProcessAsync_Should_HandleWithdrawal_AndUpdateBalanceAndFees()
        {
            // Arrange
            var depositDate = new DateTime(2024, 8, 22);
            var withdrawalDate = new DateTime(2024, 8, 23);

            var depositAmount = new Money(2m, "BTC");
            var withdrawalAmount = new Money(1m, "BTC");

            var depositFee = new Money(0.1m, "BTC");
            var withdrawalFee = new Money(0.05m, "BTC");

            var depositTransaction = CryptoCurrencyRawTransaction.CreateDeposit(
                depositDate,
                depositAmount,
                depositFee,
                "a",
                Array.Empty<string>(),
                string.Empty).Value;

            var withdrawalTransaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                withdrawalDate,
                withdrawalAmount,
                withdrawalFee,
                "b",
                Array.Empty<string>(),
                string.Empty).Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(depositTransaction);
            wallet.AddTransaction(withdrawalTransaction);

            _portfolio.AddWallet(wallet);

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
                .ReturnsAsync(Result.Success(25000m));

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate))
                .ReturnsAsync(Result.Success(35000m));

            // Act
            var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(1);

            var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            holding.Balance.Should().Be(0.95m); // 2 BTC - 1 BTC withdrawal - 0.05 BTC fee

            // Check the updated purchase records
            holding.PurchaseRecords.Should().HaveCount(1);
            var remainingPurchaseRecord = holding.PurchaseRecords.Single();
            remainingPurchaseRecord.Amount.Should().Be(0.95m);

            depositTransaction.ValueInDefaultCurrency.Amount.Should().Be(depositAmount.Amount * 25000m);
            depositTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            depositTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(depositFee.Amount * 25000m);
            depositTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            withdrawalTransaction.ValueInDefaultCurrency.Amount.Should().Be(withdrawalAmount.Amount * 35000m);
            withdrawalTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            withdrawalTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(withdrawalFee.Amount * 35000m);
            withdrawalTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            var taxableEvent = _portfolio.TaxableEvents.Should().ContainSingle().Subject;
            taxableEvent.Currency.Should().Be("USD");
            taxableEvent.Amount.Should().Be(1m);
            taxableEvent.AverageCost.Should().Be(25000m);
            taxableEvent.ValueAtDisposal.Should().Be(35000m);

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate), Times.AtLeast(1));
        }

        [Test]
        public async Task ProcessAsync_Should_MarkTransactionAsFailed_WhenPriceHistoryUnavailable()
        {
            // Arrange
            var transactionDate = new DateTime(2024, 8, 22);
            var depositAmount = new Money(1m, "BTC");
            var feeAmount = new Money(0.1m, "BTC");

            var transaction = CryptoCurrencyRawTransaction.CreateDeposit(
                transactionDate,
                depositAmount,
                feeAmount,
                "a",
                Array.Empty<string>(),
                string.Empty).Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(transaction);

            _portfolio.AddWallet(wallet);

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
                .ReturnsAsync(Result.Failure<decimal>("Price history unavailable"));

            // Act
            var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();

            transaction.ErrorType.Should().Be(ErrorType.PriceHistoryUnavailable);

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate), Times.AtLeast(1));
        }

        [Test]
        public async Task ProcessAsync_Should_HandleZeroBalanceCorrectly_OnWithdrawal()
        {
            // Arrange
            var depositDate = new DateTime(2024, 8, 22);
            var withdrawalDate = new DateTime(2024, 8, 23);

            var depositAmount = new Money(1m, "BTC");
            var withdrawalAmount = new Money(1m, "BTC");

            var depositFee = new Money(0.1m, "BTC");
            var withdrawalFee = new Money(0m, "BTC");

            var depositTransaction = CryptoCurrencyRawTransaction.CreateDeposit(
                depositDate,
                depositAmount,
                depositFee,
                "a",
                Array.Empty<string>(),
                string.Empty).Value;

            var withdrawalTransaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                withdrawalDate,
                withdrawalAmount,
                withdrawalFee,
                "b",
                Array.Empty<string>(),
                string.Empty).Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(depositTransaction);
            wallet.AddTransaction(withdrawalTransaction);

            _portfolio.AddWallet(wallet);

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
                .ReturnsAsync(Result.Success(25000m));

            // Act
            var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(1);

            var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            holding.Balance.Should().Be(0m); // 1 BTC - 1 BTC withdrawal

            holding.PurchaseRecords.Should().BeEmpty(); // No remaining purchase records when balance is zero

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
        }
    }
}
