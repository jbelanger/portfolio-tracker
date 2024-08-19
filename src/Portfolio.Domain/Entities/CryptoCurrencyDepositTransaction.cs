using CSharpFunctionalExtensions;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>
    public class CryptoCurrencyDepositTransaction : CryptoCurrencyTransaction
    {


        /// <summary>
        /// Private constructor used internally for factory methods.
        /// </summary>
        private CryptoCurrencyDepositTransaction()
        { }

        /// <summary>
        /// Factory method to create a deposit transaction.
        /// </summary>
        /// <param name="date">The date and time of the deposit.</param>
        /// <param name="receivedAmount">The amount received in the deposit (without fees).</param>
        /// <param name="feeAmount">The transaction fee associated with the deposit.</param>
        /// <param name="account">The account identifier associated with the deposit.</param>
        /// <param name="transactionIds">The list of transaction identifiers.</param>
        /// <param name="note">An optional note associated with the deposit.</param>
        /// <returns>A new instance of <see cref="CryptoCurrencyTransaction"/> representing a deposit.</returns>
        public static Result<CryptoCurrencyDepositTransaction> Create(
            DateTime date,
            Money receivedAmount,
            Money feeAmount,
            string account,
            IEnumerable<string> transactionIds,
            string note = "")
        {
            if (receivedAmount == null)
                return Result.Failure<CryptoCurrencyDepositTransaction>($"Received amount cannot be null for a deposit.");

            if (feeAmount == null)
                feeAmount = new Money(0, receivedAmount.CurrencyCode);
            else if (feeAmount.CurrencyCode != receivedAmount.CurrencyCode)
                return Result.Failure<CryptoCurrencyDepositTransaction>($"Fees are not in the same currency as the deposit currency.");

            if (string.IsNullOrWhiteSpace(account))
                return Result.Failure<CryptoCurrencyDepositTransaction>($"Account cannot be null or whitespace.");

            if (transactionIds == null || !transactionIds.Any())
                return Result.Failure<CryptoCurrencyDepositTransaction>($"Transaction IDs cannot be null or empty.");

            var deposit = new CryptoCurrencyDepositTransaction()
            {
                DateTime = date,
                Amount = receivedAmount.ToAbsoluteAmountMoney(),
                FeeAmount = feeAmount.ToAbsoluteAmountMoney(),
                Account = account,
                TransactionIds = transactionIds,
                Note = note
            };
            return deposit;
        }

        public CryptoCurrencyTransaction ToGenericTransaction()
        {
            return new CryptoCurrencyTransaction
            {
                DateTime = DateTime,
                Type = TransactionType.Deposit,
                Amount = Amount,
                //SentAmount = null,
                FeeAmount = FeeAmount,
                Account = Account,
                Note = Note,
                TransactionIds = TransactionIds
            };
        }
    }
}
