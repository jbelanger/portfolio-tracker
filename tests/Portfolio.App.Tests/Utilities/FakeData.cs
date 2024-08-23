using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.App.Tests.Utilities;

public class FakeDataHelper
{
    public static UserPortfolio AddPortfolio(DbContext dbContext)
    {
        UserPortfolio userPortfolio = new UserPortfolio();
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