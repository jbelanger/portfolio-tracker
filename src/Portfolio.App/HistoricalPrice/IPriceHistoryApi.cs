using CSharpFunctionalExtensions;

namespace Portfolio.App.HistoricalPrice
{
    public interface IPriceHistoryApi
    {
        public string DetermineTradingPair(string fromSymbol, string toSymbol);
        public Task<Result<IEnumerable<CryptoPriceRecord>>> FetchDataAsync(string symbol, DateTime startDate, DateTime endDate);
    }
}
