using System;
using System.Threading.Tasks;

namespace Portfolio.App;

public interface IPriceHistoryStore
{
    Task<CryptoPriceData> GetPriceDataAsync(DateTime date);
}
