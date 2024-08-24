using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces
{
    public interface ICryptoCurrencyTransaction
    {
        /// <summary>
        /// Gets or sets the date and time of the transaction.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the total amount in the transaction without any fees.
        /// </summary>
        public Money Amount { get; set; }

        /// <summary>
        /// Gets or sets the fee amount for the transaction.
        /// </summary>
        public Money FeeAmount { get; set; }

        /// <summary>
        /// Gets or sets the account identifier associated with the transaction.
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Gets or sets a note or description associated with the transaction.
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets a list of transaction identifiers related to this transaction.
        /// </summary>
        public IEnumerable<string> TransactionIds { get; set; }

        public object State { get; set; }

        //public CryptoCurrencyTransaction ToGenericTransaction();

    }
}
