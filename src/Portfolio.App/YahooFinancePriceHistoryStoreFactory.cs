using CSharpFunctionalExtensions;

namespace Portfolio.App;

public class YahooFinancePriceHistoryStoreFactory : IPriceHistoryStoreFactory
{
    public async Task<Result<IPriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {        
        var createResult = await YahooFinancePriceHistoryStore.Create(symbolFrom, symbolTo, startDate, endDate);
        if(createResult.IsFailure)
            return Result.Failure<IPriceHistoryStore>(createResult.Error);
        return createResult.Value;
    }
}
