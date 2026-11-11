using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Api.IntegrationTests;

public sealed class EvidenceApiFactory : AuthApiFactory
{
    public ScriptedAiClient Ai { get; } = new();

    public ScriptedEvidenceProvider Evidence { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            foreach (var existing in services.Where(service => service.ServiceType == typeof(IAiAnalysisClient) || service.ServiceType == typeof(IEvidenceProvider)).ToArray())
            {
                services.Remove(existing);
            }

            services.AddSingleton<IAiAnalysisClient>(Ai);
            services.AddSingleton<IEvidenceProvider>(Evidence);
        });
    }
}

public sealed class ScriptedEvidenceProvider : IEvidenceProvider
{
    public string Name => "mock";

    public IReadOnlyList<RetrievedEvidence>? Items { get; set; }

    public Exception? Error { get; set; }

    public Task<IReadOnlyList<RetrievedEvidence>> RetrieveAsync(string claimText, CancellationToken cancellationToken)
    {
        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(Items ?? []);
    }
}

public class EvidenceCollectionTests : IClassFixture<EvidenceApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly EvidenceApiFactory _factory;

    public EvidenceCollectionTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_sources_are_saved_and_reused()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Error = null;
        _factory.Ai.Result = new ClaimExtractionResult(
            [new ExtractedClaim("Coffee does not dehydrate regular drinkers.", "Coffee does not dehydrate regular drinkers.", "scientific", 0.8m)],
            "claim-extraction",
            "v1",
            "test");
        _factory.Evidence.Error = null;
        _factory.Evidence.Items =
        [
            new RetrievedEvidence(
                "https://fixtures.claimlens.test/public-health-note",
                "Fixture public-health note",
                "A. Fixture",
                "ClaimLens fixtures",
                new DateTimeOffset(2020, 3, 1, 0, 0, 0, TimeSpan.Zero),
                "Coffee does not dehydrate regular drinkers.",
                new SourceQualityFactors(true, true, true, false, true, false)),
            new RetrievedEvidence(
                "https://fixtures.claimlens.test/short-note",
                "Fixture short note",
                null,
                null,
                null,
                "A short fixture excerpt.",
                new SourceQualityFactors(true, false, false, false, false, false))
        ];

        await ProcessExtractionAsync();
        await ProcessEvidenceAsync();

        var stored = await ReadAsync(client, id);
        Assert.Equal("AnalyzingEvidence", stored.ProcessingStatus);
        Assert.Equal("mock", stored.EvidenceProvider);
        Assert.Equal(2, stored.Claims[0].Evidence.Length);
        Assert.Contains(stored.Claims[0].Evidence, item => item.QualityBand == "StrongIndicators");
        Assert.Contains(stored.Claims[0].Evidence, item => item.QualityBand == "Limited");
    }

    [Fact]
    public async Task Invalid_source_fails_without_evidence()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Error = null;
        _factory.Evidence.Error = null;
        _factory.Evidence.Items =
        [
            new RetrievedEvidence("not-a-url", "Bad", null, null, null, "Excerpt", new SourceQualityFactors(false, false, false, false, false, false))
        ];

        await ProcessExtractionAsync();
        await ProcessEvidenceAsync();

        var stored = await ReadAsync(client, id);
        Assert.Equal("Failed", stored.ProcessingStatus);
        Assert.Equal("EVIDENCE_RESPONSE_INVALID", stored.FailureCode);
        Assert.Empty(stored.Claims[0].Evidence);
    }

    private async Task ProcessExtractionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ClaimExtractionProcessor>();
        await processor.ProcessNextAsync(CancellationToken.None);
    }

    private async Task ProcessEvidenceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<EvidenceCollectionProcessor>();
        await processor.ProcessNextAsync(CancellationToken.None);
    }

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = _factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"evidence-{Guid.NewGuid():N}@example.com",
            password = "CorrectHorse1",
            firstName = "Ada",
            lastName = "Lovelace"
        });
        var auth = await registered.Content.ReadFromJsonAsync<AuthBody>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/api/investigations", new
        {
            inputType = "Claim",
            text = "Drinking coffee causes dehydration."
        });
        var body = await created.Content.ReadFromJsonAsync<CreatedBody>(JsonOptions);
        return body!.Id;
    }

    private static async Task<StoredBody> ReadAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"/api/investigations/{id}");
        return (await response.Content.ReadFromJsonAsync<StoredBody>(JsonOptions))!;
    }

    private sealed record AuthBody(string AccessToken);
    private sealed record CreatedBody(Guid Id);
    private sealed record StoredBody(string ProcessingStatus, string? FailureCode, string? EvidenceProvider, ClaimBody[] Claims);
    private sealed record ClaimBody(EvidenceBody[] Evidence);
    private sealed record EvidenceBody(string QualityBand);
}
