using CSharpFunctionalExtensions;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain;

public class TransactionProcessor
{
    private readonly Dictionary<TransactionType, ITransactionStrategy> _transactionStrategies;

    public TransactionProcessor(Dictionary<TransactionType, ITransactionStrategy> transactionStrategies)
    {
        _transactionStrategies = transactionStrategies;
    }

    public async Task<Result> ProcessTransactionsAsync(IEnumerable<CryptoCurrencyRawTransaction> transactions, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
    {
        foreach (var tx in transactions)
        {
            if (_transactionStrategies.TryGetValue(tx.Type, out var strategy))
            {
                var result = await strategy.ProcessTransactionAsync(tx, portfolio, priceHistoryService);
                if (result.IsFailure)
                {
                    return result; // Optionally stop processing on failure
                }
            }
            else
            {
                return Result.Failure($"No strategy found for transaction type: {tx.Type}");
            }
        }

        return Result.Success();
    }
}
