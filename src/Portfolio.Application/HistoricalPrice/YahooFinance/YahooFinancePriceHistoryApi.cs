using System.Diagnostics;
using Portfolio.Domain;
using Portfolio.Domain.Constants;
using Portfolio.Domain.ValueObjects;
using YahooFinanceApi;

namespace Portfolio.App.HistoricalPrice.YahooFinance
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryApi"/> that retrieves historical cryptocurrency price data from the Yahoo Finance API.
    /// </summary>
    public class YahooFinancePriceHistoryApi : IPriceHistoryApi
    {
        /// <summary>
        /// Fetches historical price data for a given cryptocurrency symbol and date range from the Yahoo Finance API.
        /// </summary>
        /// <param name="symbolPair">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data.</param>
        /// <param name="endDate">The end date for fetching data.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbolPair, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Log the beginning of the data fetch operation
                Log.ForContext<YahooFinancePriceHistoryApi>().Information("Initiating data fetch for {SymbolPair} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.", symbolPair, startDate, endDate);

                // Start a timer to measure how long the data fetch takes
                var stopwatch = Stopwatch.StartNew();

                // Fetch the data from Yahoo Finance API
                var candles = await Yahoo.GetHistoricalAsync(symbolPair, startDate.Date, endDate.Date, Period.Daily).ConfigureAwait(false);

                // Log the time taken to fetch the data
                stopwatch.Stop();
                Log.Information("Data fetch for {SymbolPair} completed in {ElapsedMilliseconds}ms. Retrieved {CandlesCount} day(s) of data.", symbolPair, stopwatch.ElapsedMilliseconds, candles.Count());

                return Result.Success(MapCandlesToCryptoPriceData(symbolPair, candles));
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred while fetching data for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_YAHOO_API_FETCH_FAILURE);
            }
            catch (TimeoutException timeoutEx)
            {
                // Log timeout errors separately
                Log.Error(timeoutEx, "Timeout occurred while fetching data for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<PriceRecord>>("Timeout while fetching data.");
            }
            catch (Exception ex)
            {
                // General catch-all for unexpected errors
                Log.Error(ex, "Unexpected error in {MethodName} for {SymbolPair}.", nameof(FetchPriceHistoryAsync), symbolPair);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_YAHOO_API_FETCH_FAILURE);
            }
        }

        /// <summary>
        /// Determines the appropriate trading pair symbol based on the provided symbols.
        /// </summary>
        /// <param name="fromSymbol">The base currency or coin symbol.</param>
        /// <param name="toSymbol">The quote currency or coin symbol.</param>
        /// <returns>The trading pair symbol in the appropriate format.</returns>
        public string DetermineTradingPair(string fromSymbol, string toSymbol)
        {
            if (FiatCurrency.All.Any(f => f == fromSymbol) && FiatCurrency.All.Any(f => f == toSymbol))
                return $"{fromSymbol}{toSymbol}=X";

            return fromSymbol switch
            {
                "IMX" => $"IMX10603-{toSymbol}",
                "GRT" => $"GRT6719-{toSymbol}",
                "RNDR" => $"RENDER-{toSymbol}",
                "UNI" => $"UNI7083-{toSymbol}",
                "BEAM" => $"BEAM28298-{toSymbol}",
                _ => $"{fromSymbol}-{toSymbol}"
            };
        }

        /// <summary>
        /// Maps the historical price data (candles) retrieved from Yahoo Finance to a collection of <see cref="PriceRecord"/> objects.
        /// </summary>
        /// <param name="currencyPair">The trading pair symbol (e.g., "BTC/USD").</param>
        /// <param name="candles">The collection of <see cref="Candle"/> objects retrieved from Yahoo Finance API.</param>
        /// <returns>A collection of <see cref="PriceRecord"/> objects.</returns>
        private static IEnumerable<PriceRecord> MapCandlesToCryptoPriceData(string currencyPair, IEnumerable<Candle> candles)
        {
            return candles.Select(c => ToCryptoPriceData(currencyPair, c));
        }

        /// <summary>
        /// Converts a single <see cref="Candle"/> object into a <see cref="PriceRecord"/> object.
        /// </summary>
        /// <param name="currencyPair">The trading pair symbol (e.g., "BTC/USD").</param>
        /// <param name="candle">The <see cref="Candle"/> object representing the historical price data for a specific date.</param>
        /// <returns>A <see cref="PriceRecord"/> object containing the price data.</returns>
        private static PriceRecord ToCryptoPriceData(string currencyPair, Candle candle)
        {
            return new PriceRecord
            {
                CurrencyPair = currencyPair,
                CloseDate = candle.DateTime > DateTime.Now ? DateTime.Now.Date : candle.DateTime, // Yahoo API sometimes puts tomorrow's date when fetching current day...
                ClosePrice = candle.Close
            };
        }
    }
}
