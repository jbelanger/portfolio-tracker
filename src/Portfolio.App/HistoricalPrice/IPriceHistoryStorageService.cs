namespace Portfolio.App.HistoricalPrice
{
    /// <summary>
    /// Interface for a service that handles storage and retrieval of historical price data.
    /// </summary>
    public interface IPriceHistoryStorageService
    {
        /// <summary>
        /// Loads historical price data for a specified symbol from storage.
        /// </summary>
        /// <param name="symbol">The symbol of the currency or coin.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="CryptoPriceRecord"/> or an error message.</returns>
        Task<Result<IEnumerable<CryptoPriceRecord>>> LoadHistoryAsync(string symbol);

        /// <summary>
        /// Saves historical price data for a specified symbol to storage.
        /// </summary>
        /// <param name="symbol">The symbol of the currency or coin.</param>
        /// <param name="priceHistory">The historical price data to be saved.</param>
        /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
        Task<Result> SaveHistoryAsync(string symbol, IEnumerable<CryptoPriceRecord> priceHistory);
    }
}
