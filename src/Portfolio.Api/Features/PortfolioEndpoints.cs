using Microsoft.EntityFrameworkCore;
using Portfolio.App.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Infrastructure;

namespace Portfolio.Api.Features
{
    public static class PortfolioEndpoints
    {
        public static void MapPortfolioEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/portfolios");

            group.MapPost("/", async (PortfolioDbContext dbContext, PortfolioDto portfolio) =>
            {
                var up = new UserPortfolio();
                dbContext.Portfolios.Add(up);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/portfolios/{up.Id}", portfolio);
            });

            group.MapGet("/{id:long}", async (PortfolioDbContext dbContext, long id) =>
            {
                var portfolio = await dbContext.Portfolios
                    .Include(p => p.Wallets)
                    .Include(p => p.Holdings)
                    .Include("Wallets.Transactions")
                    .FirstOrDefaultAsync(p => p.Id == id);

                return portfolio is not null ? Results.Ok(portfolio) : Results.NotFound();
            });

            group.MapGet("/", async (PortfolioDbContext dbContext) =>
            {
                var portfolios = await dbContext.Portfolios
                    .Include(p => p.Wallets)
                    .Include(p => p.Holdings)
                    .Include("Wallets.Transactions")
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

            group.MapPost("/{id:long}/calculate-trades", async (PortfolioDbContext dbContext, IPriceHistoryService priceHistoryService, long id) =>
            {
                var portfolio = await dbContext.Portfolios
                    .Include(p => p.Holdings)
                    .Include("Wallets.Transactions")
                    .Include(p => p.FinancialEvents)                    
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (portfolio is null)
                {
                    return Results.NotFound();
                }

                await portfolio.CalculateTradesAsync(priceHistoryService);
                await dbContext.SaveChangesAsync();

                return Results.Ok(portfolio);
            });
        }
    }
}
