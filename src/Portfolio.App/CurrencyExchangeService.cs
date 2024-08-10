using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class CurrencyExchangeService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.exchangeratesapi.io";

    public CurrencyExchangeService(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
    }

    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency)) 
            throw new ArgumentException("From currency code cannot be null or empty.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency)) 
            throw new ArgumentException("To currency code cannot be null or empty.", nameof(toCurrency));

        string formattedDate = date.ToString("yyyy-MM-dd");
        string url = $"{BaseUrl}/{formattedDate}?access_key={_apiKey}&base={fromCurrency}&symbols={toCurrency}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);
            decimal exchangeRate = json["rates"][toCurrency].Value<decimal>();
            return exchangeRate;
        }
        else
        {
            throw new Exception($"Error fetching exchange rate: {response.ReasonPhrase}");
        }
    }
}
