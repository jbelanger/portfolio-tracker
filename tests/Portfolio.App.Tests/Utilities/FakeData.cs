using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure;

namespace Portfolio.App.Tests.Utilities;

public class FakeDataHelper
{
    public static UserPortfolio AddPortfolio(DbContext dbContext)
    {
        UserPortfolio userPortfolio = UserPortfolio.Create().Value;
        userPortfolio.Id = 1;
        dbContext.Add(userPortfolio);        
        return userPortfolio;
    }

    public static Wallet AddWallet(DbContext dbContext, UserPortfolio portfolio)
    {
        Wallet wallet = Wallet.Create("test wallet").Value;
        wallet.Id = 1;
        portfolio.AddWallet(wallet);      
        return wallet;  
    }
}