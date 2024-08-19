using CSharpFunctionalExtensions;
using Portfolio.Shared;

namespace Portfolio;

public class CryptoCurrencyHolding
{
    public string Asset { get; init; }
    public decimal AverageBoughtPrice { get; set; }
    public decimal Balance { get; set; }
    public List<ICryptoCurrencyTransaction> Transactions { get; set; } = new();
    public decimal Fees { get; set; }
    public Money CurrentPrice { get; set; }

    public CryptoCurrencyHolding(string asset)
    {
        Asset = asset;
    }

    public Result AddTransaction(ICryptoCurrencyTransaction transaction)
    {
        var lastTransaction = Transactions.LastOrDefault();
        if (lastTransaction != null && transaction.DateTime < lastTransaction.DateTime)
            return Result.Failure("Transactions not in order. Ensure transactions are added in a chronological order.");

        if (transaction is CryptoCurrencyDepositTransaction deposit)
        {
            Balance += deposit.Amount.Amount;
            Fees += deposit.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
        {
            Balance -= transaction.Amount.Add(transaction.FeeAmount).Amount;
            Fees += transaction.FeeAmount.Amount;

            if (Balance == 0)
                AverageBoughtPrice = 0;
        }

        if (transaction is CryptoCurrencyTradeTransaction trade)
        {
            if (trade.Amount.CurrencyCode == Asset)
            {
                Balance += trade.Amount.Amount;
            }
            else if (trade.TradeAmount.CurrencyCode == Asset)
            {
                Balance -= trade.TradeAmount.Amount;
                if (trade.FeeAmount.CurrencyCode == Asset)
                {
                    Balance -= trade.FeeAmount.Amount;
                    Fees += trade.FeeAmount.Amount;
                }

                if (Balance == 0)
                    AverageBoughtPrice = 0;
            }
            else
                return Result.Failure("Invalid transaction for this holding.");
        }

        Transactions.Add(transaction);

        return Result.Success();
    }

    public Result RemoveTransaction(ICryptoCurrencyTransaction transaction)
    {
        if (transaction is CryptoCurrencyDepositTransaction deposit)
        {
            Balance -= deposit.Amount.Amount;
            Fees -= deposit.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
        {
            Balance += transaction.Amount.Add(transaction.FeeAmount).Amount;
            Fees -= transaction.FeeAmount.Amount;
        }

        if (transaction is CryptoCurrencyTradeTransaction trade)
        {
            if (trade.Amount.CurrencyCode == Asset)
            {
                Balance -= trade.Amount.Amount;
            }
            else if (trade.TradeAmount.CurrencyCode == Asset)
            {
                Balance += trade.TradeAmount.Amount;
            }
            else
                return Result.Failure("Invalid transaction for this holding.");

            if (trade.FeeAmount.CurrencyCode == trade.Amount.CurrencyCode)
                Fees -= trade.FeeAmount.Amount;
            else if (trade.FeeAmount.CurrencyCode == trade.TradeAmount.CurrencyCode)
            {
                Balance += trade.FeeAmount.Amount;
                Fees -= trade.FeeAmount.Amount;
            }
        }

        Transactions.Remove(transaction);

        return Result.Success();
    }
}