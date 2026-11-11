using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace ClaimLens.Api.IntegrationTests;

public class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public int AuthPermitLimit { get; set; } = 30;

    public async Task InitializeAsync() => await _database.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-test-signing-key-32-characters");
        builder.UseSetting("RateLimiting:AuthPermitLimit", AuthPermitLimit.ToString());
        builder.UseEnvironment("Development");
    }
}

public class AuthEndpointTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public AuthEndpointTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_login_and_me_round_trip()
    {
        var registered = await RegisterAsync("ada@example.com");

        var me = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/auth/me", registered.AccessToken));
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<UserBody>(JsonOptions);
        Assert.Equal("ada@example.com", profile!.Email);
        Assert.Equal("User", profile.Role);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "ada@example.com",
            password = "WrongPass1"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected()
    {
        await RegisterAsync("grace@example.com");
        var duplicate = await _client.PostAsJsonAsync("/api/auth/register", Sample("grace@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var error = await duplicate.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions);
        Assert.Equal("EMAIL_ALREADY_REGISTERED", error!.Code);
    }

    [Fact]
    public async Task Reused_refresh_token_revokes_the_replacement()
    {
        var registered = await RegisterAsync("linus@example.com");
        var rotated = await RefreshAsync(registered.RefreshToken);

        var reused = await RefreshRawAsync(registered.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);

        var replacement = await RefreshRawAsync(rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replacement.StatusCode);
    }

    private async Task<AuthBody> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", Sample(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthBody>(JsonOptions))!;
    }

    private async Task<AuthBody> RefreshAsync(string refreshToken)
    {
        var response = await RefreshRawAsync(refreshToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthBody>(JsonOptions))!;
    }

    private Task<HttpResponseMessage> RefreshRawAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    private static object Sample(string email) => new
    {
        email,
        password = "CorrectHorse1",
        firstName = "Ada",
        lastName = "Lovelace"
    };

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private sealed record AuthBody(string AccessToken, string RefreshToken, UserBody User);
    private sealed record UserBody(Guid Id, string Email, string FirstName, string LastName, string Role);
    private sealed record ErrorBody(string Code, string Message);
}
