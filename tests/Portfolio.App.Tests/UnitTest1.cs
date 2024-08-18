using Portfolio.Shared;
using FluentAssertions;
using CSharpFunctionalExtensions;

namespace Portfolio.App.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Withdraw_WhenAllWithdrawn_BalancesAreZero()
    {
        var deposit = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-01-01"),
            new Money(985, "USD"),
            new Money(15, "USD"),
            "test-account",
            ["0"]).Value;

        var trade = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(0.04m, "BTC"),
            new Money(975, "USD"),
            new Money(10, "USD"),
            "test-account",
            ["1"]).Value;

        var withdraw = CryptoCurrencyWithdrawTransaction.Create(
            DateTime.Parse("2023-06-01"),
            new Money(0.03999m, "BTC"),
            new Money(0.00001m, "BTC"),
            "test-account",
            ["2"]).Value;

        var testWalletResult = Wallet.Create("test-account", [deposit, trade, withdraw]);
        if (testWalletResult.IsFailure)
            throw new Exception(testWalletResult.Error);

        var portfolio = new Portfolio(new MockPriceHistoryStoreFactory());

        var addWalletResult = portfolio.AddWallet(testWalletResult.Value);
        if (addWalletResult.IsFailure)
            throw new Exception(addWalletResult.Error);

        var processResult = await portfolio.Process();
        if (processResult.IsFailure)
            throw new Exception(processResult.Error);

        portfolio.Holdings.Should().HaveCount(2);
        var btcHolding = portfolio.Holdings.First(h => h.Asset == "BTC");
        btcHolding.Balance.Should().Be(0);
        btcHolding.AverageBoughtPrice.Should().Be(0);

        var usdHolding = portfolio.Holdings.First(h => h.Asset == "USD");
        usdHolding.Balance.Should().Be(0);
    }

    [Test]
    public async Task Withdraw_WhenHalfWithdrawn_BalancesAreUpdated()
    {
        var deposit = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-01-01"),
            new Money(985, "USD"),
            new Money(15, "USD"),
            "test-account",
            ["0"]).Value;

        var trade = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(0.04m, "BTC"),
            new Money(975, "USD"),
            new Money(10, "USD"),
            "test-account",
            ["1"]).Value;

        var withdraw = CryptoCurrencyWithdrawTransaction.Create(
            DateTime.Parse("2023-06-01"),
            new Money(0.01999m, "BTC"),
            new Money(0.00001m, "BTC"),
            "test-account",
            ["2"]).Value;

        var testWalletResult = Wallet.Create("test-account", [deposit, trade, withdraw]);
        if (testWalletResult.IsFailure)
            throw new Exception(testWalletResult.Error);

        var portfolio = new Portfolio(new MockPriceHistoryStoreFactory());

        var addWalletResult = portfolio.AddWallet(testWalletResult.Value);
        if (addWalletResult.IsFailure)
            throw new Exception(addWalletResult.Error);

        var processResult = await portfolio.Process();
        if (processResult.IsFailure)
            throw new Exception(processResult.Error);

        portfolio.Holdings.Should().HaveCount(2);
        var btcHolding = portfolio.Holdings.First(h => h.Asset == "BTC");
        btcHolding.Balance.Should().Be(0.02m);
        btcHolding.AverageBoughtPrice.Should().Be(975 / 0.04m);

        var usdHolding = portfolio.Holdings.First(h => h.Asset == "USD");
        usdHolding.Balance.Should().Be(0);
    }

    [Test]
    public async Task Deposit_AfterFullWithdrawn_AvgCostIsRenewed()
    {
        var deposit = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-01-01"),
            new Money(985, "USD"),
            new Money(15, "USD"),
            "test-account",
            ["0"]).Value;

        var trade = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(0.04m, "BTC"),
            new Money(975, "USD"),
            new Money(10, "USD"),
            "test-account",
            ["1"]).Value;

        var withdraw = CryptoCurrencyWithdrawTransaction.Create(
            DateTime.Parse("2023-06-01"),
            new Money(0.03999m, "BTC"),
            new Money(0.00001m, "BTC"),
            "test-account",
            ["2"]).Value;

        var deposit2 = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-07-01"),
            new Money(0.01999m, "BTC"),
            new Money(0.00001m, "BTC"),
            "test-account",
            ["3"]).Value;

        var testWalletResult = Wallet.Create("test-account", [deposit, trade, withdraw, deposit2]);
        if (testWalletResult.IsFailure)
            throw new Exception(testWalletResult.Error);

        var portfolio = new Portfolio(new MockPriceHistoryStoreFactory());

        var addWalletResult = portfolio.AddWallet(testWalletResult.Value);
        if (addWalletResult.IsFailure)
            throw new Exception(addWalletResult.Error);

        var processResult = await portfolio.Process();
        if (processResult.IsFailure)
            throw new Exception(processResult.Error);

        portfolio.Holdings.Should().HaveCount(2);
        var btcHolding = portfolio.Holdings.First(h => h.Asset == "BTC");
        btcHolding.Balance.Should().Be(0.01999m);
        btcHolding.AverageBoughtPrice.Should().Be(50000);

        var usdHolding = portfolio.Holdings.First(h => h.Asset == "USD");
        usdHolding.Balance.Should().Be(0);
    }

    [Test]
    public async Task Deposit_AfterHalfWithdrawn_AvgCostIsUpdated()
    {
        var deposit = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-01-01"),
            new Money(1000, "USD"),
            new Money(0, "USD"),
            "test-account",
            ["0"]).Value;

        var trade = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(0.04m, "BTC"),
            new Money(1000, "USD"),
            new Money(0, "USD"),
            "test-account",
            ["1"]).Value;

        var withdraw = CryptoCurrencyWithdrawTransaction.Create(
            DateTime.Parse("2023-06-01"),
            new Money(0.03m, "BTC"),
            new Money(0m, "BTC"),
            "test-account",
            ["2"]).Value;

        var deposit2 = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-07-01"),
            new Money(0.03m, "BTC"),
            new Money(0m, "BTC"),
            "test-account",
            ["3"]).Value;

        var testWalletResult = Wallet.Create("test-account", [deposit, trade, withdraw, deposit2]);
        if (testWalletResult.IsFailure)
            throw new Exception(testWalletResult.Error);

        var portfolio = new Portfolio(new MockPriceHistoryStoreFactory());

        var addWalletResult = portfolio.AddWallet(testWalletResult.Value);
        if (addWalletResult.IsFailure)
            throw new Exception(addWalletResult.Error);

        var processResult = await portfolio.Process();
        if (processResult.IsFailure)
            throw new Exception(processResult.Error);

        portfolio.Holdings.Should().HaveCount(2);
        var btcHolding = portfolio.Holdings.First(h => h.Asset == "BTC");
        btcHolding.Balance.Should().Be(0.04m);
        btcHolding.AverageBoughtPrice.Should().Be(43750);

        var usdHolding = portfolio.Holdings.First(h => h.Asset == "USD");
        usdHolding.Balance.Should().Be(0);
    }

    [Test]
    public async Task Trade_WhenCryptoToCryptoTrading_AvgCostIsUpdated()
    {
        var deposit = CryptoCurrencyDepositTransaction.Create(
            DateTime.Parse("2023-01-01"),
            new Money(1000, "USD"),
            new Money(0, "USD"),
            "test-account",
            ["0"]).Value;

        var trade = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(0.04m, "BTC"),
            new Money(1000, "USD"),
            new Money(0, "USD"),
            "test-account",
            ["1"]).Value;

        var trade2 = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(6250, "ADA"),
            new Money(0.02m, "BTC"),
            new Money(0, "BTC"),
            "test-account",
            ["1"]).Value;

        var trade3 = CryptoCurrencyTradeTransaction.Create(
            DateTime.Parse("2023-03-01"),
            new Money(6250, "ADA"),
            new Money(0.01m, "BTC"),
            new Money(0, "BTC"),
            "test-account",
            ["1"]).Value;


        var testWalletResult = Wallet.Create("test-account", [deposit, trade, trade2, trade3]);
        if (testWalletResult.IsFailure)
            throw new Exception(testWalletResult.Error);

        var portfolio = new Portfolio(new MockPriceHistoryStoreFactory());

        var addWalletResult = portfolio.AddWallet(testWalletResult.Value);
        if (addWalletResult.IsFailure)
            throw new Exception(addWalletResult.Error);

        var processResult = await portfolio.Process();
        if (processResult.IsFailure)
            throw new Exception(processResult.Error);

        portfolio.Holdings.Should().HaveCount(3);
        var btcHolding = portfolio.Holdings.First(h => h.Asset == "BTC");
        btcHolding.Balance.Should().Be(0.01m);
        btcHolding.AverageBoughtPrice.Should().Be(25000m);

        var adaHolding = portfolio.Holdings.First(h => h.Asset == "ADA");
        adaHolding.Balance.Should().Be(12500);
        adaHolding.AverageBoughtPrice.Should().Be(0.06m);

        var usdHolding = portfolio.Holdings.First(h => h.Asset == "USD");
        usdHolding.Balance.Should().Be(0);
    }

    public class MockPriceHistoryStore : IPriceHistoryService
    {
        public Task<CryptoPriceData> GetPriceDataAsync(DateTime date)
        {
            if (date.ToString("yyyy-MM-dd") == "2023-07-01")
            {
                return Task.FromResult(new CryptoPriceData { Close = 50000 });
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public class MockPriceHistoryStoreFactory : IPriceHistoryStoreFactory
    {
        public Task<Result<IPriceHistoryService>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
        {
            return Task.FromResult(Result.Success<IPriceHistoryService>(new MockPriceHistoryStore()));
        }
    }

}