using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Portfolio.App.Common.Interfaces;

namespace Portfolio.App.HistoricalPrice.CoinGecko
{
    public interface ICoinDataResolver
    {
        Task<Result<CoinInfo>> FindBestMatchBySymbol(string symbol);
        Task<Result<IEnumerable<CoinInfo>>> FindBestMatchBySymbols(IEnumerable<string> symbols);
    }

    public class CoinGeckoCoinListApi : ICoinDataResolver
    {
        private readonly string COIN_GECKO_COIN_LIST_URI = "https://api.coingecko.com/api/v3/coins/list";
        private readonly string COIN_GECKO_MARKETS_URI = $"https://api.coingecko.com/api/v3/coins/markets";

        private readonly HttpClient _httpClient;
        private readonly MemoryCache _cache;
        private readonly IApplicationDbContext _dbContext;
        private readonly RateLimiter _rateLimiter;

        private int _errorCacheDurationMinutes = 5;

        private int RetryAfter => 60 / _rateLimiter.RequestsPerMinute;

        public int RequestsPerMinute
        {
            get => _rateLimiter.RequestsPerMinute;
            set => _rateLimiter.UpdateRequestsPerMinute(value);
        }

        public int ErrorCacheDurationMinutes
        {
            get => _errorCacheDurationMinutes;
            set
            {
                if (value > 0)
                    _errorCacheDurationMinutes = value;
                throw new InvalidOperationException("ErrorCacheInMinutes must be greater than 0.");
            }
        }

        public CoinGeckoCoinListApi(IHttpClientFactory httpClientFactory, MemoryCache cache, IApplicationDbContext dbContext)
        {
            _httpClient = httpClientFactory.CreateClient("CoinGeckoClient") ?? throw new InvalidOperationException(nameof(httpClientFactory.CreateClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _rateLimiter = new RateLimiter(15); // Default to 15 requests per minute
        }

        public async Task<Result<CoinInfo>> FindBestMatchBySymbol(string symbol)
        {
            var searchSymbol = symbol.ToLower();

            var result = await RefreshIfNecessaryAsync();
            if (result.IsFailure)
                return Result.Failure<CoinInfo>(result.Error);

            // Look in DB first
            var coinInfo = _dbContext.CoinInfos.Where(c => c.Symbol == searchSymbol).OrderBy(c => c.MarketCapRank).FirstOrDefault();
            if (coinInfo == null)
                return Result.Failure<CoinInfo>($"Coin with symbol {symbol} could not be found.");

            return coinInfo;
        }

        public async Task<Result<IEnumerable<CoinInfo>>> FindBestMatchBySymbols(IEnumerable<string> symbols)
        {
            var searchSymbols = symbols.Select(s => s.ToLower());

            var result = await RefreshIfNecessaryAsync();
            if (result.IsFailure)
                return Result.Failure<IEnumerable<CoinInfo>>(result.Error);

            // Look in DB first
            var coinInfoList = _dbContext.CoinInfos.Where(c => searchSymbols.Contains(c.Symbol)).OrderBy(c => c.MarketCapRank).ToList();
            if (!coinInfoList.Any())
            {
                var symbolsString = string.Join(",", searchSymbols);
                Log.Error("Coins with symbols '{SymbolsString}' could not be found.", symbolsString);
                return Result.Failure<IEnumerable<CoinInfo>>($"Coins with symbols '{symbolsString}' could not be found.");
            }

            var group = coinInfoList.GroupBy(c => c.Symbol);
            var coinInfoListReturn = new List<CoinInfo>();
            foreach (var g in group)
            {
                if (g.Sum(c => c.MarketCapRank) == 0)
                    Log.Warning("No market cap ranking data for coin '{Key}'", g.Key);

                var possiblyHighestRanked = g.OrderBy(c => c.MarketCapRank).First();
                coinInfoListReturn.Add(possiblyHighestRanked);
            }

            return coinInfoListReturn;
        }

        public async Task<Result> RefreshIfNecessaryAsync()
        {
            if (!_dbContext.CoinInfos.Any())
            {
                var coinListResult = await FetchCoinDataFromListApiAsync();
                if (coinListResult.IsFailure)
                    return coinListResult;

                var coinList = coinListResult.Value;
                coinList = coinList.OrderBy(c => c.Symbol);

                _dbContext.CoinInfos.AddRange(coinListResult.Value);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                var fetchResult = await FetchPaginatedCoinDataAsync(1, 1000, 250).ConfigureAwait(false);
                if (fetchResult.IsFailure)
                    return fetchResult;

                foreach (var c in fetchResult.Value)
                {
                    var coinInfo = coinList.FirstOrDefault(x => x.CoinId == c.CoinId);
                    if (coinInfo == null)
                    {
                        _dbContext.CoinInfos.Add(c);
                        continue;
                    }

                    coinInfo.MarketCapRank = c.MarketCapRank;
                    coinInfo.Image = c.Image;
                    coinInfo.CurrentPrice = c.CurrentPrice;
                    coinInfo.MarketCap = c.MarketCap;
                    coinInfo.MarketCapRank = c.MarketCapRank;
                    coinInfo.FullyDilutedValuation = c.FullyDilutedValuation;
                    coinInfo.TotalVolume = c.TotalVolume;
                    coinInfo.High24h = c.High24h;
                    coinInfo.Low24h = c.Low24h;
                    coinInfo.PriceChange24h = c.PriceChange24h;
                    coinInfo.PriceChangePercentage24h = c.PriceChangePercentage24h;
                    coinInfo.MarketCapChange24h = c.MarketCapChange24h;
                    coinInfo.MarketCapChangePercentage24h = c.MarketCapChangePercentage24h;
                    coinInfo.CirculatingSupply = c.CirculatingSupply;
                    coinInfo.TotalSupply = c.TotalSupply;
                    coinInfo.MaxSupply = c.MaxSupply;
                    coinInfo.Ath = c.Ath;
                    coinInfo.AthChangePercentage = c.AthChangePercentage;
                    coinInfo.AthDate = c.AthDate;
                    coinInfo.Atl = c.Atl;
                    coinInfo.AtlChangePercentage = c.AthChangePercentage;
                    coinInfo.AtlDate = c.AtlDate;
                    coinInfo.LastUpdated = c.LastUpdated;
                }

                _dbContext.HttpRequestLogEntries.Add(new HttpRequestLogEntry(){ RequestDate = DateTime.UtcNow, RequestUri = COIN_GECKO_COIN_LIST_URI} );
                _dbContext.HttpRequestLogEntries.Add(new HttpRequestLogEntry(){ RequestDate = DateTime.UtcNow, RequestUri = COIN_GECKO_MARKETS_URI} );
                
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            return Result.Success();
        }

        private async Task<Result<IEnumerable<CoinInfo>>> FetchPaginatedCoinDataAsync(int currentPage, int totalCoins, int pageSize = 250)
        {
            if (IsInErrorCache(nameof(FetchPaginatedCoinDataAsync)))
            {
                Log.Warning("Skipping request for the CoinGecko markets api due to recent error.");
                return Result.Failure<IEnumerable<CoinInfo>>("Failed to fetch coins market information from the CoinGecko markets API. Please try again later.");
            }

            var totalPages = (int)Math.Ceiling(totalCoins / (double)pageSize);
            List<CoinInfo> coinList = new List<CoinInfo>();

            for (int page = currentPage; page <= totalPages; page++)
            {
                await _rateLimiter.EnsureRateLimitAsync();

                var uriMarkets = $"{COIN_GECKO_MARKETS_URI}?vs_currency=usd&order=market_cap_desc&per_page={pageSize}&page={page}";

                var response = await RetryUtility.RetryAsync(
                    () => _httpClient.GetAsync(uriMarkets),
                    maxRetries: 3,
                    delay: TimeSpan.FromSeconds(RetryAfter)).ConfigureAwait(false);

                if (response.IsFailure || !response.Value.IsSuccessStatusCode)
                {
                    Log.Error("Failed to fetch supported coins from CoinGecko markets API. HTTP status: {StatusCode} for page {Page}", response.Value.StatusCode, page);
                    AddToErrorCache(nameof(FetchPaginatedCoinDataAsync));
                    return Result.Failure<IEnumerable<CoinInfo>>("Failed to fetch coins market information from the CoinGecko markets API. Please try again later.");
                }

                var content = await response.Value.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JArray.Parse(content);

                coinList.AddRange(ParseCoinsFromMarketsResponse(json));
            }

            return coinList;
        }

        public async Task<Result<IEnumerable<CoinInfo>>> FetchCoinDataFromListApiAsync()
        {
            var response = await RetryUtility.RetryAsync(
                () => _httpClient.GetAsync(COIN_GECKO_COIN_LIST_URI),
                maxRetries: 3,
                delay: TimeSpan.FromSeconds(RetryAfter)).ConfigureAwait(false);

            if (response.IsFailure || !response.Value.IsSuccessStatusCode)
            {
                Log.Error("Failed to fetch supported coins from CoinGecko list API. HTTP status: {StatusCode}", response.Value.StatusCode);
                return Result.Failure<IEnumerable<CoinInfo>>("Failed to fetch coin information from the CoinGecko Coin List API. Please try again later.");
            }

            var content = await response.Value.Content.ReadAsStringAsync().ConfigureAwait(false);
            var json = JArray.Parse(content);

            return Result.Success(ParseCoinsFromMarketsResponse(json));
        }

        private IEnumerable<CoinInfo> ParseCoinsFromMarketsResponse(JArray json)
        {
            foreach (var coin in json)
            {
                var symbol = coin["symbol"]?.Value<string>()?.ToLower();
                var id = coin["id"]?.Value<string>();

                if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(id))
                {
                    yield return new CoinInfo
                    {
                        CoinId = id,
                        Symbol = symbol,
                        Name = coin["name"]?.Value<string>() ?? string.Empty,
                        Image = coin["image"]?.Value<string>() ?? string.Empty,
                        CurrentPrice = SafeConvertToDecimal(coin["current_price"]),
                        MarketCap = SafeConvertToDecimal(coin["market_cap"]),
                        MarketCapRank = coin["market_cap_rank"]?.Value<int?>(),
                        FullyDilutedValuation = SafeConvertToDecimal(coin["fully_diluted_valuation"]),
                        TotalVolume = SafeConvertToDecimal(coin["total_volume"]),
                        High24h = SafeConvertToDecimal(coin["high_24h"]),
                        Low24h = SafeConvertToDecimal(coin["low_24h"]),
                        PriceChange24h = SafeConvertToDecimal(coin["price_change_24h"]),
                        PriceChangePercentage24h = SafeConvertToDecimal(coin["price_change_percentage_24h"]),
                        MarketCapChange24h = SafeConvertToDecimal(coin["market_cap_change_24h"]),
                        MarketCapChangePercentage24h = SafeConvertToDecimal(coin["market_cap_change_percentage_24h"]),
                        CirculatingSupply = SafeConvertToDecimal(coin["circulating_supply"]),
                        TotalSupply = SafeConvertToDecimal(coin["total_supply"]),
                        MaxSupply = SafeConvertToDecimal(coin["max_supply"]),
                        Ath = SafeConvertToDecimal(coin["ath"]),
                        AthChangePercentage = SafeConvertToDecimal(coin["ath_change_percentage"]),
                        AthDate = coin["ath_date"]?.Value<DateTime?>(),
                        Atl = SafeConvertToDecimal(coin["atl"]),
                        AtlChangePercentage = SafeConvertToDecimal(coin["atl_change_percentage"]),
                        AtlDate = coin["atl_date"]?.Value<DateTime?>(),
                        LastUpdated = coin["last_updated"]?.Value<string>() ?? string.Empty
                    };
                }
            }
        }

        // private IEnumerable<CoinInfo> ParseCoinsFromCoinListResponse(JArray json)
        // {
        //     foreach (var coin in json)
        //     {
        //         var symbol = coin["symbol"]?.Value<string>()?.ToLower();
        //         var id = coin["id"]?.Value<string>();

        //         if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(id))
        //         {
        //             yield return new CoinInfo
        //             {
        //                 CoinId = id,
        //                 Symbol = symbol,
        //                 Name = coin["name"]?.Value<string>() ?? string.Empty
        //             };
        //         }
        //     }
        // }

        private decimal? SafeConvertToDecimal(JToken? token)
        {
            if (token == null)
            {
                return null;
            }

            try
            {
                return token.Value<decimal?>();
            }
            catch (OverflowException)
            {
                Log.Warning("Overflow when converting {TokenValue} to decimal.", token);
                return null; // Returning null to handle the overflow scenario
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error when converting {TokenValue} to decimal.", token);
                return null; // Returning null for any other conversion error
            }
        }

        private void AddToErrorCache(string uri)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_errorCacheDurationMinutes));
            _cache.Set($"ErrorCache_{uri}", true, cacheEntryOptions);
        }

        private bool IsInErrorCache(string uri)
        {
            return _cache.TryGetValue($"ErrorCache_{uri}", out _);
        }
    }
}
