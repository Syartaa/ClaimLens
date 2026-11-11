using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimLens.Domain.Users;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Api.IntegrationTests;

public class AuthorizationMatrixTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationMatrixTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Each_role_receives_the_status_its_permission_allows()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var owner = await RegisterAsync(ownerEmail);
        var created = await CreateAsync(owner);
        var other = await RegisterAsync($"other-{Guid.NewGuid():N}@example.com");
        var reviewer = await RegisterAndPromoteAsync(UserRole.Reviewer);
        var admin = await RegisterAndPromoteAsync(UserRole.Admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, $"/api/investigations/{created}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, "/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/health")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"/api/investigations/{created}", other)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Delete, $"/api/investigations/{created}", other)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Post, $"/api/investigations/{created}/reanalyze", other)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/reviews/queue", other)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/admin/dashboard", other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, $"/api/investigations/{created}", owner)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, $"/api/investigations/{created}", reviewer)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/reviews/queue", reviewer)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Delete, $"/api/investigations/{created}", reviewer)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/admin/users", reviewer)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, $"/api/investigations/{created}", admin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/admin/dashboard", admin)).StatusCode);
        var restarted = await Send(HttpMethod.Post, $"/api/investigations/{created}/reanalyze", admin);
        Assert.Equal(HttpStatusCode.Accepted, restarted.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Delete, $"/api/investigations/{created}", admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"/api/investigations/{created}", owner)).StatusCode);
    }

    private async Task<string> RegisterAndPromoteAsync(UserRole role)
    {
        var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimLensDbContext>();
        var user = await db.Users.SingleAsync(item => item.Email == email);
        user.Role = role;
        await db.SaveChangesAsync();
        return await LoginAsync(email);
    }

    private async Task<string> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "CorrectHorse1",
            firstName = "Ada",
            lastName = "Lovelace"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthBody>(JsonOptions);
        return body!.AccessToken;
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "CorrectHorse1"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthBody>(JsonOptions);
        return body!.AccessToken;
    }

    private async Task<Guid> CreateAsync(string token)
    {
        var response = await Send(HttpMethod.Post, "/api/investigations", token, new { inputType = "Claim", text = "A private claim." });
        var body = await response.Content.ReadFromJsonAsync<CreatedBody>(JsonOptions);
        return body!.Id;
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? accessToken = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private sealed record AuthBody(string AccessToken);
    private sealed record CreatedBody(Guid Id);
}
