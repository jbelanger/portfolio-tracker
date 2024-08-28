using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.HistoricalPrice.AlphaVantage
{
    /// <summary>
    /// Provides an implementation of <see cref="IPriceHistoryApi"/> that retrieves current daily cryptocurrency price data from the Alpha Vantage API.
    /// </summary>
    public class AlphaVantagePriceHistoryApi : IPriceHistoryApi
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private static readonly SemaphoreSlim _semaphore = new(1, 1); // Limit to 1 request at a time
        private static DateTime _lastRequestTime = DateTime.MinValue;
        private const int RequestsPerMinute = 5;

        public AlphaVantagePriceHistoryApi(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        /// <summary>
        /// Fetches current daily price data for a given cryptocurrency symbol from the Alpha Vantage API.
        /// </summary>
        /// <param name="symbolPair">The trading pair symbol, e.g., "BTC/USD".</param>
        /// <param name="startDate">The start date for fetching data. Only today's date is supported with the free API.</param>
        /// <param="endDate">The end date for fetching data. Only today's date is supported with the free API.</param>
        /// <returns>A <see cref="Result{T}"/> containing a list of <see cref="PriceRecord"/> or an error message.</returns>
        public async Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbolPair, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Validate that only current daily prices are requested
                if (startDate.Date != DateTime.Today || endDate.Date != DateTime.Today)
                {
                    Log.Error("Alpha Vantage free API only supports querying the current daily prices. Requested range: {StartDate} to {EndDate}.", startDate, endDate);
                    return Result.Failure<IEnumerable<PriceRecord>>("Alpha Vantage free API only supports querying the current daily prices.");
                }

                // Ensure we do not exceed the rate limit
                await _semaphore.WaitAsync();

                try
                {
                    var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                    if (timeSinceLastRequest < TimeSpan.FromSeconds(60.0 / RequestsPerMinute))
                    {
                        var delay = TimeSpan.FromSeconds(60.0 / RequestsPerMinute) - timeSinceLastRequest;
                        await Task.Delay(delay);
                    }

                    _lastRequestTime = DateTime.UtcNow;

                    // Log the beginning of the data fetch operation
                    Log.ForContext<AlphaVantagePriceHistoryApi>().Information("Initiating data fetch for {SymbolPair} for today's date: {CurrentDate:yyyy-MM-dd}.", symbolPair, DateTime.Today);

                    // Start a timer to measure how long the data fetch takes
                    var stopwatch = Stopwatch.StartNew();

                    var uri = $"https://www.alphavantage.co/query?function=DIGITAL_CURRENCY_DAILY&symbol={symbolPair}&market=USD&apikey={_apiKey}";
                    var response = await _httpClient.GetAsync(uri).ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        Log.Error("Failed to fetch data for {SymbolPair}. HTTP status: {StatusCode}", symbolPair, response.StatusCode);
                        return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_ALPHAVANTAGE_API_FETCH_FAILURE);
                    }

                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var json = JObject.Parse(content);

                    if (!json.ContainsKey("Time Series (Digital Currency Daily)"))
                    {
                        Log.Error("Alpha Vantage API returned an unexpected format for {SymbolPair}.", symbolPair);
                        return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_ALPHAVANTAGE_API_FETCH_FAILURE);
                    }

                    var todayData = json["Time Series (Digital Currency Daily)"]
                                    .Cast<JProperty>()
                                    .Where(x => DateTime.Parse(x.Name) == DateTime.Today)
                                    .Select(x => ToCryptoPriceData(symbolPair, x))
                                    .ToList();

                    // Log the time taken to fetch the data
                    stopwatch.Stop();
                    Log.Information("Data fetch for {SymbolPair} completed in {ElapsedMilliseconds}ms. Retrieved {RecordsCount} day(s) of data.", symbolPair, stopwatch.ElapsedMilliseconds, todayData.Count);

                    return Result.Success(todayData.AsEnumerable());
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Log specific errors for HTTP request issues
                Log.Error(httpEx, "HTTP error occurred while fetching data for {SymbolPair}.", symbolPair);
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_ALPHAVANTAGE_API_FETCH_FAILURE);
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
                return Result.Failure<IEnumerable<PriceRecord>>(Errors.ERR_ALPHAVANTAGE_API_FETCH_FAILURE);
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
            return $"{fromSymbol}/{toSymbol}";
        }

        /// <summary>
        /// Converts a JSON object from Alpha Vantage API into a <see cref="PriceRecord"/> object.
        /// </summary>
        /// <param name="currencyPair">The trading pair symbol (e.g., "BTC/USD").</param>
        /// <param name="priceData">The <see cref="JProperty"/> object representing the historical price data for a specific date.</param>
        /// <returns>A <see cref="PriceRecord"/> object containing the price data.</returns>
        private static PriceRecord ToCryptoPriceData(string currencyPair, JProperty priceData)
        {
            var date = DateTime.Parse(priceData.Name);
            var closePrice = priceData.Value["4a. close (USD)"].Value<decimal>();

            return new PriceRecord
            {
                CurrencyPair = currencyPair,
                CloseDate = date,
                ClosePrice = closePrice
            };
        }

        Task<Result<IEnumerable<PriceRecord>>> IPriceHistoryApi.FetchCurrentPriceAsync(IEnumerable<string> symbols, string currency)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<PriceRecord>>> FetchPriceHistoryAsync(string symbolPair, string currency, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException();
        }
    }
}
