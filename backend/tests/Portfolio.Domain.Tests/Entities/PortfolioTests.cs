using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.Entities
{
    [TestFixture]
    public class PortfolioTests
    {
        private Mock<IPriceHistoryService> _priceHistoryServiceMock;
        private UserPortfolio _portfolio;
        private AssetHolding _holding;

        [SetUp]
        public void SetUp()
        {
            _priceHistoryServiceMock = new Mock<IPriceHistoryService>();
            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(Result.Failure<decimal>("Price not found"));
            _portfolio = new UserPortfolio();
            _portfolio.SetCostBasisStrategy("LIFO");
            _holding = new AssetHolding("BTC");
        }

        #region contructor

        [Test]
        public void UserPortfolio_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var portfolio = new UserPortfolio();

            // Assert
            portfolio.DefaultCurrency.Should().Be("USD");
            portfolio.Wallets.Should().BeEmpty();
            portfolio.Holdings.Should().BeEmpty();
            portfolio.FinancialEvents.Should().BeEmpty();
        }

        #endregion

        #region Wallet

        [Test]
        public void AddWallet_ShouldAddWalletSuccessfully()
        {
            // Arrange
            var portfolio = new UserPortfolio();
            var wallet = Wallet.Create("Test Wallet").Value;

            // Act
            var result = portfolio.AddWallet(wallet);

            // Assert
            result.IsSuccess.Should().BeTrue();
            portfolio.Wallets.Should().Contain(wallet);
        }

        [Test]
        public void AddWallet_ShouldFailWhenAddingDuplicateWallet()
        {
            // Arrange
            var portfolio = new UserPortfolio();
            var wallet = Wallet.Create("Test Wallet").Value;
            portfolio.AddWallet(wallet);

            // Act
            var result = portfolio.AddWallet(wallet);

            // Assert
            result.IsFailure.Should().BeTrue();
            portfolio.Wallets.Should().HaveCount(1);
        }

        #endregion

        #region SetDefaultCurrency

        [Test]
        public void SetDefaultCurrency_ShouldSetSuccessfully()
        {
            // Arrange
            var portfolio = new UserPortfolio();

            // Act
            var result = portfolio.SetDefaultCurrency("CAD");

            // Assert
            result.IsSuccess.Should().BeTrue();
            portfolio.DefaultCurrency.Should().Be("CAD");
        }

        [Test]
        public void SetDefaultCurrency_ShouldFailForInvalidCurrency()
        {
            // Arrange
            var portfolio = new UserPortfolio();

            // Act
            var result = portfolio.SetDefaultCurrency("INVALID");

            // Assert
            result.IsFailure.Should().BeTrue();
            portfolio.DefaultCurrency.Should().Be("USD");
        }

        #endregion

        #region SettingCostBasisStrategy

        [Test]
        public void SetCostBasisStrategy_ShouldSetStrategySuccessfully()
        {
            // Arrange
            var portfolio = new UserPortfolio();

            // Act
            Action act = () => portfolio.SetCostBasisStrategy("FIFO");

            // Assert
            act.Should().NotThrow<Exception>();
        }

        [Test]
        public void SetCostBasisStrategy_ShouldThrowForInvalidStrategy()
        {
            // Arrange
            var portfolio = new UserPortfolio();

            // Act
            Action act = () => portfolio.SetCostBasisStrategy("INVALID");

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("Cost basis strategy INVALID not supported.");
        }

        #endregion

        #region ProcessingTransactions

        [Test]
        public async Task ProcessTransactions_ShouldProcessSuccessfully()
        {
            // Arrange
            var portfolio = new UserPortfolio();
            var wallet = Wallet.Create("Test Wallet").Value;
            portfolio.AddWallet(wallet);

            var transaction = FinancialTransaction.CreateDeposit(DateTime.UtcNow, new Money(100, "USD"), null, "Test Account", null).Value;
            wallet.AddTransaction(transaction);

            var mockPriceHistoryService = new Mock<IPriceHistoryService>();
            mockPriceHistoryService.Setup(p => p.GetPriceAtCloseTimeAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                                   .ReturnsAsync(Result.Success(100m));

            // Act
            var result = await portfolio.CalculateTradesAsync(mockPriceHistoryService.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task ProcessTransactions_ShouldContinueWhenPriceHistoryUnavailable()
        {
            // Arrange
            var portfolio = new UserPortfolio();
            var wallet = Wallet.Create("Test Wallet").Value;
            portfolio.AddWallet(wallet);

            var transaction = FinancialTransaction.CreateDeposit(DateTime.UtcNow, new Money(100, "BTC"), null, "Test Account", null).Value;
            wallet.AddTransaction(transaction);

            var mockPriceHistoryService = new Mock<IPriceHistoryService>();
            mockPriceHistoryService.Setup(p => p.GetPriceAtCloseTimeAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                                   .ReturnsAsync(Result.Failure<decimal>("Price history unavailable"));

            // Act
            var result = await portfolio.CalculateTradesAsync(mockPriceHistoryService.Object);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region RecordFinancialEvent

        [Test]
        public void RecordFinancialEvent_ShouldNotRecordEventForFiatToAssetPurchase()
        {
            // Arrange
            var tradeDate = DateTime.UtcNow;
            var tradeTransaction = CreateTrade(
                new Money(0.5m, "BTC"),
                new Money(500m, "USD"), // Fiat-to-Asset purchase
                tradeDate
            );

            SetupPriceHistory("BTC", tradeDate, 1200m);

            // Act
            _portfolio.RecordFinancialEvent(tradeTransaction, _holding, 1200m);

            // Assert
            _portfolio.FinancialEvents.Should().BeEmpty(); // No event should be recorded for fiat-to-asset purchases
            _portfolio.DomainEvents.Should().BeEmpty(); // No domain event should be added
        }


        [Test]
        public void RecordFinancialEvent_ShouldNotRecordEventForDeposit()
        {
            // Arrange
            var depositDate = DateTime.UtcNow;
            var depositTransaction = CreateDeposit(new Money(1m, "BTC"), 1000m, depositDate); // Deposit 1 BTC at 1000 USD per BTC

            // Act
            _portfolio.RecordFinancialEvent(depositTransaction, _holding, 1000m);

            // Assert
            _portfolio.FinancialEvents.Should().BeEmpty(); // No event should be recorded for a deposit
            _portfolio.DomainEvents.Should().BeEmpty(); // No domain event should be added
        }

        [Test]
        public void RecordFinancialEvent_ShouldRecordEventSuccessfully()
        {
            // Arrange
            var depositDate = DateTime.UtcNow.AddDays(-1);
            var tradeDate = DateTime.UtcNow;

            CreateDeposit(new Money(1m, "BTC"), 1000m, depositDate); // Deposit 1 BTC at 1000 USD per BTC
            SetupPriceHistory("BTC", tradeDate, 1200m);
            SetupPriceHistory("ETH", tradeDate, 2500m);

            var tradeTransaction = CreateTrade(
                new Money(0.5m, "ETH"),  // Receiving 0.5 ETH
                new Money(0.5m, "BTC"),  // Sending 0.5 BTC
                tradeDate
            ); // Trade 0.5 BTC for 0.5 ETH

            // Act
            _portfolio.RecordFinancialEvent(tradeTransaction, _holding, 1200m);

            // Assert
            _portfolio.FinancialEvents.Should().HaveCount(1); // Only the trade event should be recorded
            var recordedEvent = _portfolio.FinancialEvents.Last();
            recordedEvent.AssetSymbol.Should().Be("BTC");
            recordedEvent.CostBasisPerUnit.Should().Be(1000m);
            recordedEvent.MarketPricePerUnit.Should().Be(1200m);
            recordedEvent.Amount.Should().Be(0.5m);
        }

        [Test]
        public void RecordFinancialEvent_ShouldContinueWhenEventCreationFails()
        {
            // Arrange
            var depositDate = DateTime.UtcNow.AddDays(-1);
            var tradeDate = DateTime.UtcNow;

            CreateDeposit(new Money(1m, "BTC"), 1000m, depositDate); // Deposit 1 BTC at 1000 USD per BTC
            SetupPriceHistory("BTC", tradeDate, 1200m);
            SetupPriceHistory("ETH", tradeDate, 2500m);

            var tradeTransaction = CreateTrade(
                new Money(0.5m, "ETH"),  // Receiving 0.5 ETH
                new Money(0.5m, "BTC"),  // Sending 0.5 BTC
                tradeDate
            ); // Trade 0.5 BTC for 0.5 ETH

            // Manipulating holding's purchase record to cause a failure in event creation
            _holding.PurchaseRecords.Clear();

            // Act
            _portfolio.RecordFinancialEvent(tradeTransaction, _holding, 1200m);

            // Assert
            _portfolio.FinancialEvents.Should().HaveCount(0); // Only the deposit should be recorded
            tradeTransaction.ErrorType.Should().Be(ErrorType.InsufficientFunds);            
            _portfolio.DomainEvents.Should().HaveCount(1);
        }

        private FinancialTransaction CreateDeposit(Money receivedAmount, decimal pricePerUnit, DateTime date)
        {
            var depositTransaction = FinancialTransaction.CreateDeposit(
                date,
                receivedAmount,
                null,
                "Test Account",
                null
            ).Value;

            _holding.AddPurchase(depositTransaction.ReceivedAmount.Amount, pricePerUnit, depositTransaction.DateTime);
            _portfolio.RecordFinancialEvent(depositTransaction, _holding, pricePerUnit);
            return depositTransaction;
        }

        private FinancialTransaction CreateTrade(Money receivedAmount, Money sentAmount, DateTime date)
        {
            return FinancialTransaction.CreateTrade(
                date,
                receivedAmount,
                sentAmount,
                null,
                "Test Account",
                null
            ).Value;
        }

        private void SetupPriceHistory(string currency, DateTime date, decimal price)
        {
            _priceHistoryServiceMock
                .Setup(p => p.GetPriceAtCloseTimeAsync(currency, date))
                .ReturnsAsync(Result.Success(price));
        }


        #endregion

        // [Test]
        // public async Task ProcessAsync_Should_UpdateBalanceAndAveragePrice_OnDeposit()
        // {
        //     // Arrange            
        //     var transactionDate = new DateTime(2024, 8, 22);
        //     var depositAmount = new Money(1m, "BTC");
        //     var feeAmount = new Money(0.1m, "BTC");

        //     var transaction = FinancialTransaction.CreateDeposit(
        //         transactionDate,
        //         depositAmount,
        //         feeAmount,
        //         "a",
        //         [],
        //         "").Value;

        //     var wallet = Wallet.Create("Test Wallet").Value;
        //     wallet.AddTransaction(transaction);

        //     _portfolio.AddWallet(wallet);

        //     _priceHistoryServiceMock
        //         .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
        //         .ReturnsAsync(Result.Success(25000m));

        //     // Act
        //     var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        //     // Assert
        //     result.IsSuccess.Should().BeTrue();
        //     _portfolio.Holdings.Should().HaveCount(1);

        //     var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        //     holding.Balance.Should().Be(1m);
        //     holding.AverageBoughtPrice.Should().Be(25000m);

        //     var purchaseRecord = holding.PurchaseRecords.Should().ContainSingle().Subject;
        //     PortfolioTestUtils.EnsurePurchaseRecord(purchaseRecord, transaction.ReceivedAmount.Amount, 25000m, transaction.DateTime);

        //     transaction.ValueInDefaultCurrency.Amount.Should().Be(transaction.ReceivedAmount.Amount * 25000m);
        //     transaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        //     transaction.FeeValueInDefaultCurrency.Amount.Should().Be(transaction.FeeAmount.Amount * 25000m);
        //     transaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

        //     _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate), Times.AtLeast(1));
        // }

        // [Test]
        // public async Task ProcessAsync_Should_HandleWithdrawal_AndUpdateBalanceAndFees()
        // {
        //     // Arrange
        //     var depositDate = new DateTime(2024, 8, 22);
        //     var withdrawalDate = new DateTime(2024, 8, 23);

        //     var depositAmount = new Money(2m, "BTC");
        //     var withdrawalAmount = new Money(1m, "BTC");

        //     var depositFee = new Money(0.1m, "BTC");
        //     var withdrawalFee = new Money(0.05m, "BTC");

        //     var depositTransaction = FinancialTransaction.CreateDeposit(
        //         depositDate,
        //         depositAmount,
        //         depositFee,
        //         "a",
        //         [],
        //         "").Value;

        //     var withdrawalTransaction = FinancialTransaction.CreateWithdraw(
        //         withdrawalDate,
        //         withdrawalAmount,
        //         withdrawalFee,
        //         "b",
        //         [],
        //         "").Value;

        //     var wallet = Wallet.Create("Test Wallet").Value;
        //     wallet.AddTransaction(depositTransaction);
        //     wallet.AddTransaction(withdrawalTransaction);

        //     _portfolio.AddWallet(wallet);

        //     _priceHistoryServiceMock
        //         .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
        //         .ReturnsAsync(Result.Success(25000m));

        //     _priceHistoryServiceMock
        //         .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate))
        //         .ReturnsAsync(Result.Success(35000m));

        //     // Act
        //     var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        //     // Assert
        //     result.IsSuccess.Should().BeTrue();
        //     _portfolio.Holdings.Should().HaveCount(1);

        //     var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        //     holding.Balance.Should().Be(0.95m); // 2 BTC - 1 BTC withdrawal - 0.05 BTC fee
        //     holding.AverageBoughtPrice.Should().Be(25000m); // No change in average bought price

        //     depositTransaction.ValueInDefaultCurrency.Amount.Should().Be(depositTransaction.ReceivedAmount.Amount * 25000m);
        //     depositTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        //     depositTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(depositTransaction.FeeAmount.Amount * 25000m);
        //     depositTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

        //     withdrawalTransaction.ValueInDefaultCurrency.Amount.Should().Be(withdrawalTransaction.SentAmount.Amount * 35000m);
        //     withdrawalTransaction.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        //     withdrawalTransaction.FeeValueInDefaultCurrency.Amount.Should().Be(withdrawalTransaction.FeeAmount.Amount * 35000m);
        //     withdrawalTransaction.FeeValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);

        //     // If strategy is Adjusted Cost Base (ACB)...
        //     var taxableEvent = _portfolio.FinancialEvents.Should().ContainSingle(h => h.BaseCurrency == "USD").Subject;
        //     taxableEvent.Amount.Should().Be(1m);
        //     taxableEvent.CostBasisPerUnit.Should().Be(25000m);
        //     taxableEvent.MarketPricePerUnit.Should().Be(35000m);

        //     _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
        //     _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", withdrawalDate), Times.AtLeast(1));
        // }

        // [Test]
        // public async Task ProcessAsync_Should_MarkTransactionAsFailed_WhenPriceHistoryUnavailable()
        // {
        //     // Arrange
        //     var transactionDate = new DateTime(2024, 8, 22);
        //     var depositAmount = new Money(1m, "BTC");
        //     var feeAmount = new Money(0.1m, "BTC");

        //     var transaction = FinancialTransaction.CreateDeposit(
        //         transactionDate,
        //         depositAmount,
        //         feeAmount,
        //         "a",
        //         [],
        //         "").Value;

        //     var wallet = Wallet.Create("Test Wallet").Value;
        //     wallet.AddTransaction(transaction);

        //     _portfolio.AddWallet(wallet);

        //     _priceHistoryServiceMock
        //         .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
        //         .ReturnsAsync(Result.Failure<decimal>("Price history unavailable"));

        //     // Act
        //     var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        //     // Assert
        //     result.IsSuccess.Should().BeTrue();

        //     transaction.ErrorType.Should().Be(ErrorType.PriceHistoryUnavailable);

        //     //_priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate), Times.Exactly(3)); // Retries 3 times
        // }

        // [Test]
        // public async Task ProcessAsync_Should_HandleZeroBalanceCorrectly_OnWithdrawal()
        // {
        //     // Arrange
        //     var depositDate = new DateTime(2024, 8, 22);
        //     var withdrawalDate = new DateTime(2024, 8, 23);

        //     var depositAmount = new Money(1m, "BTC");
        //     var withdrawalAmount = new Money(1m, "BTC");

        //     var depositFee = new Money(0.1m, "BTC");
        //     var withdrawalFee = new Money(0m, "BTC");

        //     var depositTransaction = FinancialTransaction.CreateDeposit(
        //         depositDate,
        //         depositAmount,
        //         depositFee,
        //         "a",
        //         [],
        //         "").Value;

        //     var withdrawalTransaction = FinancialTransaction.CreateWithdraw(
        //         withdrawalDate,
        //         withdrawalAmount,
        //         withdrawalFee,
        //         "b",
        //         [],
        //         "").Value;

        //     var wallet = Wallet.Create("Test Wallet").Value;
        //     wallet.AddTransaction(depositTransaction);
        //     wallet.AddTransaction(withdrawalTransaction);

        //     _portfolio.AddWallet(wallet);

        //     _priceHistoryServiceMock
        //         .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
        //         .ReturnsAsync(Result.Success(25000m));

        //     // Act
        //     var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        //     // Assert
        //     result.IsSuccess.Should().BeTrue();
        //     _portfolio.Holdings.Should().HaveCount(1);

        //     var holding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        //     holding.Balance.Should().Be(0m); // 1 BTC - 1 BTC withdrawal
        //     holding.AverageBoughtPrice.Should().Be(0m); // Reset to zero when balance is zero

        //     _priceHistoryServiceMock.Verify(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate), Times.AtLeast(1));
        // }
    }
}
