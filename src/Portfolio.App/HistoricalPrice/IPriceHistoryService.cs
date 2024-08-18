using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice;

public interface IPriceHistoryService
{
    public string DefaultCurrency { get; set; }
    Task<Result<decimal>> GetPriceAtCloseTimeAsync(string symbol, DateTime date);
}
