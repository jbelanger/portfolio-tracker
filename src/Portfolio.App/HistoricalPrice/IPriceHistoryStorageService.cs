using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice;

public interface IPriceHistoryStorageService
{
    Task<Result<IEnumerable<CryptoPriceRecord>>> LoadHistoryAsync(string symbol);
    Task<Result> SaveHistoryAsync(string symbol, IEnumerable<CryptoPriceRecord> priceHistory);
}
