using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClaimLens.Infrastructure.Persistence;

public sealed class ClaimLensDbContextFactory : IDesignTimeDbContextFactory<ClaimLensDbContext>
{
    public ClaimLensDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClaimLensDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=claimlens;Username=claimlens;Password=claimlens")
            .Options;
        return new ClaimLensDbContext(options);
    }
}
