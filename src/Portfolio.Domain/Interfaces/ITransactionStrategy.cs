using CSharpFunctionalExtensions;
using Portfolio.Domain;
using Portfolio.Domain.Entities;

public interface ITransactionStrategy
{
    Task<Result> ProcessTransactionAsync(FinancialTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService);
}