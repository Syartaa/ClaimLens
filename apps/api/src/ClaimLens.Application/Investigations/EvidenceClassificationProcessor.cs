using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public sealed class EvidenceClassificationProcessor
{
    private readonly IInvestigationStore _store;
    private readonly IAiAnalysisClient _ai;
    private readonly TimeProvider _clock;

    public EvidenceClassificationProcessor(IInvestigationStore store, IAiAnalysisClient ai, TimeProvider clock)
    {
        _store = store;
        _ai = ai;
        _clock = clock;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var investigation = await _store.ClaimNextAnalyzingAsync(cancellationToken);
        if (investigation is null)
        {
            return false;
        }

        var started = _clock.GetUtcNow();
        try
        {
            var claims = await _store.ListClaimsAsync(investigation.Id, cancellationToken);
            var evidence = await _store.ListEvidenceForClaimsAsync(claims.Select(claim => claim.Id).ToArray(), cancellationToken);
            var classified = new List<(Evidence Item, EvidenceClassification Result)>();
            foreach (var item in evidence)
            {
                var claim = claims.Single(candidate => candidate.Id == item.ClaimId);
                var result = await _ai.ClassifyEvidenceAsync(claim.ClaimText, item.EvidenceText, cancellationToken);
                classified.Add((item, result));
            }

            foreach (var (item, result) in classified)
            {
                var stance = ParseStance(result.Stance);
                item.AiStance = stance;
                item.Stance = stance;
                item.RelevanceScore = result.Relevance;
                item.ConfidenceScore = result.Confidence;
                item.Explanation = result.Explanation.Trim();
                item.PromptVersion = result.PromptVersion;
                item.ModelUsed = result.ModelUsed;
                await _store.AddAiRunAsync(new AiRun
                {
                    Id = Guid.CreateVersion7(),
                    InvestigationId = investigation.Id,
                    ClaimId = item.ClaimId,
                    Purpose = AiRunPurpose.EvidenceClassification,
                    PromptName = result.PromptName,
                    PromptVersion = result.PromptVersion,
                    ModelUsed = result.ModelUsed,
                    Status = AiRunStatus.Succeeded,
                    LatencyMs = (int)(_clock.GetUtcNow() - started).TotalMilliseconds,
                    CreatedAt = _clock.GetUtcNow()
                }, cancellationToken);
            }

            if (!await _store.StillOwnsAsync(investigation.Id, ProcessingStatus.AnalyzingEvidence, cancellationToken))
            {
                return true;
            }

            investigation.ProcessingStatus = ProcessingStatus.GeneratingConclusion;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception exception) when (exception is AiResponseException or HttpRequestException or TaskCanceledException)
        {
            investigation.ProcessingStatus = ProcessingStatus.Failed;
            investigation.FailureCode = exception is AiResponseException ai ? ai.Code : ClaimExtractionProcessor.RequestFailedCode;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.AddAiRunAsync(new AiRun
            {
                Id = Guid.CreateVersion7(),
                InvestigationId = investigation.Id,
                Purpose = AiRunPurpose.EvidenceClassification,
                PromptName = "evidence-classification",
                PromptVersion = "v1",
                ModelUsed = "unknown",
                Status = AiRunStatus.Failed,
                ErrorCategory = investigation.FailureCode,
                LatencyMs = (int)(_clock.GetUtcNow() - started).TotalMilliseconds,
                CreatedAt = _clock.GetUtcNow()
            }, cancellationToken);
            await _store.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static Stance ParseStance(string stance) => stance.ToLowerInvariant() switch
    {
        "supporting" => Stance.Supporting,
        "contradicting" => Stance.Contradicting,
        "neutral" => Stance.Neutral,
        _ => throw new AiResponseException(ClaimExtractionProcessor.InvalidResponseCode)
    };
}
