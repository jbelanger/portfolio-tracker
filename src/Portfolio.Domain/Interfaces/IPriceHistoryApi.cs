using CSharpFunctionalExtensions;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces
{
    /// <summary>
    /// Interface for fetching historical price data from a specific API.
    /// </summary>
    public interface IPriceHistoryApi
    {
        /// <summary>
        /// Determines the appropriate trading pair symbol based on the provided symbols.
        /// </summary>
        /// <param name="fromSymbol">The base currency or coin symbol.</param>
        /// <param name="toSymbol">The quote currency or coin symbol.</param>
        /// <returns>The trading pair symbol in the appropriate format.</returns>
        string DetermineTradingPair(string fromSymbol, string toSymbol);

        /// <summary>
        /// Fetches historical price data for a given symbol and date range.
        /// </summary>
        /// <param name="symbol">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbol, DateTime startDate, DateTime endDate);
    }
}
