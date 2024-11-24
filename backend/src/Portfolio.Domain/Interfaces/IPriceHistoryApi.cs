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
        /// Fetches historical price data for a given symbol and date range.
        /// </summary>
        /// <param name="symbol">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbolPair, string currency, DateTime startDate, DateTime endDate);

        Task<Result<IEnumerable<PriceRecord>>> FetchCurrentPriceAsync(IEnumerable<string> symbols, string currency);
    }
}
