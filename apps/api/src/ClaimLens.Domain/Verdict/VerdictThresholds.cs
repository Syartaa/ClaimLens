namespace ClaimLens.Domain.Scoring;

/// <summary>
/// Engineering heuristics for the verdict calculator.
/// These cutoffs are not scientifically validated, and they are not the
/// probability that a claim is true. System confidence only describes how
/// strongly the stored evidence leans under these rules.
/// The calculator must read these constants and must not introduce new literals.
/// Behavior tests belong with the calculator in the verdict-engine phase.
/// </summary>
public static class VerdictThresholds
{
    public const decimal MinimumRelevance = 0.40m;

    public const decimal MinimumClassificationConfidence = 0.50m;

    public const decimal QualityMultiplierLimited = 0.70m;

    public const decimal QualityMultiplierModerate = 1.00m;

    public const decimal QualityMultiplierStrongIndicators = 1.15m;

    public const decimal MaximumEvidenceWeight = 1.15m;

    public const int MinimumIndependentSources = 2;

    public const decimal MinimumTotalWeight = 1.20m;

    public const decimal SupportedBalance = 0.60m;

    public const decimal MostlySupportedBalance = 0.25m;

    public const decimal MostlyContradictedBalance = -0.25m;

    public const decimal ContradictedBalance = -0.60m;

    public const decimal MaxContradictionShareForSupported = 0.15m;

    public const decimal MinimumReportedConfidence = 0.35m;

    public const decimal MaximumReportedConfidence = 0.95m;

    public const decimal InsufficientEvidenceConfidenceCap = 0.40m;

    public const decimal ExtraDomainConfidenceBonus = 0.05m;
}
