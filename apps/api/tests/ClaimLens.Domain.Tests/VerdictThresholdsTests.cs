using ClaimLens.Domain.Scoring;

namespace ClaimLens.Domain.Tests;

public class VerdictThresholdsTests
{
    [Fact]
    public void Cutoffs_stay_inside_the_ranges_the_calculator_will_assume()
    {
        Assert.InRange(VerdictThresholds.MinimumRelevance, 0m, 1m);
        Assert.InRange(VerdictThresholds.MinimumClassificationConfidence, 0m, 1m);
        Assert.InRange(VerdictThresholds.MaxContradictionShareForSupported, 0m, 1m);
        Assert.InRange(VerdictThresholds.MinimumReportedConfidence, 0m, 1m);
        Assert.InRange(VerdictThresholds.MaximumReportedConfidence, 0m, 1m);
        Assert.InRange(VerdictThresholds.InsufficientEvidenceConfidenceCap, 0m, 1m);

        Assert.True(VerdictThresholds.SupportedBalance > VerdictThresholds.MostlySupportedBalance);
        Assert.True(VerdictThresholds.MostlySupportedBalance > 0m);
        Assert.True(VerdictThresholds.ContradictedBalance < VerdictThresholds.MostlyContradictedBalance);
        Assert.True(VerdictThresholds.MostlyContradictedBalance < 0m);
        Assert.True(VerdictThresholds.MinimumReportedConfidence < VerdictThresholds.InsufficientEvidenceConfidenceCap);
        Assert.True(VerdictThresholds.InsufficientEvidenceConfidenceCap < VerdictThresholds.MaximumReportedConfidence);
        Assert.True(VerdictThresholds.MaximumEvidenceWeight >= VerdictThresholds.QualityMultiplierStrongIndicators);
        Assert.True(VerdictThresholds.MinimumIndependentSources >= 2);
        Assert.True(VerdictThresholds.MinimumTotalWeight > 0m);
    }
}
