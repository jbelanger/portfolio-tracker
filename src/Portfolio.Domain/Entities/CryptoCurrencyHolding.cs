using CSharpFunctionalExtensions;

namespace Portfolio.Domain.Entities
{
    public class CryptoCurrencyHolding
    {
        public string Asset { get; init; }
        public decimal AverageBoughtPrice { get; set; }
        public decimal Balance { get; set; }
        private readonly HashSet<CryptoCurrencyTransaction> _transactions = new();

        public IReadOnlyCollection<CryptoCurrencyTransaction> Transactions => _transactions;

        public CryptoCurrencyHolding(string asset)
        {
            Asset = asset;
        }

        public Result AddTransaction(CryptoCurrencyTransaction transaction)
        {
            if (transaction == null)
                return Result.Failure("Transaction cannot be null.");

            if (!_transactions.Add(transaction))
            {
                return Result.Failure("Transaction already exists in this holding.");
            }

            UpdateBalance(transaction, isAdding: true);

            return Result.Success();
        }

        public Result RemoveTransaction(CryptoCurrencyTransaction transaction)
        {
            if (transaction == null)
                return Result.Failure("Transaction cannot be null.");

            if (!_transactions.Remove(transaction))
            {
                return Result.Failure("Transaction not found in this holding.");
            }

            UpdateBalance(transaction, isAdding: false);

            return Result.Success();
        }

        private void UpdateBalance(CryptoCurrencyTransaction transaction, bool isAdding)
        {
            if (transaction is CryptoCurrencyDepositTransaction deposit)
            {
                Balance += isAdding ? deposit.Amount.Amount : -deposit.Amount.Amount;
            }
            else if (transaction is CryptoCurrencyWithdrawTransaction withdraw)
            {
                Balance -= isAdding ? withdraw.Amount.Amount : -withdraw.Amount.Amount;
            }
            else if (transaction is CryptoCurrencyTradeTransaction trade)
            {
                if (trade.Amount.CurrencyCode == Asset)
                {
                    Balance += isAdding ? trade.Amount.Amount : -trade.Amount.Amount;
                }
                else if (trade.TradeAmount.CurrencyCode == Asset)
                {
                    Balance -= isAdding ? trade.TradeAmount.Amount : -trade.TradeAmount.Amount;
                }
            }
        }

        public void UpdateAverageBoughtPrice(CryptoCurrencyDepositTransaction deposit)
        {
            if (Balance == 0)
            {
                AverageBoughtPrice = deposit.UnitValue.Amount;
            }
            else
            {
                decimal newAverage = (AverageBoughtPrice * (Balance - deposit.Amount.Amount) + deposit.Amount.Amount * deposit.UnitValue.Amount) / Balance;
                AverageBoughtPrice = newAverage;
            }
        }

        public void UpdateAverageBoughtPrice(CryptoCurrencyTradeTransaction trade)
        {
            if (trade.Amount.CurrencyCode == Asset)
            {
                decimal newAverage = (AverageBoughtPrice * Balance + trade.Amount.Amount * trade.TradeAmount.Amount) / (Balance + trade.Amount.Amount);
                AverageBoughtPrice = newAverage;
            }
        }
    }
}
