using ClaimLens.Application.Investigations;

namespace ClaimLens.Api.IntegrationTests;

public sealed class ScriptedAiClient : IAiAnalysisClient
{
    public ClaimExtractionResult? Result { get; set; }

    public Exception? Error { get; set; }

    public Task<ClaimExtractionResult> ExtractClaimsAsync(string text, string inputType, CancellationToken cancellationToken)
    {
        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(Result ?? new ClaimExtractionResult(
            [new ExtractedClaim(text, text, "factual", 0.8m)],
            "claim-extraction",
            "v1",
            "test"));
    }

    public EvidenceClassification? Classification { get; set; }

    public Task<EvidenceClassification> ClassifyEvidenceAsync(string claimText, string excerpt, CancellationToken cancellationToken)
    {
        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(Classification ?? new EvidenceClassification(
            "neutral",
            0.5m,
            0.5m,
            "Scripted classification.",
            "evidence-classification",
            "v1",
            "test"));
    }

    public string? Summary { get; set; }

    public Task<string> SummarizeAsync(string verdict, decimal confidence, string explanation, CancellationToken cancellationToken)
    {
        if (Error is not null)
        {
            throw Error;
        }

        return Task.FromResult(Summary ?? $"Calculator verdict: {verdict}. System confidence {confidence} is not the probability the claim is true.");
    }
}
