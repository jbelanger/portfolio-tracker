using Portfolio.Domain.Common;

namespace Portfolio.Domain.Entities
{
    public class CryptoCurrencyProcessedTransaction : BaseAuditableEntity
    {
        public string WalletName { get; private set; }
        public string Asset { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime DateTime { get; private set; }
        public decimal? AveragePriceAtTime { get; private set; } // New metadata
        public decimal? BalanceAfterTransaction { get; private set; } // New metadata

        private CryptoCurrencyProcessedTransaction() { }

        public CryptoCurrencyProcessedTransaction(string walletName, string asset, decimal amount, DateTime dateTime, decimal? averagePriceAtTime, decimal? balanceAfterTransaction)
        {
            WalletName = walletName;
            Asset = asset;
            Amount = amount;
            DateTime = dateTime;
            AveragePriceAtTime = averagePriceAtTime;
            BalanceAfterTransaction = balanceAfterTransaction;
        }

        // public static CryptoCurrencyProcessedTransaction CreateFromTransaction(Wallet wallet, CryptoCurrencyHolding holding, CryptoCurrencyRawTransaction transaction)
        // {
        //     return new CryptoCurrencyProcessedTransaction(
        //         wallet.Name,
        //         holding.Asset,
        //         transaction is CryptoCurrencyDepositTransaction ? (transaction as CryptoCurrencyDepositTransaction)!.Amount.Amount : 
        //         transaction is CryptoCurrencyWithdrawTransaction ? -(transaction as CryptoCurrencyWithdrawTransaction)!.Amount.Amount :
        //         (transaction as CryptoCurrencyTradeTransaction)!.Amount.Amount,
        //         transaction.DateTime,
        //         holding.AverageBoughtPrice,
        //         holding.Balance
        //     );
        // }
    }
}
