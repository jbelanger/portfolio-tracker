namespace Portfolio.App;

public class MockCurrencyExchangeService : ICurrencyExchangeService
{
    public Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date)
    {
        // Return a mock exchange rate
        return Task.FromResult(1.4m);
    }
}
