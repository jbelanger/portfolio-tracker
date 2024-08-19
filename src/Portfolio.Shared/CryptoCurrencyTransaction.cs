namespace Portfolio.Shared
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>
    public class CryptoCurrencyTransaction
    {
        /// <summary>
        /// Gets or sets the date and time of the transaction.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the type of transaction, e.g., trade, deposit, withdrawal.
        /// </summary>
        public TransactionType Type { get; set; }

        /// <summary>
        /// Gets or sets the total amount received in the transaction without any fees.
        /// </summary>
        public Money? ReceivedAmount { get; set; } = null!;

        /// <summary>
        /// Gets or sets the total amount sent in the transaction without any fees.
        /// </summary>
        public Money? SentAmount { get; set; }

        /// <summary>
        /// Gets or sets the fee amount for the transaction.
        /// </summary>
        public Money? FeeAmount { get; set; } = null!;

        /// <summary>
        /// Gets or sets the account identifier associated with the transaction.
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a note or description associated with the transaction.
        /// </summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of transaction identifiers related to this transaction.
        /// </summary>
        public IEnumerable<string> TransactionIds { get; set; } = new List<string>();

        /// <summary>
        /// Private constructor used internally for factory methods.
        /// </summary>
        public CryptoCurrencyTransaction()
        { }
    }
}
