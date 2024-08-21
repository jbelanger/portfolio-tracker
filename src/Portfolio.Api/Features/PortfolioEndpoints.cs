using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure;

namespace Portfolio.Api.Features
{
    public static class PortfolioEndpoints
    {
        public static void MapPortfolioEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/portfolios");

            group.MapPost("/", async (PortfolioDbContext dbContext, UserPortfolio portfolio) =>
            {
                dbContext.Portfolios.Add(portfolio);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/portfolios/{portfolio.Id}", portfolio);
            });

            group.MapGet("/{id:long}", async (PortfolioDbContext dbContext, long id) =>
            {
                var portfolio = await dbContext.Portfolios
                    .Include(p => p.Wallets)
                    .Include(p => p.Holdings)
                    .Include(p => p.ProcessedTransactions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                return portfolio is not null ? Results.Ok(portfolio) : Results.NotFound();
            });

            group.MapGet("/", async (PortfolioDbContext dbContext) =>
            {
                var portfolios = await dbContext.Portfolios
                    .Include(p => p.Wallets)
                    .Include(p => p.Holdings)
                    .Include(p => p.ProcessedTransactions)
                    .ToListAsync();

                return Results.Ok(portfolios);
            });

            group.MapPut("/{id:long}", async (PortfolioDbContext dbContext, long id, UserPortfolio updatedPortfolio) =>
            {
                var portfolio = await dbContext.Portfolios.FindAsync(id);

                if (portfolio is null)
                {
                    return Results.NotFound();
                }

                dbContext.Entry(portfolio).CurrentValues.SetValues(updatedPortfolio);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });

            group.MapDelete("/{id:long}", async (PortfolioDbContext dbContext, long id) =>
            {
                var portfolio = await dbContext.Portfolios.FindAsync(id);

                if (portfolio is null)
                {
                    return Results.NotFound();
                }

                dbContext.Portfolios.Remove(portfolio);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });

            group.MapPost("/{id:long}/calculate-trades", async (PortfolioDbContext dbContext, long id) =>
            {
                var portfolio = await dbContext.Portfolios
                    .Include(p => p.Wallets)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (portfolio is null)
                {
                    return Results.NotFound();
                }

                await portfolio.CalculateTradesAsync();
                await dbContext.SaveChangesAsync();

                return Results.Ok(portfolio);
            });
        }
    }
}
