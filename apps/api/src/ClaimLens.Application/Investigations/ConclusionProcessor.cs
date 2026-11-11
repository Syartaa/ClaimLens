using ClaimLens.Domain.Investigations;

namespace ClaimLens.Application.Investigations;

public sealed class ConclusionProcessor
{
    public const string SummaryInvalidCode = "AI_RESPONSE_INVALID";

    private readonly IInvestigationStore _store;
    private readonly IAiAnalysisClient _ai;
    private readonly TimeProvider _clock;

    public ConclusionProcessor(IInvestigationStore store, IAiAnalysisClient ai, TimeProvider clock)
    {
        _store = store;
        _ai = ai;
        _clock = clock;
    }

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var investigation = await _store.ClaimNextConcludingAsync(cancellationToken);
        if (investigation is null)
        {
            return false;
        }

        var started = _clock.GetUtcNow();
        try
        {
            var claims = await _store.ListClaimsAsync(investigation.Id, cancellationToken);
            var evidence = await _store.ListEvidenceForClaimsAsync(claims.Select(claim => claim.Id).ToArray(), cancellationToken);
            var assessments = claims.Select(claim => new ClaimAssessment(
                claim.Id,
                claim.Position,
                claim.ImportanceScore,
                evidence.Where(item => item.ClaimId == claim.Id).Select(ToAssessment).ToArray())).ToArray();
            var calculated = ClaimVerdictCalculator.Calculate(assessments);
            var summary = await _ai.SummarizeAsync(
                calculated.Verdict.ToString(),
                calculated.Confidence,
                string.Join(" ", calculated.Claims.Select(claim => claim.Explanation)),
                cancellationToken);
            if (string.IsNullOrWhiteSpace(summary) || summary.Length > 2000)
            {
                throw new AiResponseException(SummaryInvalidCode);
            }

            if (!await _store.StillOwnsAsync(investigation.Id, ProcessingStatus.GeneratingConclusion, cancellationToken))
            {
                return true;
            }

            var now = _clock.GetUtcNow();
            await _store.AddAnalysisResultsAsync(calculated.Claims.Select(claim => new AnalysisResult
            {
                Id = Guid.CreateVersion7(),
                ClaimId = claim.ClaimId,
                Verdict = claim.Verdict,
                Confidence = claim.Confidence,
                Explanation = claim.Explanation,
                AlgorithmVersion = ClaimVerdictCalculator.AlgorithmVersion,
                GeneratedAt = now
            }).ToArray(), cancellationToken);
            await _store.AddAiRunAsync(new AiRun
            {
                Id = Guid.CreateVersion7(),
                InvestigationId = investigation.Id,
                Purpose = AiRunPurpose.Summary,
                PromptName = "summary-generation",
                PromptVersion = "v1",
                ModelUsed = "mock",
                Status = AiRunStatus.Succeeded,
                LatencyMs = (int)(_clock.GetUtcNow() - started).TotalMilliseconds,
                CreatedAt = now
            }, cancellationToken);

            investigation.Verdict = calculated.Verdict;
            investigation.ConfidenceScore = calculated.Confidence;
            investigation.Summary = summary.Trim();
            investigation.ProcessingStatus = ProcessingStatus.Completed;
            investigation.CompletedAt = now;
            investigation.UpdatedAt = now;
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return true;
        }
        catch (Exception exception) when (exception is AiResponseException or HttpRequestException or TaskCanceledException)
        {
            investigation.Verdict = null;
            investigation.ConfidenceScore = null;
            investigation.Summary = null;
            investigation.ProcessingStatus = ProcessingStatus.Failed;
            investigation.FailureCode = exception is AiResponseException ai ? ai.Code : ClaimExtractionProcessor.RequestFailedCode;
            investigation.UpdatedAt = _clock.GetUtcNow();
            await _store.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static EvidenceAssessment ToAssessment(Evidence evidence) =>
        new(
            evidence.Source.Domain,
            evidence.Stance,
            evidence.RelevanceScore,
            evidence.ConfidenceScore,
            evidence.Source.Quality?.Band);
}
