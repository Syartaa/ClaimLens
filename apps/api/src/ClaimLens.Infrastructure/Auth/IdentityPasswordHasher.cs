using ClaimLens.Application.Auth;
using ClaimLens.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace ClaimLens.Infrastructure.Auth;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(User user, string password)
    {
        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return false;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = Hash(user, password);
        }

        return true;
    }
}
