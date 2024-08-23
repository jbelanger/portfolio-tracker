using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.App.DTOs
{
    public class CryptoCurrencyTransactionDto
    {
        public long Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal? ReceivedAmount { get; set; }
        public string ReceivedCurrency { get; set; } = string.Empty;
        public decimal? SentAmount { get; set; }
        public string SentCurrency { get; set; } = string.Empty;
        public decimal? FeeAmount { get; set; }
        public string FeeCurrency { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        public static CryptoCurrencyTransactionDto From(CryptoCurrencyRawTransaction transaction)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));

            return new CryptoCurrencyTransactionDto
            {
                Id = transaction.Id,
                DateTime = transaction.DateTime,
                Type = transaction.Type.ToString(),
                ReceivedAmount = (transaction.ReceivedAmount == Money.Empty) ? null : transaction.ReceivedAmount.Amount,
                ReceivedCurrency = transaction.ReceivedAmount?.CurrencyCode ?? string.Empty,
                SentAmount = (transaction.SentAmount == Money.Empty) ? null : transaction.SentAmount.Amount,
                SentCurrency = transaction.SentAmount?.CurrencyCode ?? string.Empty,
                FeeAmount = (transaction.FeeAmount == Money.Empty) ? null : transaction.FeeAmount.Amount,
                FeeCurrency = transaction.FeeAmount?.CurrencyCode ?? string.Empty,
                Account = transaction.Account,
                Note = transaction.Note
            };
        }
    }
}
