using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClaimLens.Api.IntegrationTests;

public class InvestigationEndpointTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _client;

    public InvestigationEndpointTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Owner_can_create_list_and_delete_an_investigation()
    {
        var token = await RegisterAsync($"owner-{Guid.NewGuid():N}@example.com");
        var created = await CreateAsync(token, "Coffee dehydrates you.");

        Assert.Equal("Pending", created.ProcessingStatus);
        Assert.Equal("Claim", created.InputType);

        var list = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/investigations?q=Coffee", token));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var page = await list.Content.ReadFromJsonAsync<ListBody>(JsonOptions);
        Assert.Contains(page!.Items, item => item.Id == created.Id);

        var other = await RegisterAsync($"other-{Guid.NewGuid():N}@example.com");
        var hidden = await _client.SendAsync(Authorized(HttpMethod.Get, $"/api/investigations/{created.Id}", other));
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var deleted = await _client.SendAsync(Authorized(HttpMethod.Delete, $"/api/investigations/{created.Id}", token));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Summary_counts_the_owner_and_reanalyze_returns_pending()
    {
        var token = await RegisterAsync($"summary-{Guid.NewGuid():N}@example.com");
        var created = await CreateAsync(token, "A claim to rerun.");

        var summary = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/investigations/summary", token));
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        var counts = await summary.Content.ReadFromJsonAsync<SummaryBody>(JsonOptions);
        Assert.True(counts!.Total >= 1);

        var restarted = await _client.SendAsync(Authorized(HttpMethod.Post, $"/api/investigations/{created.Id}/reanalyze", token));
        Assert.Equal(HttpStatusCode.Accepted, restarted.StatusCode);
        var body = await restarted.Content.ReadFromJsonAsync<InvestigationBody>(JsonOptions);
        Assert.Equal("Pending", body!.ProcessingStatus);
    }

    [Fact]
    public async Task Url_input_is_rejected()
    {
        var token = await RegisterAsync($"url-{Guid.NewGuid():N}@example.com");
        var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/investigations", token, new
        {
            inputType = "Url",
            title = "A link",
            text = "https://example.com"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>(JsonOptions);
        Assert.Equal("INPUT_TYPE_NOT_SUPPORTED", error!.Code);
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

    private async Task<InvestigationBody> CreateAsync(string token, string text)
    {
        var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/api/investigations", token, new
        {
            inputType = "Claim",
            text
        }));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<InvestigationBody>(JsonOptions))!;
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
    private sealed record InvestigationBody(Guid Id, string ProcessingStatus, string InputType);
    private sealed record ListBody(InvestigationBody[] Items);
    private sealed record ErrorBody(string Code);
    private sealed record SummaryBody(int Total);
}
