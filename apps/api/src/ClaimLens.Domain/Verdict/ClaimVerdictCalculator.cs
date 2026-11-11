using ClaimLens.Domain.Scoring;

namespace ClaimLens.Domain.Investigations;

public sealed record EvidenceAssessment(
    string Domain,
    Stance? Stance,
    decimal? Relevance,
    decimal? ClassificationConfidence,
    SourceQualityBand? Quality);

public sealed record ClaimAssessment(Guid ClaimId, int Position, decimal Importance, IReadOnlyList<EvidenceAssessment> Evidence);

public sealed record ClaimVerdictResult(Guid ClaimId, Verdict Verdict, decimal Confidence, string Explanation);

public sealed record InvestigationVerdictResult(Verdict Verdict, decimal Confidence, IReadOnlyList<ClaimVerdictResult> Claims);

public static class ClaimVerdictCalculator
{
    public const string AlgorithmVersion = "verdict-v1";

    public static InvestigationVerdictResult Calculate(IReadOnlyList<ClaimAssessment> claims)
    {
        var claimResults = claims
            .OrderBy(claim => claim.Position)
            .Select(CalculateClaim)
            .ToArray();

        if (claimResults.Length == 0)
        {
            return new InvestigationVerdictResult(Verdict.InsufficientEvidence, VerdictThresholds.MinimumReportedConfidence, claimResults);
        }

        var byImportance = claims
            .OrderByDescending(claim => claim.Importance)
            .ThenBy(claim => claim.Position)
            .Take(2)
            .Select(claim => claimResults.Single(result => result.ClaimId == claim.ClaimId))
            .ToArray();

        var verdict = byImportance.Length == 2 && Opposite(byImportance[0].Verdict, byImportance[1].Verdict)
            ? Verdict.Mixed
            : byImportance[0].Verdict;

        var weight = claims.Sum(claim => claim.Importance);
        var confidence = weight == 0
            ? claimResults.Average(result => result.Confidence)
            : claims.Sum(claim => claim.Importance * claimResults.Single(result => result.ClaimId == claim.ClaimId).Confidence) / weight;

        return new InvestigationVerdictResult(verdict, Round(confidence), claimResults);
    }

    public static ClaimVerdictResult CalculateClaim(ClaimAssessment claim)
    {
        var usable = claim.Evidence.Where(IsUsable).ToArray();
        var domains = usable.Select(item => RegistrableDomain(item.Domain)).Distinct(StringComparer.Ordinal).ToArray();
        var support = usable.Where(item => item.Stance == Stance.Supporting).Sum(Weight);
        var contradict = usable.Where(item => item.Stance == Stance.Contradicting).Sum(Weight);
        var total = support + contradict;
        var insufficient = domains.Length < VerdictThresholds.MinimumIndependentSources || total < VerdictThresholds.MinimumTotalWeight;
        var balance = total == 0 ? 0 : (support - contradict) / total;
        var verdict = insufficient ? Verdict.InsufficientEvidence : Map(balance, contradict, total);
        var confidence = Confidence(balance, domains.Length, insufficient);
        var explanation = $"{verdict} from {domains.Length} independent usable domain(s). Support weight {Round(support)}, contradict weight {Round(contradict)}. System confidence is not the probability the claim is true.";
        return new ClaimVerdictResult(claim.ClaimId, verdict, confidence, explanation);
    }

    public static string RegistrableDomain(string domain)
    {
        var host = domain.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return labels.Length <= 2 ? string.Join('.', labels) : string.Join('.', labels[^2], labels[^1]);
    }

    private static bool IsUsable(EvidenceAssessment item) =>
        item.Stance is not null
        && item.Relevance >= VerdictThresholds.MinimumRelevance
        && item.ClassificationConfidence >= VerdictThresholds.MinimumClassificationConfidence
        && item.Quality is not null;

    private static decimal Weight(EvidenceAssessment item)
    {
        var multiplier = item.Quality switch
        {
            SourceQualityBand.Limited => VerdictThresholds.QualityMultiplierLimited,
            SourceQualityBand.Moderate => VerdictThresholds.QualityMultiplierModerate,
            SourceQualityBand.StrongIndicators => VerdictThresholds.QualityMultiplierStrongIndicators,
            _ => VerdictThresholds.QualityMultiplierLimited
        };
        var weight = item.Relevance!.Value * item.ClassificationConfidence!.Value * multiplier;
        return weight > VerdictThresholds.MaximumEvidenceWeight ? VerdictThresholds.MaximumEvidenceWeight : weight;
    }

    private static Verdict Map(decimal balance, decimal contradict, decimal total)
    {
        var share = contradict / total;
        if (balance >= VerdictThresholds.SupportedBalance && share <= VerdictThresholds.MaxContradictionShareForSupported)
        {
            return Verdict.Supported;
        }

        if (balance >= VerdictThresholds.MostlySupportedBalance)
        {
            return Verdict.MostlySupported;
        }

        if (balance <= VerdictThresholds.ContradictedBalance)
        {
            return Verdict.Contradicted;
        }

        if (balance <= VerdictThresholds.MostlyContradictedBalance)
        {
            return Verdict.MostlyContradicted;
        }

        return Verdict.Mixed;
    }

    private static decimal Confidence(decimal balance, int independentDomains, bool insufficient)
    {
        var extra = independentDomains - VerdictThresholds.MinimumIndependentSources;
        if (extra < 0)
        {
            extra = 0;
        }

        var raw = Math.Abs(balance) + (extra * VerdictThresholds.ExtraDomainConfidenceBonus);
        if (insufficient)
        {
            return Round(raw > VerdictThresholds.InsufficientEvidenceConfidenceCap ? VerdictThresholds.InsufficientEvidenceConfidenceCap : raw);
        }

        if (raw < VerdictThresholds.MinimumReportedConfidence)
        {
            raw = VerdictThresholds.MinimumReportedConfidence;
        }

        if (raw > VerdictThresholds.MaximumReportedConfidence)
        {
            raw = VerdictThresholds.MaximumReportedConfidence;
        }

        return Round(raw);
    }

    private static bool Opposite(Verdict left, Verdict right) =>
        (IsSupportive(left) && IsContradictory(right)) || (IsContradictory(left) && IsSupportive(right));

    private static bool IsSupportive(Verdict verdict) => verdict is Verdict.Supported or Verdict.MostlySupported;

    private static bool IsContradictory(Verdict verdict) => verdict is Verdict.Contradicted or Verdict.MostlyContradicted;

    private static decimal Round(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
