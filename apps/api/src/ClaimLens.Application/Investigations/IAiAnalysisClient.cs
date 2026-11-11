namespace ClaimLens.Application.Investigations;

public sealed record ExtractedClaim(string Text, string NormalizedText, string Type, decimal Importance);

public sealed record EvidenceClassification(
    string Stance,
    decimal Relevance,
    decimal Confidence,
    string Explanation,
    string PromptName,
    string PromptVersion,
    string ModelUsed);

public interface IAiAnalysisClient
{
    Task<ClaimExtractionResult> ExtractClaimsAsync(string text, string inputType, CancellationToken cancellationToken);

    Task<EvidenceClassification> ClassifyEvidenceAsync(string claimText, string excerpt, CancellationToken cancellationToken);

    Task<string> SummarizeAsync(string verdict, decimal confidence, string explanation, CancellationToken cancellationToken);
}

public sealed record ClaimExtractionResult(
    IReadOnlyList<ExtractedClaim> Claims,
    string PromptName,
    string PromptVersion,
    string ModelUsed);
