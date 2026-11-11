using ClaimLens.Application.Auth;
using ClaimLens.Domain.Auth;
using ClaimLens.Domain.Users;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClaimLens.Infrastructure.Auth;

public sealed class EfUserStore : IUserStore
{
    private readonly ClaimLensDbContext _db;

    public EfUserStore(ClaimLensDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        _db.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        _db.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _db.RefreshTokens.Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeActiveTokensAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        var active = await _db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = revokedAt;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
