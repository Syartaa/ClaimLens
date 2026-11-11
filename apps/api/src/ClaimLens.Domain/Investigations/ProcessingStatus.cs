namespace ClaimLens.Domain.Investigations;

public enum ProcessingStatus
{
    Pending,
    ExtractingClaims,
    GatheringEvidence,
    AnalyzingEvidence,
    GeneratingConclusion,
    Completed,
    Failed
}
