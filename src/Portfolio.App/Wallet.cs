using Portfolio.Shared;

namespace Portfolio.App
{
    public class Wallet
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ICryptoCurrencyTransaction> Transactions { get; set; } = null!;

        private Wallet() {}

        public static Result<Wallet> Create(string walletName, IEnumerable<ICryptoCurrencyTransaction> transactions)
        {
            if(string.IsNullOrWhiteSpace(walletName)) return Result.Failure<Wallet>("Name cannot be empty.");
        
            return new Wallet 
            {
                Name = walletName,
                Transactions = transactions ?? new List<ICryptoCurrencyTransaction>()
            };
        }
    }
}
