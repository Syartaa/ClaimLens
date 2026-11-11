namespace ClaimLens.Domain.Investigations;

public enum SourceQualityBand
{
    Limited,
    Moderate,
    StrongIndicators
}

public enum Stance
{
    Supporting,
    Contradicting,
    Neutral
}

public readonly record struct SourceQualityFactors(
    bool Https,
    bool HasAuthor,
    bool HasPublishedAt,
    bool AcademicDomain,
    bool PublicInstitution,
    bool PrimaryReport);

public static class SourceQualityBands
{
    public static SourceQualityBand From(SourceQualityFactors factors)
    {
        var institutional = factors.AcademicDomain || factors.PublicInstitution || factors.PrimaryReport;
        if (factors.Https && institutional && (factors.HasAuthor || factors.HasPublishedAt))
        {
            return SourceQualityBand.StrongIndicators;
        }

        var indicators = Count(
            factors.HasAuthor,
            factors.HasPublishedAt,
            factors.AcademicDomain,
            factors.PublicInstitution,
            factors.PrimaryReport);
        if (factors.Https && indicators >= 2)
        {
            return SourceQualityBand.Moderate;
        }

        return SourceQualityBand.Limited;
    }

    private static int Count(params bool[] flags) => flags.Count(flag => flag);
}
