using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClaimLens.Api.IntegrationTests;

public sealed class TightAuthApiFactory : AuthApiFactory
{
    public TightAuthApiFactory()
    {
        AuthPermitLimit = 2;
    }
}

public class RateLimitTests : IClassFixture<TightAuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public RateLimitTests(TightAuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_returns_429_after_the_permit_is_used()
    {
        var body = new { email = "nobody@example.com", password = "WrongPass1" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/login", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/auth/login", body)).StatusCode);

        var limited = await _client.PostAsJsonAsync("/api/auth/login", body);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        var error = await limited.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions);
        Assert.Equal("RATE_LIMITED", error!.Code);
    }

    private sealed record ErrorBody(string Code);
}
