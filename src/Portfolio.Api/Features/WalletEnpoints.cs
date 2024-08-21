using Portfolio.App.DTOs;
using Portfolio.App.Services;

namespace Portfolio.Api.Features
{
    public static class WalletEndpoints
    {
        public static void MapWalletEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/portfolios/{portfolioId:long}/wallets");

            group.MapGet("/{walletId:long}", async (IWalletService walletService, long portfolioId, long walletId) =>
            {
                var result = await walletService.GetWalletAsync(portfolioId, walletId);
                
                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });

            group.MapGet("/", async (IWalletService walletService, long portfolioId) =>
            {
                var result = await walletService.GetWalletsAsync(portfolioId);
                
                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });

            group.MapPost("/", async (IWalletService walletService, long portfolioId, WalletDto walletDto) =>
            {
                var result = await walletService.CreateWalletAsync(portfolioId, walletDto);

                if (result.IsSuccess)
                {
                    return Results.Created($"/portfolios/{portfolioId}/wallets/{result.Value}", result.Value);
                }
                return Results.BadRequest(result.Error);
            });

            group.MapDelete("/{walletId:long}", async (IWalletService walletService, long portfolioId, long walletId) =>
            {
                var result = await walletService.DeleteWalletAsync(portfolioId, walletId);

                if (result.IsSuccess)
                {
                    return Results.NoContent();
                }
                return Results.BadRequest(result.Error);
            });
        }


    }
}
