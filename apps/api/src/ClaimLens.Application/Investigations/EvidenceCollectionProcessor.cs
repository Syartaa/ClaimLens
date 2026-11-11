using System.Text.Json;
using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public sealed class EvidenceCollectionProcessor
{
    public const string InvalidResponseCode = "EVIDENCE_RESPONSE_INVALID";
    public const string RequestFailedCode = "EVIDENCE_REQUEST_FAILED";

    private readonly IInvestigationStore _store;
    private readonly IEvidenceProvider _provider;
    private readonly TimeProvider _clock;

    public EvidenceCollectionProcessor(IInvestigationStore store, IEvidenceProvider provider, TimeProvider clock)
    {
        _store = store;
        _provider = provider;
        _clock = clock;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var investigation = await _store.ClaimNextGatheringAsync(cancellationToken);
        if (investigation is null)
        {
            return false;
        }

        try
        {
            var claims = await _store.ListClaimsAsync(investigation.Id, cancellationToken);
            var evidence = new List<Evidence>();
            var sourcesByUrl = new Dictionary<string, Source>(StringComparer.Ordinal);
            foreach (var claim in claims)
            {
                var retrieved = await _provider.RetrieveAsync(claim.ClaimText, cancellationToken);
                if (retrieved.Count > 8)
                {
                    throw new EvidenceResponseException(InvalidResponseCode);
                }

                foreach (var item in retrieved)
                {
                    evidence.Add(await MapAsync(claim.Id, item, sourcesByUrl, cancellationToken));
                }
            }

            if (evidence.Count > 0)
            {
                await _store.AddEvidenceAsync(evidence, cancellationToken);
            }

            if (!await _store.StillOwnsAsync(investigation.Id, ProcessingStatus.GatheringEvidence, cancellationToken))
            {
                return true;
            }

            investigation.EvidenceProvider = _provider.Name;
            investigation.ProcessingStatus = ProcessingStatus.AnalyzingEvidence;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception exception) when (exception is EvidenceResponseException or HttpRequestException or TaskCanceledException)
        {
            var code = exception is EvidenceResponseException ? InvalidResponseCode : RequestFailedCode;
            investigation.ProcessingStatus = ProcessingStatus.Failed;
            investigation.FailureCode = code;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task<Evidence> MapAsync(
        Guid claimId,
        RetrievedEvidence item,
        Dictionary<string, Source> sourcesByUrl,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(item.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            throw new EvidenceResponseException(InvalidResponseCode);
        }

        if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Excerpt))
        {
            throw new EvidenceResponseException(InvalidResponseCode);
        }

        if (!sourcesByUrl.TryGetValue(uri.AbsoluteUri, out var source))
        {
            source = await _store.FindSourceByUrlAsync(uri.AbsoluteUri, cancellationToken);
        }

        if (source is null)
        {
            var band = SourceQualityBands.From(item.Factors);
            source = new Source
            {
                Id = Guid.CreateVersion7(),
                Url = uri.AbsoluteUri,
                Domain = uri.Host,
                Title = item.Title.Trim(),
                Author = item.Author?.Trim(),
                Publisher = item.Publisher?.Trim(),
                PublishedAt = item.PublishedAt,
                RetrievedAt = _clock.GetUtcNow(),
                Quality = new SourceQuality
                {
                    Id = Guid.CreateVersion7(),
                    Band = band,
                    FactorsJson = JsonSerializer.Serialize(item.Factors),
                    Explanation = Explanation(band)
                }
            };
            source.Quality.SourceId = source.Id;
            await _store.AddSourceAsync(source, cancellationToken);
            sourcesByUrl[uri.AbsoluteUri] = source;
        }
        else
        {
            sourcesByUrl[uri.AbsoluteUri] = source;
        }

        return new Evidence
        {
            Id = Guid.CreateVersion7(),
            ClaimId = claimId,
            SourceId = source.Id,
            EvidenceText = item.Excerpt.Trim()
        };
    }

    private static string Explanation(SourceQualityBand band) => band switch
    {
        SourceQualityBand.StrongIndicators => "HTTPS plus an institutional indicator and an author or date.",
        SourceQualityBand.Moderate => "HTTPS plus at least two other observed indicators.",
        _ => "Few publication indicators were observed."
    };
}

public sealed class EvidenceResponseException : Exception
{
    public EvidenceResponseException(string code)
    {
        Code = code;
    }

    public string Code { get; }
}
