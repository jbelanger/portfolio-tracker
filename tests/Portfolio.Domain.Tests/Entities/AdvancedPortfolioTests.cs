using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using Portfolio.Domain;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests.Entities;

[TestFixture]
public class PortfolioFullTestSuite
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

        _portfolio = new UserPortfolio();
    }

    [Test]
    public async Task ProcessAsync_Should_HandleSimpleDeposit()
    {
        // Arrange
        var transactionDate = new DateTime(2024, 8, 22);
        var depositAmount = new Money(1m, "BTC");

        var transaction = CryptoCurrencyRawTransaction.CreateDeposit(
            transactionDate,
            depositAmount,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(transaction);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", transactionDate))
            .ReturnsAsync(Result.Success(30000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(1);

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(1m);
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Price fetched on deposit date
    }

    [Test]
    public async Task ProcessAsync_Should_HandleSimpleWithdrawal()
    {
        // Arrange
        var depositDate = new DateTime(2024, 8, 21);
        var withdrawalDate = new DateTime(2024, 8, 22);

        var depositAmount = new Money(1m, "BTC");
        var withdrawalAmount = new Money(0.5m, "BTC");

        var depositTransaction = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDate,
            depositAmount,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var withdrawalTransaction = CryptoCurrencyRawTransaction.CreateWithdraw(
            withdrawalDate,
            withdrawalAmount,
            Money.Empty,
            "withdraw-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransaction);
        wallet.AddTransaction(withdrawalTransaction);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate))
            .ReturnsAsync(Result.Success(30000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(1);

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(0.5m); // 1 BTC - 0.5 BTC withdrawal
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Price remains as the initial deposit
    }

    [Test]
    public async Task ProcessAsync_Should_HandleDepositAndWithdrawInDifferentCurrencies()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var depositDateETH = new DateTime(2024, 8, 21);
        var withdrawalDateBTC = new DateTime(2024, 8, 22);

        var depositAmountBTC = new Money(1m, "BTC");
        var depositAmountETH = new Money(10m, "ETH");
        var withdrawalAmountBTC = new Money(0.5m, "BTC");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var depositTransactionETH = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateETH,
            depositAmountETH,
            Money.Empty,
            "deposit-2",
            [],
            "").Value;

        var withdrawalTransactionBTC = CryptoCurrencyRawTransaction.CreateWithdraw(
            withdrawalDateBTC,
            withdrawalAmountBTC,
            Money.Empty,
            "withdraw-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(depositTransactionETH);
        wallet.AddTransaction(withdrawalTransactionBTC);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", depositDateETH))
            .ReturnsAsync(Result.Success(2000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(0.5m); // 1 BTC - 0.5 BTC withdrawal
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(10m); // 10 ETH deposited
        ethHolding.AverageBoughtPrice.Should().Be(2000m); // Based on deposit price
    }

    [Test]
    public async Task ProcessAsync_Should_HandleTradeBetweenDifferentCryptos()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);

        var depositAmountBTC = new Money(1m, "BTC");
        var tradeBTCtoETH = new Money(0.5m, "BTC");
        var receivedETH = new Money(8m, "ETH");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoETH = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedETH,
            tradeBTCtoETH,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(tradeTransactionBTCtoETH);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
            .ReturnsAsync(Result.Success(30000m));

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
            .ReturnsAsync(Result.Success(2500m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(0.5m); // 1 BTC - 0.5 BTC traded
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(8m); // Received 8 ETH
        ethHolding.AverageBoughtPrice.Should().BeApproximately(1875m, 0.01m); // 0.5 BTC * 30000 USD / 8 ETH = 18750 USD/ETH
    }

    [Test]
    public async Task ProcessAsync_Should_HandleCryptoToFiatTrade()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);

        var depositAmountBTC = new Money(1m, "BTC");
        var tradeBTCtoUSD = new Money(0.5m, "BTC");
        var receivedUSD = new Money(15000m, "USD");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoUSD = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedUSD,
            tradeBTCtoUSD,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(tradeTransactionBTCtoUSD);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and USD holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(0.5m); // 1 BTC - 0.5 BTC traded
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var usdHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "USD").Subject;
        usdHolding.Balance.Should().Be(15000m); // Received 15000 USD
        usdHolding.AverageBoughtPrice.Should().Be(1m); // USD should be valued at 1:1 in the default currency
    }

    [Test]
    public async Task ProcessAsync_Should_HandleFiatToCryptoTrade()
    {
        // Arrange
        var depositDateUSD = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);

        var depositAmountUSD = new Money(30000m, "USD");
        var tradeUSDtoBTC = new Money(15000m, "USD");
        var receivedBTC = new Money(0.5m, "BTC");

        var depositTransactionUSD = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateUSD,
            depositAmountUSD,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionUSDtoBTC = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedBTC,
            tradeUSDtoBTC,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionUSD);
        wallet.AddTransaction(tradeTransactionUSDtoBTC);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
            .ReturnsAsync(Result.Success(30000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and USD holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(0.5m); // Received 0.5 BTC
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on trade price

        var usdHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "USD").Subject;
        usdHolding.Balance.Should().Be(15000m); // 30000 USD - 15000 USD traded
        usdHolding.AverageBoughtPrice.Should().Be(1m); // USD should be valued at 1:1 in the default currency
    }

    [Test]
    public async Task ProcessAsync_Should_HandleMultipleDepositsAndTradesSequentially()
    {
        // Arrange
        var depositDate1 = new DateTime(2024, 8, 21);
        var depositDate2 = new DateTime(2024, 8, 22);
        var tradeDate = new DateTime(2024, 8, 23);

        var depositAmountBTC1 = new Money(1m, "BTC");
        var depositAmountBTC2 = new Money(2m, "BTC");
        var tradeBTCtoETH = new Money(1.5m, "BTC");
        var receivedETH = new Money(10m, "ETH");

        var depositTransactionBTC1 = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDate1,
            depositAmountBTC1,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var depositTransactionBTC2 = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDate2,
            depositAmountBTC2,
            Money.Empty,
            "deposit-2",
            [],
            "").Value;

        var tradeTransactionBTCtoETH = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedETH,
            tradeBTCtoETH,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC1);
        wallet.AddTransaction(depositTransactionBTC2);
        wallet.AddTransaction(tradeTransactionBTCtoETH);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate1))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate2))
            .ReturnsAsync(Result.Success(35000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
            .ReturnsAsync(Result.Success(40000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
            .ReturnsAsync(Result.Success(2500m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(1.5m); // 1 BTC + 2 BTC deposited - 1.5 BTC traded
        btcHolding.AverageBoughtPrice.Should().BeApproximately(33333.33m, 0.01m); // Weighted average of 1 BTC @ 30000 and 2 BTC @ 35000

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(10m); // Received 10 ETH
        ethHolding.AverageBoughtPrice.Should().BeApproximately(6000m, 0.01m); // 1.5 BTC * 40000 USD / 10 ETH

        var taxableEvent = _portfolio.TaxableEvents.Should().ContainSingle(h => h.Currency == "USD").Subject;
        taxableEvent.DisposedAsset.Should().Be("BTC");
        taxableEvent.Quantity.Should().Be(tradeTransactionBTCtoETH.SentAmount.Amount);
        taxableEvent.AverageCost.Should().BeApproximately(33333.33m, 0.01m);
        taxableEvent.ValueAtDisposal.Should().Be(40000m);
    }

    [Test]
    public async Task ProcessAsync_Should_HandleDepositsAndTradesWithPartialWithdrawals()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);
        var withdrawalDate = new DateTime(2024, 8, 23);

        var depositAmountBTC = new Money(3m, "BTC");
        var tradeBTCtoETH = new Money(2m, "BTC");
        var receivedETH = new Money(15m, "ETH");
        var withdrawalAmountETH = new Money(5m, "ETH");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoETH = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedETH,
            tradeBTCtoETH,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var withdrawalTransactionETH = CryptoCurrencyRawTransaction.CreateWithdraw(
            withdrawalDate,
            withdrawalAmountETH,
            Money.Empty,
            "withdraw-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(tradeTransactionBTCtoETH);
        wallet.AddTransaction(withdrawalTransactionETH);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
            .ReturnsAsync(Result.Success(35000m));

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
            .ReturnsAsync(Result.Success(2500m));

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", withdrawalDate))
            .ReturnsAsync(Result.Success(3000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(1m); // 3 BTC deposited - 2 BTC traded
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(10m); // 15 ETH received - 5 ETH withdrawn
        ethHolding.AverageBoughtPrice.Should().BeApproximately(4666.66m, 0.01m); // 2 BTC * 30000 USD / 15 ETH

        depositTransactionBTC.ValueInDefaultCurrency.Amount.Should().Be(depositTransactionBTC.ReceivedAmount.Amount * 30000m);
        depositTransactionBTC.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        depositTransactionBTC.FeeValueInDefaultCurrency.Should().Be(Money.Empty);

        tradeTransactionBTCtoETH.ValueInDefaultCurrency.Amount.Should().Be(tradeTransactionBTCtoETH.SentAmount.Amount * 35000m);
        tradeTransactionBTCtoETH.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        tradeTransactionBTCtoETH.FeeValueInDefaultCurrency.Should().Be(Money.Empty);

        withdrawalTransactionETH.ValueInDefaultCurrency.Amount.Should().Be(withdrawalTransactionETH.SentAmount.Amount * 3000m);
        withdrawalTransactionETH.ValueInDefaultCurrency.CurrencyCode.Should().Be(_portfolio.DefaultCurrency);
        withdrawalTransactionETH.FeeValueInDefaultCurrency.Should().Be(Money.Empty);
    }

    [Test]
    public async Task ProcessAsync_Should_HandleLargeVolumeDepositsAndTrades()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);

        var depositAmountBTC = new Money(10000m, "BTC");
        var tradeBTCtoETH = new Money(5000m, "BTC");
        var receivedETH = new Money(75000m, "ETH");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoETH = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedETH,
            tradeBTCtoETH,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(tradeTransactionBTCtoETH);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
            .ReturnsAsync(Result.Success(2500m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate))
            .ReturnsAsync(Result.Success(30000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(5000m); // 10000 BTC deposited - 5000 BTC traded
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(75000m); // Received 75000 ETH
        ethHolding.AverageBoughtPrice.Should().BeApproximately(2000m, 0.01m); // 5000 BTC * 30000 USD / 75000 ETH
    }

    [Test]
    public async Task ProcessAsync_Should_HandleMultipleSequentialDepositsTradesAndWithdrawals()
    {
        // Arrange
        var depositDate1 = new DateTime(2024, 8, 21);
        var tradeDate1 = new DateTime(2024, 8, 22);
        var withdrawalDate1 = new DateTime(2024, 8, 23);
        var depositDate2 = new DateTime(2024, 8, 24);
        var tradeDate2 = new DateTime(2024, 8, 25);
        var withdrawalDate2 = new DateTime(2024, 8, 26);

        var depositAmountBTC1 = new Money(1m, "BTC");
        var tradeBTCtoETH1 = new Money(0.5m, "BTC");
        var receivedETH1 = new Money(8m, "ETH");
        var withdrawalAmountETH1 = new Money(4m, "ETH");

        var depositAmountBTC2 = new Money(2m, "BTC");
        var tradeBTCtoETH2 = new Money(1m, "BTC");
        var receivedETH2 = new Money(10m, "ETH");
        var withdrawalAmountETH2 = new Money(6m, "ETH");

        var depositTransactionBTC1 = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDate1,
            depositAmountBTC1,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoETH1 = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate1,
            receivedETH1,
            tradeBTCtoETH1,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var withdrawalTransactionETH1 = CryptoCurrencyRawTransaction.CreateWithdraw(
            withdrawalDate1,
            withdrawalAmountETH1,
            Money.Empty,
            "withdraw-1",
            [],
            "").Value;

        var depositTransactionBTC2 = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDate2,
            depositAmountBTC2,
            Money.Empty,
            "deposit-2",
            [],
            "").Value;

        var tradeTransactionBTCtoETH2 = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate2,
            receivedETH2,
            tradeBTCtoETH2,
            Money.Empty,
            "trade-2",
            [],
            "").Value;

        var withdrawalTransactionETH2 = CryptoCurrencyRawTransaction.CreateWithdraw(
            withdrawalDate2,
            withdrawalAmountETH2,
            Money.Empty,
            "withdraw-2",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC1);
        wallet.AddTransaction(tradeTransactionBTCtoETH1);
        wallet.AddTransaction(withdrawalTransactionETH1);
        wallet.AddTransaction(depositTransactionBTC2);
        wallet.AddTransaction(tradeTransactionBTCtoETH2);
        wallet.AddTransaction(withdrawalTransactionETH2);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate1))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate1))
            .ReturnsAsync(Result.Success(2500m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate1))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDate2))
            .ReturnsAsync(Result.Success(35000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate2))
            .ReturnsAsync(Result.Success(2700m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", tradeDate2))
            .ReturnsAsync(Result.Success(35000m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(1.5m); // 1 BTC + 2 BTC deposited - 0.5 BTC traded - 1 BTC traded
        btcHolding.AverageBoughtPrice.Should().BeApproximately(34000m, 0.01m); // Weighted average

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(8m); // 8 ETH + 10 ETH received - 4 ETH withdrawn - 6 ETH withdrawn
        ethHolding.AverageBoughtPrice.Should().BeApproximately(3035.714m, 0.01m); // Adjusted based on trades
    }

    [Test]
    public async Task ProcessAsync_ShouldHandleNegativeBalanceWhenSendingMoreThanOwned()
    {
        // Arrange
        var depositDateBTC = new DateTime(2024, 8, 21);
        var tradeDate = new DateTime(2024, 8, 22);

        var depositAmountBTC = new Money(1m, "BTC");
        var tradeBTCtoETH = new Money(2m, "BTC"); // More BTC than available in balance
        var receivedETH = new Money(16m, "ETH");

        var depositTransactionBTC = CryptoCurrencyRawTransaction.CreateDeposit(
            depositDateBTC,
            depositAmountBTC,
            Money.Empty,
            "deposit-1",
            [],
            "").Value;

        var tradeTransactionBTCtoETH = CryptoCurrencyRawTransaction.CreateTrade(
            tradeDate,
            receivedETH,
            tradeBTCtoETH,
            Money.Empty,
            "trade-1",
            [],
            "").Value;

        var wallet = Wallet.Create("Test Wallet").Value;
        wallet.AddTransaction(depositTransactionBTC);
        wallet.AddTransaction(tradeTransactionBTCtoETH);

        _portfolio.AddWallet(wallet);

        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("BTC", depositDateBTC))
            .ReturnsAsync(Result.Success(30000m));
        _priceHistoryServiceMock
            .Setup(p => p.GetPriceAtCloseTimeAsync("ETH", tradeDate))
            .ReturnsAsync(Result.Success(2500m));

        // Act
        var result = await _portfolio.CalculateTradesAsync(_priceHistoryServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _portfolio.Holdings.Should().HaveCount(2); // BTC and ETH holdings

        var btcHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "BTC").Subject;
        btcHolding.Balance.Should().Be(-1m); // 1 BTC deposited - 2 BTC traded
        btcHolding.AverageBoughtPrice.Should().Be(30000m); // Based on deposit price

        var ethHolding = _portfolio.Holdings.Should().ContainSingle(h => h.Asset == "ETH").Subject;
        ethHolding.Balance.Should().Be(16m); // Received 16 ETH
        ethHolding.AverageBoughtPrice.Should().BeApproximately(3750m, 0.01m); // 2 BTC * 30000 USD / 16 ETH

        tradeTransactionBTCtoETH.ErrorType.Should().Be(ErrorType.InsufficientFunds);
    }
}
