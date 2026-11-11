using System.Net.Http.Json;
using ClaimLens.Application.Investigations;

namespace ClaimLens.Infrastructure.Ai;

public sealed class HttpAiAnalysisClient : IAiAnalysisClient
{
    private static readonly HashSet<string> AllowedStances = new(StringComparer.OrdinalIgnoreCase)
    {
        "supporting", "contradicting", "neutral"
    };

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "factual", "statistical", "scientific", "historical", "medical", "political", "other"
    };

    private readonly HttpClient _http;

    public HttpAiAnalysisClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ClaimExtractionResult> ExtractClaimsAsync(string text, string inputType, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync("/v1/claims/extract", new { text, inputType }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI service returned {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadFromJsonAsync<ExtractionBody>(cancellationToken)
            ?? throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode);

        if (body.Claims is null || body.Claims.Count == 0 || body.Claims.Count > 12)
        {
            throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode);
        }

        var claims = new List<ExtractedClaim>();
        foreach (var claim in body.Claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Text)
                || string.IsNullOrWhiteSpace(claim.Type)
                || !AllowedTypes.Contains(claim.Type)
                || claim.Importance is < 0 or > 1)
            {
                throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode);
            }

            claims.Add(new ExtractedClaim(claim.Text.Trim(), claim.Text.Trim(), claim.Type, claim.Importance));
        }

        return new ClaimExtractionResult(
            claims,
            body.PromptName ?? "claim-extraction",
            body.PromptVersion ?? "v1",
            body.Model ?? "unknown");
    }

    public async Task<EvidenceClassification> ClassifyEvidenceAsync(string claimText, string excerpt, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync("/v1/evidence/classify", new { claimText, excerpt }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI service returned {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadFromJsonAsync<ClassificationBody>(cancellationToken)
            ?? throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode);

        if (string.IsNullOrWhiteSpace(body.Stance)
            || !AllowedStances.Contains(body.Stance)
            || string.IsNullOrWhiteSpace(body.Explanation)
            || body.Relevance is < 0 or > 1
            || body.Confidence is < 0 or > 1)
        {
            throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode);
        }

        return new EvidenceClassification(
            body.Stance,
            body.Relevance,
            body.Confidence,
            body.Explanation.Trim(),
            body.PromptName ?? "evidence-classification",
            body.PromptVersion ?? "v1",
            body.Model ?? "unknown");
    }

    public async Task<string> SummarizeAsync(string verdict, decimal confidence, string explanation, CancellationToken cancellationToken)
    {
        using var response = await _http.PostAsJsonAsync("/v1/summary", new { verdict, confidence, explanation }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"AI service returned {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadFromJsonAsync<SummaryBody>(cancellationToken)
            ?? throw new AiResponseException(ConclusionProcessor.SummaryInvalidCode);
        if (string.IsNullOrWhiteSpace(body.Summary))
        {
            throw new AiResponseException(ConclusionProcessor.SummaryInvalidCode);
        }

        return body.Summary.Trim();
    }

    private sealed record ExtractionBody(List<ClaimBody>? Claims, string? PromptName, string? PromptVersion, string? Model);

    private sealed record ClaimBody(string? Text, string? Type, decimal Importance);

    private sealed record ClassificationBody(
        string? Stance,
        decimal Relevance,
        decimal Confidence,
        string? Explanation,
        string? PromptName,
        string? PromptVersion,
        string? Model);

    private sealed record SummaryBody(string? Summary);
}
