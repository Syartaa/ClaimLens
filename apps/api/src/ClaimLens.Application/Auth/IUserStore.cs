using ClaimLens.Domain.Auth;
using ClaimLens.Domain.Users;

namespace ClaimLens.Application.Auth;

public interface IUserStore
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken);

    Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task RevokeActiveTokensAsync(Guid userId, DateTimeOffset revokedAt, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
