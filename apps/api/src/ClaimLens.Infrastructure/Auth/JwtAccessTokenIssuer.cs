using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClaimLens.Application.Auth;
using ClaimLens.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClaimLens.Infrastructure.Auth;

public sealed class JwtSettings
{
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "claimlens";

    public string Audience { get; set; } = "claimlens-web";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtSettings _settings;

    public JwtAccessTokenIssuer(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string CreateAccessToken(User user)
    {
        var credentials = new SigningCredentials(SigningKey(_settings.Secret), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("role", user.Role.ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public IssuedRefreshToken CreateRefreshToken(DateTimeOffset now)
    {
        var plaintext = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return new IssuedRefreshToken(plaintext, TokenHashing.Hash(plaintext), now.AddDays(_settings.RefreshTokenDays));
    }

    public static SymmetricSecurityKey SigningKey(string secret) =>
        new(Encoding.UTF8.GetBytes(secret));
}
