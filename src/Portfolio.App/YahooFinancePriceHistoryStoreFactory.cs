using CSharpFunctionalExtensions;

namespace Portfolio.App;

public class YahooFinancePriceHistoryStoreFactory : IPriceHistoryServiceFactory
{
    private const string ERR_SAME_SYMBOLS = "Symbols must be of different currency/coin ({0}-{1}).";



    public async Task<Result<IPriceHistoryService>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
    {
        if (symbolFrom == symbolTo)
            return Result.Failure<IPriceHistoryService>(string.Format(ERR_SAME_SYMBOLS, symbolFrom, symbolTo));

        var createResult = await YahooFinancePriceHistoryService.Create(symbolFrom, startDate, endDate, new YahooFinancePriceHistoryApi(symbolTo));
        if (createResult.IsFailure)
            return Result.Failure<IPriceHistoryService>(createResult.Error);
        return createResult.Value;
    }
}
