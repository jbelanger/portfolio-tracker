using Portfolio.Domain.Common;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities
{
    /// <summary>
    /// Represents a cryptocurrency transaction, which can be a trade, deposit, or withdrawal.
    /// </summary>    
    public abstract class CryptoCurrencyTransaction : BaseAuditableEntity, ICryptoCurrencyTransaction
    {
        public Money UnitValue { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time of the transaction.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the type of transaction, e.g., trade, deposit, withdrawal.
        /// </summary>
        public TransactionType Type { get; set; }

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

        public Money Amount { get; set; }

        public object State { get; set; }

        /// <summary>
        /// Private constructor used internally for factory methods.
        /// </summary>
        protected CryptoCurrencyTransaction()
        { }
    }
}
