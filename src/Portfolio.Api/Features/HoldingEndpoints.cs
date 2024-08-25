using Portfolio.App.Services;

namespace Portfolio.Api.Features
{
    public static class HoldingEndpoints
    {
        public static void MapHoldingEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/portfolios/{portfolioId:long}/holdings");

            group.MapGet("/", async (IHoldingService holdingService, long portfolioId) =>
            {
                var result = await holdingService.GetHoldingsAsync(portfolioId);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });

            group.MapGet("/{holdingId:long}", async (IHoldingService holdingService, long portfolioId, long holdingId) =>
            {
                var result = await holdingService.GetHoldingAsync(portfolioId, holdingId);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }
                return Results.NotFound(result.Error);
            });
        }
    }
}
