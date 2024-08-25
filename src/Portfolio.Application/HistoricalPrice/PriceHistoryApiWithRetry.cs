using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.HistoricalPrice.YahooFinance
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryApi"/> that retrieves historical cryptocurrency price data from the Yahoo Finance API.
    /// </summary>
    public class PriceHistoryApiWithRetry : IPriceHistoryApi
    {
        private readonly int _numberOfAttemps;
        private readonly IPriceHistoryApi _internalApi;

        public PriceHistoryApiWithRetry(IPriceHistoryApi internalApi, int numberOfAttemps)
        {
            _internalApi = internalApi;
            _numberOfAttemps = numberOfAttemps;
        }

        /// <summary>
        /// Fetches historical price data for a given cryptocurrency symbol and date range from the Yahoo Finance API.
        /// </summary>
        /// <param name="symbolPair">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbolPair, DateTime startDate, DateTime endDate)
        {
            int retryCount = 0;
            while (retryCount < _numberOfAttemps)
            {
                var priceResult = await _internalApi.FetchPriceHistoryAsync(symbolPair, startDate, endDate).ConfigureAwait(false);
                if (priceResult.IsSuccess)
                {
                    return priceResult;
                }

                retryCount++;
                Log.Warning("Retrying price retrieval for {CurrencyCode} on {Date:yyyy-MM-dd}. Attempt {RetryCount}/{MaxRetries}", symbolPair, startDate, retryCount, _numberOfAttemps);
            }

            return Result.Failure<IEnumerable<PriceRecord>>($"Failed to get price history for {symbolPair} after {_numberOfAttemps} attemps."); // Indicating failure
        }

        public string DetermineTradingPair(string fromSymbol, string toSymbol)
        {
            return _internalApi.DetermineTradingPair(fromSymbol, toSymbol);
        }

        public async Task<Result<IEnumerable<PriceRecord>>> FetchCurrentPriceAsync(IEnumerable<string> symbols, string currency)
        {
            int retryCount = 0;
            while (retryCount < _numberOfAttemps)
            {
                var priceResult = await _internalApi.FetchCurrentPriceAsync(symbols, currency).ConfigureAwait(false);
                if (priceResult.IsSuccess)
                {
                    return priceResult;
                }

                retryCount++;
                Log.Warning("Retrying price retrieval for current price of {CurrencyCode}. Attempt {RetryCount}/{MaxRetries}", symbols, retryCount, _numberOfAttemps);
            }

            return Result.Failure<IEnumerable<PriceRecord>>($"Failed to get price history for {symbols} after {_numberOfAttemps} attemps."); // Indicating failure
        }
    }
}
