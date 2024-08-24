using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Services;
using Portfolio.App;
using Portfolio.App.DTOs;

namespace Portfolio.Api.Features
{
    public static class TransactionEndpoints
    {
        public static void MapTransactionEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/portfolios/{portfolioId:long}/wallets/{walletId:long}/transactions");

            group.MapPost("/", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, CryptoCurrencyTransactionDto transactionDto) =>
            {
                var result = await transactionService.AddTransactionAsync(portfolioId, walletId, transactionDto);

                if (result.IsSuccess)
                {
                    return Results.Created($"/portfolios/{portfolioId}/wallets/{walletId}/transactions/{result.Value}", result.Value);
                }
                return Results.BadRequest(result.Error);
            });

            group.MapPut("/{transactionId:long}", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, long transactionId, CryptoCurrencyTransactionDto transactionDto) =>
            {
                var result = await transactionService.UpdateTransactionAsync(portfolioId, walletId, transactionId, transactionDto);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }
                return Results.BadRequest(result.Error);
            });

            group.MapPut("/bulk-edit", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, [FromBody] List<CryptoCurrencyTransactionDto> transactionsToUpdate) =>
            {
                var result = await transactionService.BulkUpdateTransactionsAsync(portfolioId, walletId, transactionsToUpdate);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }
                return Results.BadRequest(result.Error);
            });


            group.MapDelete("/{transactionId:long}", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, long transactionId) =>
            {
                var result = await transactionService.DeleteTransactionAsync(portfolioId, walletId, transactionId);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }
                return Results.BadRequest(result.Error);
            });

            group.MapDelete("/bulk-delete", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, [FromBody] long[] transactionIds) =>
            {
                var result = await transactionService.BulkDeleteTransactionsAsync(portfolioId, walletId, transactionIds);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }
                return Results.BadRequest(result.Error);
            });


            group.MapGet("/", async (ICryptoTransactionService transactionService, long portfolioId, long walletId) =>
            {
                var result = await transactionService.GetTransactionsAsync(portfolioId, walletId);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });

            group.MapGet("/{transactionId:long}", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, long transactionId) =>
            {
                var result = await transactionService.GetTransactionAsync(portfolioId, walletId, transactionId);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });

            group.MapPost("/upload-csv", async (ICryptoTransactionService transactionService, long portfolioId, long walletId, CsvFileImportType csvImportType, IFormFile file) =>
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var result = await transactionService.ImportTransactionsFromCsvAsync(portfolioId, walletId, App.CsvFileImportType.Kraken, reader);
                    if (result.IsSuccess)
                    {
                        return Results.Ok("Transactions imported successfully.");
                    }
                    return Results.BadRequest(result.Error);
                }
            })
#if DEBUG
            .DisableAntiforgery();
#endif
            ;
        }
    }
}
