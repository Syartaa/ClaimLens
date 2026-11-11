using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClaimLens.Api.IntegrationTests;

public class HealthEndpointTests : IClassFixture<TestingApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TestingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthBody>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
        Assert.Equal("claimlens-api", body.Service);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    private sealed record HealthBody(string Status, string Service);
}
