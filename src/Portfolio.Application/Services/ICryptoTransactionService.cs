using Portfolio.App;
using Portfolio.App.DTOs;

namespace Portfolio.Api.Services
{
    public interface ICryptoTransactionService
    {
        Task<Result<long>> AddTransactionAsync(long portfolioId, long walletId, TransactionDto transactionDto);
        Task<Result> UpdateTransactionAsync(long portfolioId, long walletId, long transactionId, TransactionDto transactionDto);
        Task<Result> DeleteTransactionAsync(long portfolioId, long walletId, long transactionId);
        Task<Result<TransactionDto>> GetTransactionAsync(long portfolioId, long walletId, long transactionId);
        Task<Result<IEnumerable<TransactionDto>>> GetTransactionsAsync(long portfolioId, long walletId);
        Task<Result> ImportTransactionsFromCsvAsync(long portfolioId, long walletId, CsvFileImportType csvType, StreamReader streamReader);
        Task<Result> BulkUpdateTransactionsAsync(long portfolioId, long walletId, List<TransactionDto> transactionsToUpdate);
        Task<Result> BulkDeleteTransactionsAsync(long portfolioId, long walletId, long[] transactionIds);
    }
}
