using Portfolio.App;
using Portfolio.App.DTOs;

namespace Portfolio.Api.Services
{
    public interface ICryptoTransactionService
    {
        Task<Result<long>> AddTransactionAsync(long portfolioId, long walletId, CryptoCurrencyTransactionDto transactionDto);
        Task<Result> UpdateTransactionAsync(long portfolioId, long walletId, long transactionId, CryptoCurrencyTransactionDto transactionDto);
        Task<Result> DeleteTransactionAsync(long portfolioId, long walletId, long transactionId);
        Task<Result<CryptoCurrencyTransactionDto>> GetTransactionAsync(long portfolioId, long walletId, long transactionId);
        Task<Result<IEnumerable<CryptoCurrencyTransactionDto>>> GetTransactionsAsync(long portfolioId, long walletId);
        Task<Result> ImportTransactionsFromCsvAsync(long portfolioId, long walletId, CsvFileImportType csvType, StreamReader streamReader);
        Task<Result> BulkUpdateTransactionsAsync(long portfolioId, long walletId, List<CryptoCurrencyTransactionDto> transactionsToUpdate);
        Task<Result> BulkDeleteTransactionsAsync(long portfolioId, long walletId, long[] transactionIds);
    }
}
