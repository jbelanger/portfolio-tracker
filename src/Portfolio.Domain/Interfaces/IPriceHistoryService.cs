using CSharpFunctionalExtensions;

namespace Portfolio.Domain.Interfaces
{
    /// <summary>
    /// Interface for a service that provides historical price data for currencies or coins.
    /// </summary>
    public interface IPriceHistoryService
    {
        /// <summary>
        /// Retrieves the price of a specific symbol at a specified close time.
        /// </summary>
        /// <param name="symbol">The symbol of the currency or coin.</param>
        /// <param name="date">The date and time at which to retrieve the closing price.</param>
        /// <returns>A <see cref="Result{T}"/> containing the closing price or an error message.</returns>
        Task<Result<decimal>> GetPriceAtCloseTimeAsync(string symbol, DateTime date);

        Task<Result<Dictionary<string, decimal>>> GetCurrentPricesAsync(IEnumerable<string> symbols);
    }
}
