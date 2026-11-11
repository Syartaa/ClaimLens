using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClaimLens.Application.Investigations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimLens.Api.IntegrationTests;

public sealed class ExtractionApiFactory : AuthApiFactory
{
    public ScriptedAiClient Ai { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            foreach (var existing in services.Where(service => service.ServiceType == typeof(IAiAnalysisClient)).ToArray())
            {
                services.Remove(existing);
            }

            services.AddSingleton<IAiAnalysisClient>(Ai);
        });
    }
}

public class ClaimExtractionTests : IClassFixture<ExtractionApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ExtractionApiFactory _factory;

    public ClaimExtractionTests(ExtractionApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_response_persists_claims()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Result = new ClaimExtractionResult(
            [new ExtractedClaim("Coffee does not dehydrate regular drinkers.", "Coffee does not dehydrate regular drinkers.", "scientific", 0.82m)],
            "claim-extraction",
            "v1",
            "test");
        _factory.Ai.Error = null;

        await ProcessAsync();

        var stored = await ReadAsync(client, id);
        Assert.Equal("GatheringEvidence", stored.ProcessingStatus);
        Assert.Single(stored.Claims);
        Assert.Equal("Scientific", stored.Claims[0].ClaimType);
    }

    [Fact]
    public async Task Invalid_response_fails_without_claims()
    {
        var client = await SignedInClientAsync();
        var id = await CreateAsync(client);
        _factory.Ai.Error = new AiResponseException("AI_RESPONSE_INVALID");

        await ProcessAsync();

        var stored = await ReadAsync(client, id);
        Assert.Equal("Failed", stored.ProcessingStatus);
        Assert.Equal("AI_RESPONSE_INVALID", stored.FailureCode);
        Assert.Empty(stored.Claims);
    }

    private async Task ProcessAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ClaimExtractionProcessor>();
        await processor.ProcessNextAsync(CancellationToken.None);
    }

    private async Task<HttpClient> SignedInClientAsync()
    {
        var client = _factory.CreateClient();
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"extract-{Guid.NewGuid():N}@example.com",
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
    private sealed record StoredBody(string ProcessingStatus, string? FailureCode, ClaimBody[] Claims);
    private sealed record ClaimBody(string ClaimType);
}
