using Portfolio.App;
using Portfolio.App.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;
using Portfolio.Infrastructure;
using Portfolio.Transactions.Importers.Csv.Kraken;

namespace Portfolio.Api.Services
{
    public partial class CryptoTransactionService : ICryptoTransactionService
    {
        private readonly PortfolioDbContext _dbContext;

        public CryptoTransactionService(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<long>> AddTransactionAsync(long portfolioId, long walletId, CryptoCurrencyTransactionDto transactionDto)
        {
            var wallet = await _dbContext.Wallets                
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
                return Result.Failure<long>($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");

            if (!Enum.TryParse(transactionDto.Type, true, out TransactionType transactionType))
                return Result.Failure<long>($"Invalid transaction type {transactionDto.Type}");

            Result<CryptoCurrencyRawTransaction> addTransactionResult = new Result<CryptoCurrencyRawTransaction>();
            switch (transactionType)
            {
                case TransactionType.Deposit:
                    addTransactionResult = CryptoCurrencyRawTransaction.CreateDeposit(
                        transactionDto.DateTime,
                        new Money(transactionDto.ReceivedAmount ?? 0m, transactionDto.ReceivedCurrency),
                        transactionDto.FeeAmount.HasValue ? new Money(transactionDto.FeeAmount ?? 0m, transactionDto.FeeCurrency) : null,
                        transactionDto.Account,
                        [],
                        transactionDto.Note
                    );
                    break;
                case TransactionType.Withdrawal:
                    addTransactionResult = CryptoCurrencyRawTransaction.CreateWithdraw(
                        transactionDto.DateTime,
                        new Money(transactionDto.SentAmount.GetValueOrDefault(), transactionDto.SentCurrency),
                        transactionDto.FeeAmount.HasValue ? new Money(transactionDto.FeeAmount.Value, transactionDto.FeeCurrency) : null,
                        transactionDto.Account,
                        [],
                        transactionDto.Note
                    );
                    break;
                case TransactionType.Trade:
                    addTransactionResult = CryptoCurrencyRawTransaction.CreateTrade(
                        transactionDto.DateTime,
                        new Money(transactionDto.ReceivedAmount.GetValueOrDefault(), transactionDto.ReceivedCurrency),
                        new Money(transactionDto.SentAmount.GetValueOrDefault(), transactionDto.SentCurrency),
                        transactionDto.FeeAmount.HasValue ? new Money(transactionDto.FeeAmount.Value, "USD") : null,
                        transactionDto.Account,
                        [],
                        transactionDto.Note
                    );
                    break;
            }

            return await addTransactionResult
                .Check(t => EnsureTransactionNotAlreadyExistsAsync(walletId, t))
                .Check(wallet.AddTransaction)
                .Tap(async t => await _dbContext.SaveChangesAsync())
                .Map(t => t.Id);
        }

        public async Task<Result> UpdateTransactionAsync(long portfolioId, long walletId, long transactionId, CryptoCurrencyTransactionDto transactionDto)
        {
            var isUserWallet = await _dbContext.Wallets
                .AsNoTracking()
                .AnyAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (!isUserWallet)
                return Result.Failure<CryptoCurrencyTransactionDto>($"Transaction with ID {transactionId} not found in Wallet {walletId}.");

            var transaction = _dbContext.RawTransactions.AsNoTracking().FirstOrDefault(t => t.Id == transactionId);
            if (transaction == null)
                return Result.Failure($"Transaction with ID {transactionId} not found in Wallet {walletId}.");

            var receivedAmount = Money.Create(transactionDto.ReceivedAmount, transactionDto.ReceivedCurrency);
            var sentAmount = Money.Create(transactionDto.SentAmount, transactionDto.SentCurrency);
            var feeAmount = Money.Create(transactionDto.FeeAmount, transactionDto.FeeCurrency);

            return await transaction.SetTransactionAmounts(receivedAmount.GetValueOrDefault(), sentAmount.GetValueOrDefault(), feeAmount.GetValueOrDefault())
                .Bind(() => transaction.SetNote(transactionDto.Note))
                .Bind(() => transaction.SetTransactionDate(transactionDto.DateTime))
                .Tap(async () => await _dbContext.SaveChangesAsync());
        }

        public async Task<Result> BulkUpdateTransactionsAsync(long portfolioId, long walletId, List<CryptoCurrencyTransactionDto> transactionsToUpdate)
        {
            var wallet = await _dbContext.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
                return Result.Failure<IEnumerable<CryptoCurrencyTransactionDto>>($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");


            var transactionIds = transactionsToUpdate.Select(t => t.Id).ToArray();
            var transactions = await _dbContext.RawTransactions
                                             .Where(t => transactionIds.Contains(t.Id))
                                             .ToListAsync();

            if (transactions.Count != transactionIds.Length)
                return Result.Failure("Some transactions could not be found.");

            foreach (var transaction in transactions)
            {
                var updatedTransaction = transactionsToUpdate.First(t => t.Id == transaction.Id);
                var receivedAmount = Money.Create(updatedTransaction.ReceivedAmount, updatedTransaction.ReceivedCurrency);
                var sentAmount = Money.Create(updatedTransaction.SentAmount, updatedTransaction.SentCurrency);
                var feeAmount = Money.Create(updatedTransaction.FeeAmount, updatedTransaction.FeeCurrency);

                var result = transaction.SetTransactionAmounts(receivedAmount.GetValueOrDefault(), sentAmount.GetValueOrDefault(), feeAmount.GetValueOrDefault())
                    .Bind(() => transaction.SetNote(updatedTransaction.Note))
                    .Bind(() => transaction.SetTransactionDate(updatedTransaction.DateTime));
            }

            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteTransactionAsync(long portfolioId, long walletId, long transactionId)
        {
            var isUserWallet = await _dbContext.Wallets                
                .AnyAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (!isUserWallet)
                return Result.Failure<CryptoCurrencyTransactionDto>($"Transaction with ID {transactionId} not found in Wallet {walletId}.");

            var transaction = _dbContext.RawTransactions.AsNoTracking().FirstOrDefault(t => t.Id == transactionId);
            if (transaction == null)
            {
                return Result.Failure($"Transaction with ID {transactionId} not found in Wallet {walletId}.");
            }

            //_dbContext.Entry(transaction).State = EntityState.Deleted;
            _dbContext.RawTransactions.Remove(transaction);

            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> BulkDeleteTransactionsAsync(long portfolioId, long walletId, long[] transactionIds)
        {
            var wallet = await _dbContext.Wallets            
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
                return Result.Failure<IEnumerable<CryptoCurrencyTransactionDto>>($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");

            var transactions = await _dbContext.RawTransactions
                                             .Where(t => transactionIds.Contains(t.Id))
                                             .ToListAsync();

            if (transactions.Count != transactionIds.Length)
                return Result.Failure("Some transactions could not be found.");
                        
            _dbContext.RawTransactions.RemoveRange(transactions);
            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }


        public async Task<Result<IEnumerable<CryptoCurrencyTransactionDto>>> GetTransactionsAsync(long portfolioId, long walletId)
        {
            var wallet = await _dbContext.Wallets
                .Include(w => w.Transactions)
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
                return Result.Failure<IEnumerable<CryptoCurrencyTransactionDto>>($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");

            var transactionsDto = wallet.Transactions.Select(t => CryptoCurrencyTransactionDto.From(t));

            return Result.Success(transactionsDto);
        }

        public async Task<Result<CryptoCurrencyTransactionDto>> GetTransactionAsync(long portfolioId, long walletId, long transactionId)
        {
            var isUserWallet = await _dbContext
                .Wallets
                .AsNoTracking()
                .AnyAsync(w => w.Id == walletId && w.PortfolioId == portfolioId && w.Transactions.Any(t => t.Id == transactionId));

            if (!isUserWallet)
                return Result.Failure<CryptoCurrencyTransactionDto>($"Transaction with ID {transactionId} not found in Wallet {walletId}.");

            var transaction = await _dbContext.RawTransactions.AsNoTracking().FirstAsync(t => t.Id == transactionId);

            return Result.Success(CryptoCurrencyTransactionDto.From(transaction));
        }

        public async Task<Result> ImportTransactionsFromCsvAsync(long portfolioId, long walletId, CsvFileImportType csvType, StreamReader streamReader)
        {
            var wallet = await _dbContext
                .Wallets                
                .FirstOrDefaultAsync(w => w.Id == walletId && w.PortfolioId == portfolioId);

            if (wallet == null)
                return Result.Failure($"Wallet with ID {walletId} not found in Portfolio {portfolioId}.");

            IEnumerable<CryptoCurrencyRawTransaction>? transactions = null;

            try
            {
                switch (csvType)
                {
                    case CsvFileImportType.Standard:
                        break;
                    case CsvFileImportType.Kraken:
                        var parserResult = KrakenCsvParser.Create(streamReader);
                        if (parserResult.IsFailure)
                            return Result.Failure(parserResult.Error);

                        transactions = parserResult.Value.ExtractTransactions();
                        break;
                }
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to import transactions from CSV: {ex.Message}");
            }

            if (transactions?.Any() == true)
            {
                foreach (var transaction in transactions)
                {
                    // TODO: See if checking record existence has performance drawbacks...
                    var addTransactionResult = await EnsureTransactionNotAlreadyExistsAsync(walletId, transaction)
                        .Bind(() => wallet.AddTransaction(transaction));
                        //.Tap(() => _dbContext.Entry(transaction).State = EntityState.Added);

                    if (addTransactionResult.IsFailure)
                        return Result.Failure(addTransactionResult.Error);
                }
            }
            else
                return Result.Failure("No transactions to import from CSV.");

            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        private async Task<Result> EnsureTransactionNotAlreadyExistsAsync(long walletId, CryptoCurrencyRawTransaction n)
        {
            decimal received = n.ReceivedAmount.Amount;// ?? null;
            decimal sent = n.SentAmount.Amount;// ?? null;
            decimal fee = n.FeeAmount.Amount;// ?? null;
            string receivedCurrency = n.ReceivedAmount.CurrencyCode;// ?? null;
            string sentCurrency = n.SentAmount.CurrencyCode;// ?? null;
            string feeCurrency = n.FeeAmount.CurrencyCode;// ?? null;

            var exists = await _dbContext.RawTransactions.AsNoTracking().AnyAsync(other =>
                walletId == other.WalletId &&
                n.DateTime == other.DateTime &&
                n.Type == other.Type &&
                received == other.ReceivedAmount.Amount &&
                sent == other.SentAmount.Amount &&
                fee == other.FeeAmount.Amount &&
                receivedCurrency == other.ReceivedAmount.CurrencyCode &&
                sentCurrency == other.SentAmount.CurrencyCode &&
                feeCurrency == other.FeeAmount.CurrencyCode
                );
            if (exists)
                return Result.Failure<long>("Transaction already exists in this wallet.");
            return Result.Success();
        }
    }
}
