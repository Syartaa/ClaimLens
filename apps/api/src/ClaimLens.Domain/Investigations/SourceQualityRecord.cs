namespace ClaimLens.Domain.Investigations;

public sealed class SourceQuality
{
    public Guid Id { get; set; }

    public Guid SourceId { get; set; }

    public Source Source { get; set; } = null!;

    public SourceQualityBand Band { get; set; }

    public string FactorsJson { get; set; } = "{}";

    public string Explanation { get; set; } = string.Empty;
}
