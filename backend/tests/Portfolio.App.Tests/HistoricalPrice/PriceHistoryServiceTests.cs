using Moq;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.App.HistoricalPrice;
using Microsoft.Extensions.Caching.Memory;
using Portfolio.Domain.Constants;
using FluentAssertions;
using CSharpFunctionalExtensions;

namespace Portfolio.App.Tests;

[TestFixture]
public class PriceHistoryServiceTests
{
    private Mock<IPriceHistoryApi> _priceHistoryApiMock;
    private Mock<IPriceHistoryStorageService> _priceHistoryStorageMock;
    private MemoryCache _cache;
    private PriceHistoryService _priceHistoryService;

    [SetUp]
    public void SetUp()
    {
        _priceHistoryApiMock = new Mock<IPriceHistoryApi>();
        _priceHistoryStorageMock = new Mock<IPriceHistoryStorageService>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _priceHistoryService = new PriceHistoryService(
            _priceHistoryApiMock.Object,
            _priceHistoryStorageMock.Object,
            _cache,
            Strings.CURRENCY_USD);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task GetPriceAtCloseTimeAsync_WhenSymbolIsDefaultCurrency_ReturnsError()
    {
        // Arrange
        var symbol = Strings.CURRENCY_USD;
        var date = DateTime.Today;

        // Act
        var result = await _priceHistoryService.GetPriceAtCloseTimeAsync(symbol, date);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(string.Format(Errors.ERR_SAME_SYMBOLS, symbol, Strings.CURRENCY_USD));
    }

    [Test]
    public async Task GetPriceAtCloseTimeAsync_WhenPriceIsFoundInStorage_ReturnsPrice()
    {
        // Arrange
        var symbol = "BTC";
        var date = DateTime.Today;
        var expectedPrice = 50000m;

        var priceRecord = new PriceRecord
        {
            CurrencyPair = symbol,
            CloseDate = date,
            ClosePrice = expectedPrice
        };

        _priceHistoryStorageMock.Setup(s => s.GetPriceAsync(symbol, date))
            .ReturnsAsync(Result.Success(priceRecord));

        // Act
        var result = await _priceHistoryService.GetPriceAtCloseTimeAsync(symbol, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedPrice);
    }

    [Test]
    public async Task GetPriceAtCloseTimeAsync_WhenPriceIsNotFoundAndApiFetchFails_ReturnsError()
    {
        // Arrange
        var symbol = "BTC";
        var date = DateTime.Today;

        _priceHistoryStorageMock.Setup(s => s.GetPriceAsync(symbol, date))
            .ReturnsAsync(Result.Failure<PriceRecord>("Price not found"));

        _priceHistoryApiMock.Setup(a => a.FetchPriceHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result.Failure<IEnumerable<PriceRecord>>("API error"));

        // Act
        var result = await _priceHistoryService.GetPriceAtCloseTimeAsync(symbol, date);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error retrieving or saving price data.");
    }

    [Test]
    public async Task GetPriceAtCloseTimeAsync_WhenPriceIsNotFoundInStorageButApiFetchSucceeds_ReturnsFetchedPrice()
    {
        // Arrange
        var symbol = "BTC";
        var date = DateTime.Today;
        var expectedPrice = 50000m;

        var priceRecord = new PriceRecord
        {
            CurrencyPair = symbol,
            CloseDate = date,
            ClosePrice = expectedPrice
        };

        _priceHistoryStorageMock.Setup(s => s.GetPriceAsync(symbol, date))
            .ReturnsAsync(Result.Failure<PriceRecord>("Price not found"));

        _priceHistoryApiMock.Setup(a => a.FetchPriceHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result.Success(new List<PriceRecord> { priceRecord }.AsEnumerable()));

        _priceHistoryStorageMock.Setup(s => s.SaveHistoryAsync(symbol, It.IsAny<IEnumerable<PriceRecord>>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _priceHistoryService.GetPriceAtCloseTimeAsync(symbol, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedPrice);
    }

    [Test]
    public async Task HandleMissingFiatDataAsync_WhenPriceIsFoundOnPreviousDay_ReturnsPrice()
    {
        // Arrange
        var symbol = "CAD";
        var date = DateTime.Today;
        var previousDate = date.AddDays(-1);
        var expectedPrice = 1.0m;

        var priceRecord = new PriceRecord
        {
            CurrencyPair = symbol,
            CloseDate = previousDate,
            ClosePrice = expectedPrice
        };

        _priceHistoryStorageMock.SetupSequence(s => s.GetPriceAsync(symbol, It.IsAny<DateTime>()))
            .ReturnsAsync(Result.Failure<PriceRecord>("Price not found"))
            .ReturnsAsync(Result.Success(priceRecord));

        // Act
        var result = await _priceHistoryService.GetPriceAtCloseTimeAsync(symbol, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedPrice);
    }

    [Test]
    public async Task GetCurrentPricesAsync_WhenCached_ReturnsCachedPrices()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };
        var cacheKey = $"CurrentPrices_{string.Join("_", symbols.OrderBy(s => s))}";

        var expectedPrices = new Dictionary<string, decimal>
        {
            { "BTC", 50000m },
            { "ETH", 3000m }
        };

        _cache.Set(cacheKey, expectedPrices);

        // Act
        var result = await _priceHistoryService.GetCurrentPricesAsync(symbols);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(expectedPrices);
    }

    [Test]
    public async Task GetCurrentPricesAsync_WhenApiFetchesPartialData_ReturnsPartialData()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH", "LTC" };
        var fetchedPrices = new List<PriceRecord>
        {
            new PriceRecord { CurrencyPair = "BTC-USD", CloseDate = DateTime.UtcNow, ClosePrice = 50000m },
            new PriceRecord { CurrencyPair = "ETH-USD", CloseDate = DateTime.UtcNow, ClosePrice = 3000m }
        };

        _priceHistoryApiMock.Setup(a => a.FetchCurrentPriceAsync(symbols, Strings.CURRENCY_USD))
            .ReturnsAsync(Result.Success(fetchedPrices.AsEnumerable()));

        // Act
        var result = await _priceHistoryService.GetCurrentPricesAsync(symbols);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(2); // Only 2 out of 3 symbols are returned
        result.Value.Should().ContainKey("BTC").WhoseValue.Should().Be(50000m);
        result.Value.Should().ContainKey("ETH").WhoseValue.Should().Be(3000m);
        result.Value.Should().NotContainKey("LTC");
    }

    [Test]
    public async Task GetCurrentPricesAsync_WhenCalledConcurrently_ReturnsCachedPrices()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };
        var cacheKey = $"CurrentPrices_{string.Join("_", symbols.OrderBy(s => s))}";

        var expectedPrices = new Dictionary<string, decimal>
        {
            { "BTC", 50000m },
            { "ETH", 3000m }
        };

        _priceHistoryApiMock.Setup(a => a.FetchCurrentPriceAsync(symbols, Strings.CURRENCY_USD))
            .ReturnsAsync(Result.Success(new List<PriceRecord>
            {
                new PriceRecord { CurrencyPair = "BTC-USD", CloseDate = DateTime.UtcNow, ClosePrice = 50000m },
                new PriceRecord { CurrencyPair = "ETH-USD", CloseDate = DateTime.UtcNow, ClosePrice = 3000m }
            }.AsEnumerable()));

        // Act
        var tasks = new[]
        {
            _priceHistoryService.GetCurrentPricesAsync(symbols),
            _priceHistoryService.GetCurrentPricesAsync(symbols),
            _priceHistoryService.GetCurrentPricesAsync(symbols)
        };

        var results = await Task.WhenAll(tasks);

        // Assert
        foreach (var result in results)
        {
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Equal(expectedPrices);
        }
    }

    [Test]
    public async Task GetCurrentPricesAsync_WhenApiReturnsEmptyResponse_ReturnsSuccess()
    {
        // Arrange
        var symbols = new[] { "BTC", "ETH" };

        _priceHistoryApiMock.Setup(a => a.FetchCurrentPriceAsync(symbols, Strings.CURRENCY_USD))
            .ReturnsAsync(Result.Success(Enumerable.Empty<PriceRecord>()));

        // Act
        var result = await _priceHistoryService.GetCurrentPricesAsync(symbols);

        // Assert
        result.IsSuccess.Should().BeTrue();        
    }
}
