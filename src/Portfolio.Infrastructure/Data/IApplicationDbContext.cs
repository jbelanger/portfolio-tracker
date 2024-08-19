namespace Portfolio.App.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
