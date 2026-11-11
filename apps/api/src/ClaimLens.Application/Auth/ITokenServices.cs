using ClaimLens.Domain.Users;

namespace ClaimLens.Application.Auth;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    bool Verify(User user, string password);
}

public interface IAccessTokenIssuer
{
    string CreateAccessToken(User user);

    IssuedRefreshToken CreateRefreshToken(DateTimeOffset now);
}

public sealed record IssuedRefreshToken(string Plaintext, string TokenHash, DateTimeOffset ExpiresAt);
