namespace Portfolio.App;

public interface ICurrencyExchangeService
{
    Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency, DateTime date);
}
