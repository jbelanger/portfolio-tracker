namespace Portfolio.Domain.ValueObjects
{
    /// <summary>
    /// Specifies the type of financial transaction, including trades, deposits, withdrawals, and fees.
    /// This enum is used to categorize transactions and determine the appropriate processing strategy.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// The transaction type is not defined or is unknown.
        /// This value should generally be avoided in favor of a defined transaction type.
        /// </summary>
        Undefined,

        /// <summary>
        /// A trade transaction, representing an exchange between two different assets.
        /// For example, trading one cryptocurrency for another, or exchanging stocks.
        /// </summary>
        Trade,

        /// <summary>
        /// A deposit transaction, representing the addition of funds or assets into an account.
        /// For example, depositing money into a bank account or adding cryptocurrency to a wallet.
        /// </summary>
        Deposit,

        /// <summary>
        /// A withdrawal transaction, representing the removal of funds or assets from an account.
        /// For example, withdrawing cash from a bank account or transferring cryptocurrency out of a wallet.
        /// </summary>
        Withdrawal,

        /// <summary>
        /// A fee transaction, representing a charge or cost associated with another transaction.
        /// Fees may be applied during trades, deposits, or withdrawals.
        /// </summary>
        Fee
    }
}
