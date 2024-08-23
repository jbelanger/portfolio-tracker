using CSharpFunctionalExtensions;
using Portfolio.Domain;
using Portfolio.Domain.Entities;

public interface ITransactionStrategy
{
    Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService);
}