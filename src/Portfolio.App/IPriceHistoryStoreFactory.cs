using CSharpFunctionalExtensions;

namespace Portfolio.App;

public interface IPriceHistoryStoreFactory
{
    Task<Result<IPriceHistoryStore>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate);
}
