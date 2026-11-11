using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public sealed class ClaimExtractionProcessor
{
    public const string InvalidResponseCode = "AI_RESPONSE_INVALID";
    public const string RequestFailedCode = "AI_REQUEST_FAILED";

    private readonly IInvestigationStore _store;
    private readonly IAiAnalysisClient _ai;
    private readonly TimeProvider _clock;

    public ClaimExtractionProcessor(IInvestigationStore store, IAiAnalysisClient ai, TimeProvider clock)
    {
        _store = store;
        _ai = ai;
        _clock = clock;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var investigation = await _store.ClaimNextPendingAsync(cancellationToken);
        if (investigation is null)
        {
            return false;
        }

        var started = _clock.GetUtcNow();
        try
        {
            var result = await _ai.ExtractClaimsAsync(investigation.OriginalInput, investigation.InputType.ToString(), cancellationToken);
            if (result.Claims.Count == 0)
            {
                throw new AiResponseException(InvalidResponseCode);
            }

            var claims = result.Claims.Select((claim, index) => new Claim
            {
                Id = Guid.CreateVersion7(),
                InvestigationId = investigation.Id,
                Position = index + 1,
                ClaimText = claim.Text.Trim(),
                NormalizedClaim = claim.NormalizedText.Trim(),
                ClaimType = ParseType(claim.Type),
                ImportanceScore = claim.Importance,
                CreatedAt = _clock.GetUtcNow()
            }).ToArray();

            await _store.AddClaimsAsync(claims, cancellationToken);
            await _store.AddAiRunAsync(new AiRun
            {
                Id = Guid.CreateVersion7(),
                InvestigationId = investigation.Id,
                Purpose = AiRunPurpose.ClaimExtraction,
                PromptName = result.PromptName,
                PromptVersion = result.PromptVersion,
                ModelUsed = result.ModelUsed,
                Status = AiRunStatus.Succeeded,
                LatencyMs = (int)(_clock.GetUtcNow() - started).TotalMilliseconds,
                CreatedAt = _clock.GetUtcNow()
            }, cancellationToken);

            if (!await _store.StillOwnsAsync(investigation.Id, ProcessingStatus.ExtractingClaims, cancellationToken))
            {
                return true;
            }

            investigation.ProcessingStatus = ProcessingStatus.GatheringEvidence;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception exception) when (exception is AiResponseException or HttpRequestException or TaskCanceledException)
        {
            var code = exception is AiResponseException ai ? ai.Code : RequestFailedCode;
            await FailAsync(investigation, code, started, cancellationToken);
        }

        return true;
    }

    private async Task FailAsync(Investigation investigation, string code, DateTimeOffset started, CancellationToken cancellationToken)
    {
        investigation.ProcessingStatus = ProcessingStatus.Failed;
        investigation.FailureCode = code;
        investigation.UpdatedAt = _clock.GetUtcNow();
        await _store.AddAiRunAsync(new AiRun
        {
            Id = Guid.CreateVersion7(),
            InvestigationId = investigation.Id,
            Purpose = AiRunPurpose.ClaimExtraction,
            PromptName = "claim-extraction",
            PromptVersion = "v1",
            ModelUsed = "unknown",
            Status = AiRunStatus.Failed,
            ErrorCategory = code,
            LatencyMs = (int)(_clock.GetUtcNow() - started).TotalMilliseconds,
            CreatedAt = _clock.GetUtcNow()
        }, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
    }

    private static ClaimType ParseType(string type) => type.ToLowerInvariant() switch
    {
        "factual" => ClaimType.Factual,
        "statistical" => ClaimType.Statistical,
        "scientific" => ClaimType.Scientific,
        "historical" => ClaimType.Historical,
        "medical" => ClaimType.Medical,
        "political" => ClaimType.Political,
        "other" => ClaimType.Other,
        _ => throw new AiResponseException(InvalidResponseCode)
    };
}

public sealed class AiResponseException : Exception
{
    public AiResponseException(string code)
    {
        Code = code;
    }

    public string Code { get; }
}
