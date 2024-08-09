namespace Portfolio;

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
    public Money? ReceivedAmount { get; set; }

    /// <summary>
    /// Gets or sets the total amount sent in the transaction without any fees.
    /// </summary>
    public Money? SentAmount { get; set; }

    /// <summary>
    /// Gets or sets the fee amount for the transaction.
    /// </summary>
    public Money? FeeAmount { get; set; }

    /// <summary>
    /// Gets or sets the account identifier associated with the transaction.
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a note or description associated with the transaction.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets a list of transaction identifiers related to this transaction.
    /// </summary>
    public IEnumerable<string> TransactionIds { get; set; } = new List<string>();

    /// <summary>
    /// Private constructor used internally for factory methods.
    /// </summary>
    private CryptoCurrencyTransaction()
    { }

    /// <summary>
    /// Factory method to create a trade transaction.
    /// </summary>
    /// <param name="date">The date and time of the trade.</param>
    /// <param name="receivedAmount">The amount received in the trade.</param>
    /// <param name="sentAmount">The amount sent in the trade.</param>
    /// <param name="feeAmount">The transaction fee associated with the trade.</param>
    /// <param name="account">The account identifier associated with the trade.</param>
    /// <param name="transactionIds">The list of transaction identifiers.</param>
    /// <param name="note">An optional note associated with the trade.</param>
    /// <returns>A new instance of <see cref="CryptoCurrencyTransaction"/> representing a trade.</returns>
    public static CryptoCurrencyTransaction CreateTrade(
        DateTime date,
        Money receivedAmount,
        Money sentAmount,
        Money feeAmount,
        string account,
        IEnumerable<string> transactionIds,
        string note = "")
    {
        if (receivedAmount == null)
            throw new ArgumentNullException(nameof(receivedAmount), "Received amount cannot be null for a trade transaction.");
        if (sentAmount == null)
            throw new ArgumentNullException(nameof(sentAmount), "Sent amount cannot be null for a trade transaction.");
        if (string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("Account cannot be null or whitespace.", nameof(account));
        if (transactionIds == null || !transactionIds.Any())
            throw new ArgumentException("Transaction IDs cannot be null or empty.", nameof(transactionIds));

        var trade = new CryptoCurrencyTransaction()
        {
            DateTime = date,
            Type = TransactionType.Trade,
            ReceivedAmount = receivedAmount,
            SentAmount = sentAmount,
            FeeAmount = feeAmount,
            Account = account,
            TransactionIds = transactionIds,
            Note = note
        };
        return trade;
    }

    /// <summary>
    /// Factory method to create a deposit transaction.
    /// </summary>
    /// <param name="date">The date and time of the deposit.</param>
    /// <param name="receivedAmount">The amount received in the deposit.</param>
    /// <param name="feeAmount">The transaction fee associated with the deposit.</param>
    /// <param name="account">The account identifier associated with the deposit.</param>
    /// <param name="transactionIds">The list of transaction identifiers.</param>
    /// <param name="note">An optional note associated with the deposit.</param>
    /// <returns>A new instance of <see cref="CryptoCurrencyTransaction"/> representing a deposit.</returns>
    public static CryptoCurrencyTransaction CreateDeposit(
        DateTime date,
        Money receivedAmount,
        Money feeAmount,
        string account,
        IEnumerable<string> transactionIds,
        string note = "")
    {
        if (receivedAmount == null)
            throw new ArgumentNullException(nameof(receivedAmount), "Received amount cannot be null for a deposit.");
        if (string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("Account cannot be null or whitespace.", nameof(account));
        if (transactionIds == null || !transactionIds.Any())
            throw new ArgumentException("Transaction IDs cannot be null or empty.", nameof(transactionIds));

        var deposit = new CryptoCurrencyTransaction()
        {
            DateTime = date,
            Type =  TransactionType.Deposit,
            ReceivedAmount = receivedAmount,
            FeeAmount = feeAmount,
            Account = account,
            TransactionIds = transactionIds,
            Note = note
        };
        return deposit;
    }

    /// <summary>
    /// Factory method to create a withdrawal transaction.
    /// </summary>
    /// <param name="date">The date and time of the withdrawal.</param>
    /// <param name="sentAmount">The amount sent in the withdrawal.</param>
    /// <param name="feeAmount">The transaction fee associated with the withdrawal.</param>
    /// <param name="account">The account identifier associated with the withdrawal.</param>
    /// <param name="transactionIds">The list of transaction identifiers.</param>
    /// <param name="note">An optional note associated with the withdrawal.</param>
    /// <returns>A new instance of <see cref="CryptoCurrencyTransaction"/> representing a withdrawal.</returns>
    public static CryptoCurrencyTransaction CreateWithdrawal(
        DateTime date,
        Money sentAmount,
        Money feeAmount,
        string account,
        IEnumerable<string> transactionIds,
        string note = "")
    {
        if (sentAmount == null)
            throw new ArgumentNullException(nameof(sentAmount), "Sent amount cannot be null for a withdrawal.");
        if (string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("Account cannot be null or whitespace.", nameof(account));
        if (transactionIds == null || !transactionIds.Any())
            throw new ArgumentException("Transaction IDs cannot be null or empty.", nameof(transactionIds));

        var withdrawal = new CryptoCurrencyTransaction()
        {
            DateTime = date,
            Type = TransactionType.Withdrawal,
            SentAmount = sentAmount,
            FeeAmount = feeAmount,
            Account = account,
            TransactionIds = transactionIds,
            Note = note
        };
        return withdrawal;
    }

        public static CryptoCurrencyTransaction CreateFee(
        DateTime date,        
        Money feeAmount,
        string account,
        IEnumerable<string> transactionIds,
        string note = "")
    {
        if (feeAmount == null)
            throw new ArgumentNullException(nameof(feeAmount), "Fee amount cannot be null for a fee.");
        if (string.IsNullOrWhiteSpace(account))
            throw new ArgumentException("Account cannot be null or whitespace.", nameof(account));
        if (transactionIds == null || !transactionIds.Any())
            throw new ArgumentException("Transaction IDs cannot be null or empty.", nameof(transactionIds));

        var fee = new CryptoCurrencyTransaction()
        {
            DateTime = date,
            Type =  TransactionType.Fee,            
            SentAmount = feeAmount,
            Account = account,
            TransactionIds = transactionIds,
            Note = note
        };
        return fee;
    }
}
