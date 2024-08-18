using CSharpFunctionalExtensions;

namespace Portfolio.App;

public interface IPriceHistoryService
{
    Task<Result<CryptoPriceData>> GetPriceDataAsync(DateTime date);
}
