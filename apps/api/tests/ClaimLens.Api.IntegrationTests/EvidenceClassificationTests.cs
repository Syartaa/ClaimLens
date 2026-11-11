using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Api.IntegrationTests;

public class EvidenceClassificationTests : IClassFixture<EvidenceApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly EvidenceApiFactory _factory;

    public EvidenceClassificationTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_classification_persists_matching_stances()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Error = null;
        _factory.Evidence.Error = null;
        _factory.Evidence.Items =
        [
            new RetrievedEvidence(
                "https://fixtures.claimlens.test/classify-note",
                "Fixture note",
                "A. Fixture",
                "ClaimLens fixtures",
                null,
                "Vitamin C prevents the common cold.",
                new SourceQualityFactors(true, true, true, false, true, false))
        ];
        _factory.Ai.Classification = new EvidenceClassification(
            "supporting",
            0.75m,
            0.7m,
            "Mock classification from the excerpt text.",
            "evidence-classification",
            "v1",
            "test");

        await ProcessAsync<ClaimExtractionProcessor>();
        await ProcessAsync<EvidenceCollectionProcessor>();
        await ProcessAsync<EvidenceClassificationProcessor>();

        var stored = await ReadAsync(client, id);
        Assert.Equal("GeneratingConclusion", stored.ProcessingStatus);
        var item = Assert.Single(stored.Claims[0].Evidence);
        Assert.Equal("Supporting", item.AiStance);
        Assert.Equal("Supporting", item.Stance);
        Assert.Equal(0.75m, item.RelevanceScore);
        Assert.Equal(0.7m, item.ConfidenceScore);
    }

    [Fact]
    public async Task Invalid_stance_fails_without_saving_classification()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Error = null;
        _factory.Evidence.Error = null;
        _factory.Evidence.Items =
        [
            new RetrievedEvidence(
                "https://fixtures.claimlens.test/invalid-stance",
                "Fixture note",
                null,
                null,
                null,
                "An excerpt.",
                new SourceQualityFactors(true, false, false, false, false, false))
        ];
        _factory.Ai.Classification = new EvidenceClassification("maybe", 0.4m, 0.4m, "Bad.", "evidence-classification", "v1", "test");

        await ProcessAsync<ClaimExtractionProcessor>();
        await ProcessAsync<EvidenceCollectionProcessor>();
        await ProcessAsync<EvidenceClassificationProcessor>();

        var stored = await ReadAsync(client, id);
        Assert.Equal("Failed", stored.ProcessingStatus);
        Assert.Equal("AI_RESPONSE_INVALID", stored.FailureCode);
        Assert.Null(Assert.Single(stored.Claims[0].Evidence).Stance);
    }

    private async Task ProcessAsync<T>() where T : class
    {
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<T>();
        var method = typeof(T).GetMethod("ProcessNextAsync")!;
        await (Task<bool>)method.Invoke(processor, [CancellationToken.None])!;
    }

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = _factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"classify-{Guid.NewGuid():N}@example.com",
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
            text = "Vitamin C prevents the common cold."
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
    private sealed record StoredBody(string ProcessingStatus, string? FailureCode, ClaimBody[] Claims);
    private sealed record ClaimBody(EvidenceBody[] Evidence);
    private sealed record EvidenceBody(string? AiStance, string? Stance, decimal? RelevanceScore, decimal? ConfidenceScore);
}
