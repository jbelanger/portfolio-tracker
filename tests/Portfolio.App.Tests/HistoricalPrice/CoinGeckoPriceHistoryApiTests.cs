using NUnit.Framework;
using Portfolio.App.HistoricalPrice.CoinGecko;
using Portfolio.Domain.ValueObjects;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;

namespace Portfolio.App.Tests;

[TestFixture]
public class CoinGeckoPriceHistoryApiTests
{
    private MockHttpMessageHandler _mockHttp;
    private HttpClient _httpClient;
    private CoinGeckoPriceHistoryApi _coinGeckoApi;

    [SetUp]
    public async Task SetUp()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttp);
        _coinGeckoApi = new CoinGeckoPriceHistoryApi(_httpClient);

        // Mock response for GetAllSupportedCoinIdsAsync
        var jsonResponse = @"
        [
            { 'id': 'bitcoin', 'symbol': 'btc', 'name': 'Bitcoin' },
            { 'id': 'ethereum', 'symbol': 'eth', 'name': 'Ethereum' }
        ]";

        _mockHttp.When("https://api.coingecko.com/api/v3/coins/list")
                 .Respond("application/json", jsonResponse);

        // Populate the internal cache by calling GetAllSupportedCoinIdsAsync
        await _coinGeckoApi.GetAllSupportedCoinIdsAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _mockHttp.Dispose();
    }

    [Test]
    public async Task FetchPriceHistoryAsync_WhenSymbolIsValid_ReturnsPriceRecords()
    {
        // Arrange
        var symbol = "BTC";
        var currency = "USD";
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;

        var jsonResponse = @"
        {
            'prices': [
                [1640995200000, 46204.98],
                [1641081600000, 46495.22]
            ]
        }";

        _mockHttp.When($"https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency={currency.ToLower()}&from=...")
                 .Respond("application/json", jsonResponse);

        // Act
        var result = await _coinGeckoApi.FetchPriceHistoryAsync(symbol, currency, startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(x => x.CurrencyPair == $"{symbol}/{currency}" && x.ClosePrice == 46204.98m);
        result.Value.Should().Contain(x => x.CurrencyPair == $"{symbol}/{currency}" && x.ClosePrice == 46495.22m);
    }

    [Test]
    public async Task FetchPriceHistoryAsync_WhenSymbolIsInvalid_ReturnsFailure()
    {
        // Arrange
        var symbol = "INVALID";
        var currency = "USD";
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _coinGeckoApi.FetchPriceHistoryAsync(symbol, currency, startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Coin ID for symbol INVALID could not be found.");
    }

    [Test]
    public async Task FetchPriceHistoryAsync_WhenHttpClientReturnsError_ReturnsFailure()
    {
        // Arrange
        var symbol = "BTC";
        var currency = "USD";
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;

        _mockHttp.When($"https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency={currency.ToLower()}&from=...")
                 .Respond(HttpStatusCode.BadRequest);

        // Act
        var result = await _coinGeckoApi.FetchPriceHistoryAsync(symbol, currency, startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to fetch data");
    }

    [Test]
    public async Task FetchCurrentPriceAsync_WhenSymbolIsValid_ReturnsCurrentPrice()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };
        var currency = "USD";

        var jsonResponse = @"
        {
            'bitcoin': {'usd': 46204.98},
            'ethereum': {'usd': 3504.88}
        }";

        _mockHttp.When("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd")
                 .Respond("application/json", jsonResponse);

        // Act
        var result = await _coinGeckoApi.FetchCurrentPriceAsync(symbols, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(x => x.CurrencyPair == "BTC/USD" && x.ClosePrice == 46204.98m);
        result.Value.Should().Contain(x => x.CurrencyPair == "ETH/USD" && x.ClosePrice == 3504.88m);
    }

    [Test]
    public async Task FetchCurrentPriceAsync_WhenSymbolIsInvalid_ReturnsPartialOrNoData()
    {
        // Arrange
        var symbols = new[] { "BTC", "INVALID" };
        var currency = "USD";

        var jsonResponse = @"
        {
            'bitcoin': {'usd': 46204.98}
        }";

        _mockHttp.When("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd")
                 .Respond("application/json", jsonResponse);

        // Act
        var result = await _coinGeckoApi.FetchCurrentPriceAsync(symbols, currency);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().Contain(x => x.CurrencyPair == "BTC/USD" && x.ClosePrice == 46204.98m);
        result.Value.Should().NotContain(x => x.CurrencyPair == "INVALID/USD");
    }

    [Test]
    public async Task FetchCurrentPriceAsync_WhenHttpClientReturnsError_ReturnsFailure()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };
        var currency = "USD";

        _mockHttp.When("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd")
                 .Respond(HttpStatusCode.BadRequest);

        // Act
        var result = await _coinGeckoApi.FetchCurrentPriceAsync(symbols, currency);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to fetch data");
    }

    [Test]
    public async Task GetCoinIdAsync_WhenCoinIdIsCached_ReturnsCachedCoinId()
    {
        // Arrange
        var symbol = "BTC";
        var expectedCoinId = "bitcoin";

        // Act
        var result = await _coinGeckoApi.GetCoinIdAsync(symbol);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedCoinId);
    }

    [Test]
    public async Task GetCoinIdAsync_WhenCoinIdIsNotCached_ReturnsFailure()
    {
        // Arrange
        var symbol = "INVALID";

        // Act
        var result = await _coinGeckoApi.GetCoinIdAsync(symbol);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Coin ID for symbol INVALID could not be found.");
    }

    [Test]
    public async Task FetchPriceHistoryAsync_RespectsRateLimitBetweenRequests()
    {
        // Arrange
        var symbol = "BTC";
        var currency = "USD";
        var startDate = DateTime.UtcNow.AddDays(-5);
        var endDate = DateTime.UtcNow;

        var jsonResponse = @"
        {
            'prices': [
                [1640995200000, 46204.98],
                [1641081600000, 46495.22]
            ]
        }";

        _mockHttp.When($"https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency={currency.ToLower()}&from=...")
                 .Respond("application/json", jsonResponse);

        // Act
        var firstCall = await _coinGeckoApi.FetchPriceHistoryAsync(symbol, currency, startDate, endDate);
        var secondCallStart = DateTime.UtcNow;
        var secondCall = await _coinGeckoApi.FetchPriceHistoryAsync(symbol, currency, startDate, endDate);
        var secondCallEnd = DateTime.UtcNow;

        // Assert
        firstCall.IsSuccess.Should().BeTrue();
        secondCall.IsSuccess.Should().BeTrue();

        // Ensure that the delay was applied between the two calls
        var timeBetweenCalls = secondCallEnd - secondCallStart;
        timeBetweenCalls.Should().BeGreaterOrEqualTo(TimeSpan.FromMinutes(1.0 / 15)); // 15 requests per minute
    }

    [Test]
    public async Task FetchCurrentPriceAsync_RespectsRateLimitBetweenRequests()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };
        var currency = "USD";

        var jsonResponse = @"
        {
            'bitcoin': {'usd': 46204.98},
            'ethereum': {'usd': 3504.88}
        }";

        _mockHttp.When("https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum&vs_currencies=usd")
                 .Respond("application/json", jsonResponse);

        // Act
        var firstCall = await _coinGeckoApi.FetchCurrentPriceAsync(symbols, currency);
        var secondCallStart = DateTime.UtcNow;
        var secondCall = await _coinGeckoApi.FetchCurrentPriceAsync(symbols, currency);
        var secondCallEnd = DateTime.UtcNow;

        // Assert
        firstCall.IsSuccess.Should().BeTrue();
        secondCall.IsSuccess.Should().BeTrue();

        // Ensure that the delay was applied between the two calls
        var timeBetweenCalls = secondCallEnd - secondCallStart;
        timeBetweenCalls.Should().BeGreaterOrEqualTo(TimeSpan.FromMinutes(1.0 / 15)); // 15 requests per minute
    }
}
