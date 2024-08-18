using System.Diagnostics;
using CSharpFunctionalExtensions;
using Serilog;
using YahooFinanceApi;

namespace Portfolio.App.HistoricalPrice
{
    public class YahooFinancePriceHistoryApi : IPriceHistoryApi
    {
        public async Task<Result<IEnumerable<CryptoPriceRecord>>> FetchDataAsync(string symbolPair, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Log the beginning of the data fetch operation
                Log.Information("Initiating data fetch for {SymbolPair} from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.", symbolPair, startDate, endDate);

                // Start a timer to measure how long the data fetch takes
                var stopwatch = Stopwatch.StartNew();

                // Fetch the data from Yahoo Finance API
                var candles = await Yahoo.GetHistoricalAsync(symbolPair, startDate.Date, endDate.Date, Period.Daily);

                // Log the time taken to fetch the data
                stopwatch.Stop();
                Log.Information("Data fetch for {SymbolPair} completed in {ElapsedMilliseconds}ms. Retrieved {CandlesCount} day(s) of data.", symbolPair, stopwatch.ElapsedMilliseconds, candles.Count());

                return Result.Success(MapCandlesToCryptoPriceData(symbolPair, candles));
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred while fetching data for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<CryptoPriceRecord>>(Errors.ERR_YAHOO_API_FETCH_FAILURE);
            }
            catch (TimeoutException timeoutEx)
            {
                // Log timeout errors separately
                Log.Error(timeoutEx, "Timeout occurred while fetching data for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<CryptoPriceRecord>>("Timeout while fetching data.");
            }
            catch (Exception ex)
            {
                // General catch-all for unexpected errors
                Log.Error(ex, "Unexpected error in {MethodName} for {SymbolPair}.", nameof(FetchDataAsync), symbolPair);
                return Result.Failure<IEnumerable<CryptoPriceRecord>>(Errors.ERR_YAHOO_API_FETCH_FAILURE);
            }
        }

        public string DetermineTradingPair(string fromSymbol, string toSymbol)
        {
            if (FiatCurrencies.Codes.Contains(fromSymbol) && FiatCurrencies.Codes.Contains(toSymbol))
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

        private static IEnumerable<CryptoPriceRecord> MapCandlesToCryptoPriceData(string currencyPair, IEnumerable<Candle> candles)
        {
            return candles.Select(c => ToCryptoPriceData(currencyPair, c));
        }

        private static CryptoPriceRecord ToCryptoPriceData(string currencyPair, Candle candle)
        {
            return new CryptoPriceRecord
            {
                CurrencyPair = currencyPair,
                CloseDate = candle.DateTime,
                ClosePrice = candle.Close
            };
        }
    }
}
