using CSharpFunctionalExtensions;

namespace Portfolio.App;

public interface IPriceHistoryStore
{
    Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date);
}
