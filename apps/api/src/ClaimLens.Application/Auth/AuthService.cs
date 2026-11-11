using ClaimLens.Application.Common;
using ClaimLens.Domain.Auth;
using ClaimLens.Domain.Users;
using FluentValidation;

namespace ClaimLens.Application.Auth;

public sealed class AuthService
{
    private readonly IUserStore _users;
    private readonly IPasswordHasher _passwords;
    private readonly IAccessTokenIssuer _tokens;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly TimeProvider _clock;

    public AuthService(
        IUserStore users,
        IPasswordHasher passwords,
        IAccessTokenIssuer tokens,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshRequest> refreshValidator,
        IValidator<LogoutRequest> logoutValidator,
        TimeProvider clock)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        if (await _users.FindByEmailAsync(email, cancellationToken) is not null)
        {
            throw new AppException("EMAIL_ALREADY_REGISTERED", 409, "An account with this email already exists.");
        }

        var now = _clock.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.PasswordHash = _passwords.Hash(user, request.Password);

        await _users.AddUserAsync(user, cancellationToken);
        var response = await IssueAsync(user, now, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        var user = await _users.FindByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null || !user.IsActive || !_passwords.Verify(user, request.Password))
        {
            throw new AppException("INVALID_CREDENTIALS", 401, "Email or password is incorrect.");
        }

        var response = await IssueAsync(user, _clock.GetUtcNow(), cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_refreshValidator, request, cancellationToken);

        var now = _clock.GetUtcNow();
        var existing = await FindUsableTokenAsync(request.RefreshToken, now, cancellationToken);
        if (existing.RevokedAt is not null)
        {
            await _users.RevokeActiveTokensAsync(existing.UserId, now, cancellationToken);
            await _users.SaveChangesAsync(cancellationToken);
            throw new AppException("REFRESH_TOKEN_INVALID", 401, "The refresh token is no longer valid.");
        }

        var user = existing.User;
        if (!user.IsActive)
        {
            throw new AppException("REFRESH_TOKEN_INVALID", 401, "The refresh token is no longer valid.");
        }

        var issued = _tokens.CreateRefreshToken(now);
        var replacement = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = issued.TokenHash,
            ExpiresAt = issued.ExpiresAt,
            CreatedAt = now
        };

        existing.RevokedAt = now;
        existing.ReplacedByTokenId = replacement.Id;
        await _users.AddRefreshTokenAsync(replacement, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        return new AuthResponse(_tokens.CreateAccessToken(user), issued.Plaintext, ToResponse(user));
    }

    public async Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(_logoutValidator, request, cancellationToken);

        var existing = await _users.FindRefreshTokenByHashAsync(TokenHashing.Hash(request.RefreshToken), cancellationToken);
        if (existing is null || existing.UserId != userId)
        {
            throw new AppException("REFRESH_TOKEN_INVALID", 401, "The refresh token is no longer valid.");
        }

        if (existing.RevokedAt is null)
        {
            existing.RevokedAt = _clock.GetUtcNow();
            await _users.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AppException("UNAUTHORIZED", 401, "Sign in is required.");
        }

        return ToResponse(user);
    }

    private async Task<AuthResponse> IssueAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var issued = _tokens.CreateRefreshToken(now);
        await _users.AddRefreshTokenAsync(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = issued.TokenHash,
            ExpiresAt = issued.ExpiresAt,
            CreatedAt = now
        }, cancellationToken);

        return new AuthResponse(_tokens.CreateAccessToken(user), issued.Plaintext, ToResponse(user));
    }

    private async Task<RefreshToken> FindUsableTokenAsync(string plaintext, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _users.FindRefreshTokenByHashAsync(TokenHashing.Hash(plaintext), cancellationToken);
        if (existing is null || existing.ExpiresAt <= now)
        {
            throw new AppException("REFRESH_TOKEN_INVALID", 401, "The refresh token is no longer valid.");
        }

        return existing;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var details = result.Errors
            .Select(error => new FieldError(error.PropertyName, error.ErrorMessage))
            .ToArray();
        throw new ValidationAppException(details);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString());
}

public static class TokenHashing
{
    public static string Hash(string plaintext)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(bytes);
    }
}
