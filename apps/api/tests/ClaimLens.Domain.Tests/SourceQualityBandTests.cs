using ClaimLens.Domain.Investigations;

namespace ClaimLens.Domain.Tests;

public class SourceQualityBandTests
{
    [Fact]
    public void Institutional_source_with_https_and_a_date_or_author_is_strong_indicators()
    {
        var band = SourceQualityBands.From(new SourceQualityFactors(true, true, true, false, true, false));

        Assert.Equal(SourceQualityBand.StrongIndicators, band);
    }

    [Fact]
    public void Https_with_two_other_indicators_is_moderate()
    {
        var band = SourceQualityBands.From(new SourceQualityFactors(true, true, true, false, false, false));

        Assert.Equal(SourceQualityBand.Moderate, band);
    }

    [Fact]
    public void Sparse_source_is_limited()
    {
        var band = SourceQualityBands.From(new SourceQualityFactors(true, false, false, false, false, false));

        Assert.Equal(SourceQualityBand.Limited, band);
    }
}
