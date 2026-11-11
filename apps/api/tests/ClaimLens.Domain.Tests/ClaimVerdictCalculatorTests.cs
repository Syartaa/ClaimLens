using ClaimLens.Domain.Investigations;
using ClaimLens.Domain.Scoring;

namespace ClaimLens.Domain.Tests;

public class ClaimVerdictCalculatorTests
{
    [Fact]
    public void Several_strong_supporting_sources_are_supported()
    {
        var result = Claim(Strong("a.example", Stance.Supporting), Strong("b.example", Stance.Supporting), Strong("c.example", Stance.Supporting));

        Assert.Equal(Verdict.Supported, result.Verdict);
    }

    [Fact]
    public void Several_strong_contradicting_sources_are_contradicted()
    {
        var result = Claim(Strong("a.example", Stance.Contradicting), Strong("b.example", Stance.Contradicting), Strong("c.example", Stance.Contradicting));

        Assert.Equal(Verdict.Contradicted, result.Verdict);
    }

    [Fact]
    public void Similar_support_and_contradiction_are_mixed()
    {
        var result = Claim(
            Strong("a.example", Stance.Supporting),
            Strong("b.example", Stance.Supporting),
            Strong("c.example", Stance.Contradicting),
            Strong("d.example", Stance.Contradicting));

        Assert.Equal(Verdict.Mixed, result.Verdict);
    }

    [Fact]
    public void No_usable_evidence_is_insufficient()
    {
        var result = Claim();

        Assert.Equal(Verdict.InsufficientEvidence, result.Verdict);
        Assert.True(result.Confidence <= VerdictThresholds.InsufficientEvidenceConfidenceCap);
    }

    [Fact]
    public void One_weak_source_is_insufficient()
    {
        var result = Claim(new EvidenceAssessment("a.example", Stance.Supporting, 0.20m, 0.20m, SourceQualityBand.Limited));

        Assert.Equal(Verdict.InsufficientEvidence, result.Verdict);
    }

    [Fact]
    public void One_strong_source_cannot_carry_a_verdict()
    {
        var result = Claim(Strong("a.example", Stance.Supporting));

        Assert.Equal(Verdict.InsufficientEvidence, result.Verdict);
    }

    [Fact]
    public void More_independent_strong_sources_raise_system_confidence()
    {
        var single = Claim(Strong("a.example", Stance.Supporting));
        var several = Claim(Strong("a.example", Stance.Supporting), Strong("b.example", Stance.Supporting), Strong("c.example", Stance.Supporting));

        Assert.True(several.Confidence > single.Confidence);
    }

    [Fact]
    public void Same_registrable_domain_counts_once()
    {
        var result = Claim(Strong("news.example.com", Stance.Supporting), Strong("www.example.com", Stance.Supporting));

        Assert.Equal(Verdict.InsufficientEvidence, result.Verdict);
    }

    [Fact]
    public void Disagreement_between_important_claims_is_mixed()
    {
        var supported = new ClaimAssessment(Guid.CreateVersion7(), 1, 0.9m, [Strong("a.example", Stance.Supporting), Strong("b.example", Stance.Supporting)]);
        var contradicted = new ClaimAssessment(Guid.CreateVersion7(), 2, 0.8m, [Strong("c.example", Stance.Contradicting), Strong("d.example", Stance.Contradicting)]);

        var result = ClaimVerdictCalculator.Calculate([supported, contradicted]);

        Assert.Equal(Verdict.Mixed, result.Verdict);
    }

    private static ClaimVerdictResult Claim(params EvidenceAssessment[] evidence) =>
        ClaimVerdictCalculator.CalculateClaim(new ClaimAssessment(Guid.CreateVersion7(), 1, 0.9m, evidence));

    private static EvidenceAssessment Strong(string domain, Stance stance) =>
        new(domain, stance, 1m, 1m, SourceQualityBand.StrongIndicators);
}
