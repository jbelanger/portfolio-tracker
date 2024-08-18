using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice;

public interface IPriceHistoryServiceFactory
{
    Task<Result<IPriceHistoryService>> Create(string symbolFrom, string symbolTo, DateTime startDate, DateTime endDate);
}
