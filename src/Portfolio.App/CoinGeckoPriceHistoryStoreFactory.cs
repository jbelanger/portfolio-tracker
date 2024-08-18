using CSharpFunctionalExtensions;
using System;
using System.Threading.Tasks;

namespace Portfolio.App
{
    public class CoinGeckoPriceHistoryStoreFactory : IPriceHistoryServiceFactory
    {
        public async Task<Result<IPriceHistoryService>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
        {
            var createResult = await CoinGeckoPriceHistoryStore.Create(symbolFrom, symbolTo, startDate, endDate);
            if (createResult.IsFailure)
                return Result.Failure<IPriceHistoryService>(createResult.Error);
            return createResult.Value;
        }
    }
}
