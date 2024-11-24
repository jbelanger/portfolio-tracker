using Portfolio.Domain.Entities;

namespace Portfolio.App.DTOs
{
    public class WalletDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<FinancialTransactionDto> Transactions { get; set; } = new();

        public static WalletDto From(Wallet wallet)
        {
            return new WalletDto
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Transactions = wallet.Transactions.Select(FinancialTransactionDto.From).ToList()
            };
        }
    }
}
