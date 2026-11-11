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

public class ReviewEndpointTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AuthApiFactory _factory;
    private readonly HttpClient _client;

    public ReviewEndpointTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reviewer_can_override_flag_and_approve_without_replacing_the_ai_stance()
    {
        var ownerToken = await RegisterAsync($"owner-{Guid.NewGuid():N}@example.com");
        var created = await CreateAsync(ownerToken, "A claim ready for review.");
        var evidenceId = Guid.CreateVersion7();
        var sourceId = Guid.CreateVersion7();
        await SeedCompletedEvidenceAsync(created.Id, evidenceId, sourceId);

        var stranger = await RegisterAsync($"stranger-{Guid.NewGuid():N}@example.com");
        var forbidden = await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/reviews/evidence/{evidenceId}", stranger, new
        {
            stance = "Neutral",
            note = "Not allowed."
        }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var hidden = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/investigations/{created.Id}", stranger));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var reviewerEmail = $"reviewer-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(reviewerEmail);
        await PromoteAsync(reviewerEmail);
        var reviewer = await LoginAsync(reviewerEmail);

        var queue = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/reviews/queue", reviewer));
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);

        var visible = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/investigations/{created.Id}", reviewer));
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);

        var overridden = await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/reviews/evidence/{evidenceId}", reviewer, new
        {
            stance = "Neutral",
            note = "Excerpt is background, not a result."
        }));
        Assert.Equal(HttpStatusCode.Created, overridden.StatusCode);
        var afterOverride = await overridden.Content.ReadFromJsonAsync<DetailBody>(JsonOptions);
        var evidence = Assert.Single(afterOverride!.Claims[0].Evidence);
        Assert.Equal("Supporting", evidence.AiStance);
        Assert.Equal("Neutral", evidence.Stance);
        Assert.Equal("Excerpt is background, not a result.", evidence.ReviewNote);
        Assert.Contains(afterOverride.Timeline, item => item.Type == "EvidenceOverridden");

        var flagged = await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/reviews/sources/{sourceId}/flag", reviewer, new
        {
            investigationId = created.Id,
            note = "Fixture publisher."
        }));
        Assert.Equal(HttpStatusCode.Created, flagged.StatusCode);
        var afterFlag = await flagged.Content.ReadFromJsonAsync<DetailBody>(JsonOptions);
        Assert.True(Assert.Single(afterFlag!.Claims[0].Evidence).SourceFlagged);

        var approved = await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/reviews/investigations/{created.Id}/approve", reviewer, new
        {
            note = "Reviewed."
        }));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        var afterApproval = await approved.Content.ReadFromJsonAsync<DetailBody>(JsonOptions);
        Assert.NotNull(afterApproval!.ApprovedAt);
        Assert.Equal("Mixed", afterApproval.Verdict);
        Assert.Contains(afterApproval.Timeline, item => item.Type == "InvestigationApproved");
    }

    private async Task SeedCompletedEvidenceAsync(Guid investigationId, Guid evidenceId, Guid sourceId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimLensDbContext>();
        var investigation = await db.Investigations.SingleAsync(item => item.Id == investigationId);
        var now = DateTimeOffset.UtcNow;
        investigation.ProcessingStatus = ProcessingStatus.Completed;
        investigation.Verdict = Verdict.Mixed;
        investigation.ConfidenceScore = 0.35m;
        investigation.CompletedAt = now;
        var claimId = Guid.CreateVersion7();
        db.Claims.Add(new Claim
        {
            Id = claimId,
            InvestigationId = investigationId,
            Position = 1,
            ClaimText = "A claim ready for review.",
            NormalizedClaim = "a claim ready for review",
            ClaimType = ClaimType.Factual,
            ImportanceScore = 0.8m,
            CreatedAt = now
        });
        db.Sources.Add(new Source
        {
            Id = sourceId,
            Url = $"https://public-health.example/{sourceId:N}",
            Domain = "public-health.example",
            Title = "Public note",
            RetrievedAt = now
        });
        db.Evidence.Add(new Evidence
        {
            Id = evidenceId,
            ClaimId = claimId,
            SourceId = sourceId,
            EvidenceText = "A claim ready for review.",
            AiStance = Stance.Supporting,
            Stance = Stance.Supporting,
            RelevanceScore = 0.9m,
            ConfidenceScore = 0.8m
        });
        await db.SaveChangesAsync();
    }

    private async Task PromoteAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ClaimLensDbContext>();
        var user = await db.Users.SingleAsync(item => item.Email == email);
        user.Role = UserRole.Reviewer;
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
    private sealed record DetailBody(string? Verdict, DateTimeOffset? ApprovedAt, ClaimBody[] Claims, TimelineBody[] Timeline);
    private sealed record ClaimBody(EvidenceBody[] Evidence);
    private sealed record EvidenceBody(string? AiStance, string? Stance, string? ReviewNote, bool SourceFlagged);
    private sealed record TimelineBody(string Type);
}
