namespace Portfolio;

internal class Portfolio
{
    List<CryptoCurrencyTransaction> _transactions = new();
    List<CryptoCurrencyHolding> _holdings = new();


    internal void AddTransactions(IEnumerable<CryptoCurrencyTransaction> transactions)
    {
        _transactions.AddRange(transactions);
    }

    internal IEnumerable<CryptoCurrencyHolding> GetHoldings()
    {
        var holdings = new List<CryptoCurrencyHolding>();
        foreach (var tx in _transactions)
        {
            CryptoCurrencyHolding? sender = null;
            CryptoCurrencyHolding? receiver = null;

            if (tx.Type == TransactionType.Deposit)
            {
                receiver = GetReceivingHoldingForTransaction(tx);
                receiver.Balance += tx.ReceivedAmount.AbsoluteAmount;
            }

            // if (tx.SentAmount != null)
            // {
            //     sender = holdings.SingleOrDefault(h => h.Asset == tx.SentAmount.CurrencyCode);
            //     if (sender == null)
            //     {
            //         sender = new CryptoCurrencyHolding();
            //         sender.Asset = tx.SentAmount.CurrencyCode;
            //         holdings.Add(sender);
            //     }
            //     sender.Balance -= tx.SentAmount.AbsoluteAmount;
            //     sender.Transactions.Add(tx);
            // }

            // if (tx.ReceivedAmount != null)
            // {
            //     receiver = holdings.SingleOrDefault(h => h.Asset == tx.ReceivedAmount.CurrencyCode);
            //     if (receiver == null)
            //     {
            //         receiver = new CryptoCurrencyHolding(tx.ReceivedAmount.CurrencyCode);                    
            //         holdings.Add(receiver);
            //     }
            //     receiver.Balance += tx.ReceivedAmount.AbsoluteAmount;
            //     receiver.Transactions.Add(tx);

            // }
        }

        return holdings;
    }

    internal CryptoCurrencyHolding GetReceivingHoldingForTransaction(CryptoCurrencyTransaction transaction)
    {
        if(transaction.ReceivedAmount == null)
            throw new ArgumentException("Transaction has no receive amount.");

        var receiver = _holdings.SingleOrDefault(h => h.Asset == transaction.ReceivedAmount.CurrencyCode);
        if (receiver == null)
        {
            receiver = new CryptoCurrencyHolding(transaction.ReceivedAmount.CurrencyCode);  
            _holdings.Add(receiver);
        }
        return receiver;
    }

        internal CryptoCurrencyHolding GetSendingHoldingForTransaction(CryptoCurrencyTransaction transaction)
    {
        if(transaction.SentAmount == null)
            throw new ArgumentException("Transaction has no sent amount.");

        var sender = _holdings.SingleOrDefault(h => h.Asset == transaction.SentAmount.CurrencyCode);
        if (sender == null)
        {
            sender = new CryptoCurrencyHolding(transaction.SentAmount.CurrencyCode);  
            _holdings.Add(sender);
        }
        return sender;
    }

    internal void CalculateAverageBuyingCost(IEnumerable<CryptoCurrencyHolding> holdings)
    {

    }
}