using CSharpFunctionalExtensions;
using System;
using System.Threading.Tasks;

namespace Portfolio.App
{
    public class CoinGeckoPriceHistoryStoreFactory : IPriceHistoryStoreFactory
    {
        public async Task<Result<IPriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate)
        {
            var createResult = await CoinGeckoPriceHistoryStore.Create(symbolFrom, symbolTo, startDate, endDate);
            if (createResult.IsFailure)
                return Result.Failure<IPriceHistoryStore>(createResult.Error);
            return createResult.Value;
        }
    }
}
