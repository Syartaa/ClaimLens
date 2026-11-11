using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Users;
using ClaimLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Api.IntegrationTests;

public class AdminEndpointTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public AdminEndpointTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_can_see_users_failures_and_provider_usage()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var ownerToken = await RegisterAsync(ownerEmail);
        var hidden = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/dashboard", ownerToken));
        Assert.Equal(HttpStatusCode.Forbidden, hidden.StatusCode);

        var created = await CreateAsync(ownerToken, "A run the admin should see.");
        await MarkFailedAsync(created.Id);

        var reviewerEmail = $"reviewer-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(reviewerEmail);
        await SetRoleAsync(reviewerEmail, UserRole.Reviewer);
        var reviewer = await LoginAsync(reviewerEmail);
        var reviewerDenied = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/users", reviewer));
        Assert.Equal(HttpStatusCode.Forbidden, reviewerDenied.StatusCode);

        var adminEmail = $"admin-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(adminEmail);
        await SetRoleAsync(adminEmail, UserRole.Admin);
        var admin = await LoginAsync(adminEmail);

        var dashboardResponse = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/dashboard", admin));
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardBody>(JsonOptions);
        Assert.True(dashboard!.Failed >= 1);
        Assert.True(dashboard.AiRunFailures >= 1);
        Assert.Contains(dashboard.FailureCodes, item => item.Label == "CANCELLED");
        Assert.Contains(dashboard.Providers, item => item.Label == "mock");
        Assert.Contains(dashboard.Models, item => item.Label == "mock");

        var users = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/users", admin));
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        var userPage = await users.Content.ReadFromJsonAsync<UserPage>(JsonOptions);
        Assert.Contains(userPage!.Items, item => item.Email == adminEmail && item.Role == "Admin");

        var failures = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/investigations?processingStatus=Failed", admin));
        var failurePage = await failures.Content.ReadFromJsonAsync<InvestigationPage>(JsonOptions);
        var failure = Assert.Single(failurePage!.Items, item => item.Id == created.Id);
        Assert.Equal(ownerEmail, failure.OwnerEmail);
        Assert.Equal("CANCELLED", failure.FailureCode);
        Assert.Equal("mock", failure.EvidenceProvider);

        var runs = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/admin/ai-runs?status=Failed", admin));
        var runPage = await runs.Content.ReadFromJsonAsync<AiRunPage>(JsonOptions);
        Assert.Contains(runPage!.Items, item => item.InvestigationId == created.Id && item.ErrorCategory == "AI_RESPONSE_INVALID");
    }

    private async Task MarkFailedAsync(Guid investigationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimLensDbContext>();
        var investigation = await db.Investigations.SingleAsync(item => item.Id == investigationId);
        investigation.ProcessingStatus = ProcessingStatus.Failed;
        investigation.FailureCode = "CANCELLED";
        investigation.EvidenceProvider = "mock";
        db.AiRuns.Add(new AiRun
        {
            Id = Guid.CreateVersion7(),
            InvestigationId = investigationId,
            Purpose = AiRunPurpose.ClaimExtraction,
            PromptName = "claim-extraction",
            PromptVersion = "v1",
            ModelUsed = "mock",
            Status = AiRunStatus.Failed,
            ErrorCategory = "AI_RESPONSE_INVALID",
            LatencyMs = 12,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task SetRoleAsync(string email, UserRole role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimLensDbContext>();
        var user = await db.Users.SingleAsync(item => item.Email == email);
        user.Role = role;
        await db.SaveChangesAsync();
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

    private async Task<CreatedBody> CreateAsync(string token, string text)
    {
        var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/investigations", token, new
        {
            inputType = "Claim",
            text
        }));
        return (await response.Content.ReadFromJsonAsync<CreatedBody>(JsonOptions))!;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private sealed record AuthBody(string AccessToken);
    private sealed record CreatedBody(Guid Id);
    private sealed record DashboardBody(
        int Failed,
        int AiRunFailures,
        CountBody[] FailureCodes,
        CountBody[] Providers,
        CountBody[] Models);
    private sealed record CountBody(string Label, int Count);
    private sealed record UserPage(UserBody[] Items);
    private sealed record UserBody(string Email, string Role);
    private sealed record InvestigationPage(InvestigationBody[] Items);
    private sealed record InvestigationBody(Guid Id, string OwnerEmail, string? FailureCode, string? EvidenceProvider);
    private sealed record AiRunPage(AiRunBody[] Items);
    private sealed record AiRunBody(Guid InvestigationId, string? ErrorCategory);
}
