using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Portfolio.Infrastructure;

namespace Portfolio.App.Tests.Utilities;

public class DbContextUtilities
{
    public static PortfolioDbContext GetMemoryDbContext()
    {
        IMediator mediator = new Mock<IMediator>().Object;
        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
            .Options;
        return new PortfolioDbContext(options, mediator);
    }
}