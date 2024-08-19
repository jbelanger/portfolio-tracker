namespace Portfolio
{
    /// <summary>
    /// Specifies the type of cryptocurrency transaction.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// The transaction type is not defined.
        /// </summary>
        Undefined,

        /// <summary>
        /// A trade between two types of assets.
        /// </summary>
        Trade,

        /// <summary>
        /// An addition of funds into an account.
        /// </summary>
        Deposit,

        /// <summary>
        /// A removal of funds from an account.
        /// </summary>
        Withdrawal,

        Fee
    }
}
