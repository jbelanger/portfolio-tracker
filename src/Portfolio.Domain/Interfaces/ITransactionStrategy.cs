using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Interfaces
{
    public interface ITransactionStrategy
    {
        Task<Result> ProcessTransactionAsync(FinancialTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService);
    }
}