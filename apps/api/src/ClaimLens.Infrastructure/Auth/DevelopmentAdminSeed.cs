using ClaimLens.Application.Auth;
using ClaimLens.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Infrastructure.Auth;

public static class DevelopmentAdminSeed
{
    public const string Email = "admin@claimlens.test";

    public const string Password = "ChangeMe!Admin1";

    public static async Task EnsureAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var users = services.GetRequiredService<IUserStore>();
        if (await users.FindByEmailAsync(Email, cancellationToken) is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = Email,
            FirstName = "Admin",
            LastName = "Desk",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = services.GetRequiredService<IPasswordHasher>().Hash(user, Password);
        await users.AddUserAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
    }
}
