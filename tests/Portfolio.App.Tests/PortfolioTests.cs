using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using Portfolio.App.HistoricalPrice;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Tests
{
    [TestFixture]
    public class PortfolioTests
    {
        private Mock<IPriceHistoryService> _priceHistoryServiceMock;
        private Portfolio.App.Portfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            _priceHistoryServiceMock = new Mock<IPriceHistoryService>();
            _portfolio = new App.Portfolio(_priceHistoryServiceMock.Object);
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

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
                .ReturnsAsync(Result.Success(25000m));

            // Act
            var result = await _portfolio.ProcessAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(1);

            var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            holding.Balance.Should().Be(1m);
            holding.AverageBoughtPrice.Should().Be(25000m);
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

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
                .ReturnsAsync(Result.Success(25000m));

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate))
                .ReturnsAsync(Result.Success(35000m));

            // Act
            var result = await _portfolio.ProcessAsync();

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

            var taxableEvent = _portfolio.TaxableEvents.Should().ContainSingle(h => h.Currency == "USD").Subject;
            taxableEvent.Amount.Should().Be(1m);
            taxableEvent.AverageCost.Should().Be(25000m);
            taxableEvent.ValueAtDisposal.Should().Be(35000m);

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate), Times.AtLeast(1));            
        }

        [Test]
        public async Task ProcessAsync_Should_HandleTrade_WithFeeOnSender()
        {
            // Arrange
            var depositDate = new DateTime(2024, 8, 21);
            var tradeDate = new DateTime(2024, 8, 22);

            var depositAmount = new Money(2m, "ETH"); // First deposit 2 ETH
            var sentAmount = new Money(1m, "ETH");
            var receivedAmount = new Money(0.1m, "BTC");
            var tradeFee = new Money(0.01m, "ETH");

            var depositTransaction = CryptoCurrencyRawTransaction.CreateDeposit(
                depositDate,
                depositAmount,
                Money.Empty, // No fee on deposit
                "deposit-1",
                [],
                "").Value;

            var tradeTransaction = CryptoCurrencyRawTransaction.CreateTrade(
                tradeDate,
                receivedAmount,
                sentAmount,
                tradeFee,
                "a",
                [],
                "").Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(depositTransaction);
            wallet.AddTransaction(tradeTransaction);

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", depositDate))
                .ReturnsAsync(Result.Success(2000m));

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
                .ReturnsAsync(Result.Success(25000m));

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
                .ReturnsAsync(Result.Success(2500m));

            // Act
            var result = await _portfolio.ProcessAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(2);

            var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            btcHolding.Balance.Should().Be(0.1m);
            btcHolding.AverageBoughtPrice.Should().BeApproximately(25000m, 0.01m); // 1 ETH * 2000 / 0.1 BTC = 20000 USD

            var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
            ethHolding.Balance.Should().Be(0.99m); // 2 ETH - 1 ETH trade - 0.01 ETH fee
            
            depositTransaction.ValueInDefaultCurrency.Amount.Should().Be(depositTransaction.ReceivedAmount.Amount * 2000m);
            depositTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            depositTransaction.FeeValueInDefaultCurrency.Should().Be(Money.Empty);            
                        
            tradeTransaction.ValueInDefaultCurrency.Amount.Should().Be(tradeTransaction.SentAmount.Amount * 2500m);
            tradeTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
            tradeTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(tradeTransaction.FeeAmount.Amount * 2500m);
            tradeTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

            var taxableEvent = _portfolio.TaxableEvents.Should().ContainSingle(h => h.Currency == "USD").Subject;
            taxableEvent.DisposedAsset.Should().Be("ETH");
            taxableEvent.Amount.Should().Be(tradeTransaction.SentAmount.Amount);
            taxableEvent.AverageCost.Should().BeApproximately(2000m, 0.01m);
            taxableEvent.ValueAtDisposal.Should().Be(2500m);

            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("ETH", depositDate), Times.AtLeastOnce());
            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate), Times.AtLeastOnce());
        }


        [Test]
        public async Task ProcessAsync_Should_HandleTrade_WithFeeOnReceiver()
        {
            // Arrange
            var tradeDate = new DateTime(2024, 8, 22);

            var sentAmount = new Money(1m, "ETH");
            var receivedAmount = new Money(0.09m, "BTC");
            var tradeFee = new Money(0.01m, "BTC");

            var tradeTransaction = CryptoCurrencyRawTransaction.CreateTrade(
                tradeDate,
                receivedAmount,
                sentAmount,
                tradeFee,
                "a",
                [],
                "").Value;

            var wallet = Wallet.Create("Test Wallet").Value;
            wallet.AddTransaction(tradeTransaction);

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
                .ReturnsAsync(Result.Success(25000m));

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
                .ReturnsAsync(Result.Success(2000m));

            // Act
            var result = await _portfolio.ProcessAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            _portfolio.Holdings.Should().HaveCount(2);

            var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
            btcHolding.Balance.Should().Be(0.09m); // 0.1 BTC - 0.01 BTC fee
            btcHolding.AverageBoughtPrice.Should().BeApproximately(22222.22m, 0.01m); // (1 ETH * 2000) / 0.09 BTC = 22222.22 USD

            var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
            ethHolding.Balance.Should().Be(-1m); // 1 ETH - 1 ETH sent

            //_priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate), Times.Once);
            _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate), Times.Once);
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

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
                .ReturnsAsync(Result.Failure<decimal>("Price history unavailable"));

            // Act
            var result = await _portfolio.ProcessAsync();

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

            _portfolio.Wallets = new List<Wallet> { wallet };

            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
                .ReturnsAsync(Result.Success(25000m));

            // Act
            var result = await _portfolio.ProcessAsync();

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
