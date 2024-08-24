using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.Entities
{
    [TestFixture]
    public class PortfolioTests
    {
        private Mock<IPriceHistoryService> _priceHistoryServiceMock;
        private Domain.Entities.UserPortfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            _priceHistoryServiceMock = new Mock<IPriceHistoryService>();
            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Failure<decimal>("Price not found"));
            _portfolio = new Domain.Entities.UserPortfolio();
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
                [],
                "").Value;

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
            holding.AverageBoughtPrice.Should().Be(25000m);
            
            var purchaseRecord = holding.PurchaseRecords.Should().ContainSingle().Subject;            
            PortfolioTestUtils.EnsurePurchaseRecord(purchaseRecord, transaction.ReceivedAmount.Amount, 25000m, transaction.DateTime);

            transaction.ValueInDefaultCurrency.Amount.Should().Be(transaction.ReceivedAmount.Amount * 25000m);
            transaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            transaction.FeeValueInDefaultCurrency.Amount.Should().Be(transaction.FeeAmount.Amount * 25000m);
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
                [],
                "").Value;

            var withdrawalTransaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                withdrawalDate,
                withdrawalAmount,
                withdrawalFee,
                "b",
                [],
                "").Value;

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
            holding.AverageBoughtPrice.Should().Be(25000m); // No change in average bought price

            depositTransaction.ValueInDefaultCurrency.Amount.Should().Be(depositTransaction.ReceivedAmount.Amount * 25000m);
            depositTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            depositTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(depositTransaction.FeeAmount.Amount * 25000m);
            depositTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            withdrawalTransaction.ValueInDefaultCurrency.Amount.Should().Be(withdrawalTransaction.SentAmount.Amount * 35000m);
            withdrawalTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            withdrawalTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(withdrawalTransaction.FeeAmount.Amount * 35000m);
            withdrawalTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            // If strategy is Adjusted Cost Base (ACB)...
            var taxableEvent = _portfolio.TaxableEvents.Should().ContainSingle(h => h.Currency == "USD").Subject;
            taxableEvent.Quantity.Should().Be(1m);
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
                [],
                "").Value;

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

            //_priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate), Times.Exactly(3)); // Retries 3 times
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
                [],
                "").Value;

            var withdrawalTransaction = CryptoCurrencyRawTransaction.CreateWithdraw(
                withdrawalDate,
                withdrawalAmount,
                withdrawalFee,
                "b",
                [],
                "").Value;

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
            holding.AverageBoughtPrice.Should().Be(0m); // Reset to zero when balance is zero

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
        }
    }
}
