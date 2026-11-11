using ClaimLens.Application.Common;
using ClaimLens.Domain.Investigations;
using FluentValidation;

namespace ClaimLens.Application.Investigations;

public sealed class InvestigationService
{
    private readonly IInvestigationStore _store;
    private readonly IValidator<CreateInvestigationRequest> _createValidator;
    private readonly TimeProvider _clock;

    public InvestigationService(
        IInvestigationStore store,
        IValidator<CreateInvestigationRequest> createValidator,
        TimeProvider clock)
    {
        _store = store;
        _createValidator = createValidator;
        _clock = clock;
    }

    public async Task<InvestigationResponse> CreateAsync(
        Guid userId,
        CreateInvestigationRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);

        if (!Enum.TryParse<InputType>(request.InputType, out var inputType))
        {
            throw new AppException("VALIDATION_FAILED", 400, "Input type is not supported.");
        }

        if (inputType == InputType.Url)
        {
            throw new AppException("INPUT_TYPE_NOT_SUPPORTED", 400, "URL investigations are not available yet.");
        }

        var now = _clock.GetUtcNow();
        var text = request.Text.Trim();
        var investigation = new Investigation
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? TitleFrom(text) : request.Title.Trim(),
            OriginalInput = text,
            InputType = inputType,
            ProcessingStatus = ProcessingStatus.Pending,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _store.AddAsync(investigation, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        return ToResponse(investigation, [], null, []);
    }

    public async Task<PagedResponse<InvestigationListItem>> ListAsync(
        Guid userId,
        InvestigationListQuery query,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(query);
        var (items, total) = await _store.ListForUserAsync(userId, normalized, cancellationToken);
        return new PagedResponse<InvestigationListItem>(
            items.Select(ToListItem).ToArray(),
            normalized.Page,
            normalized.PageSize,
            total);
    }

    public async Task<InvestigationResponse> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var investigation = await FindOwnedAsync(userId, id, cancellationToken);
        return await RespondAsync(investigation, cancellationToken);
    }

    public async Task<InvestigationResponse> GetForStaffAsync(Guid id, CancellationToken cancellationToken)
    {
        var investigation = await _store.FindByIdAsync(id, cancellationToken);
        if (investigation is null)
        {
            throw new AppException("INVESTIGATION_NOT_FOUND", 404, "Investigation could not be found.");
        }

        return await RespondAsync(investigation, cancellationToken);
    }

    public async Task<InvestigationSummary> SummarizeAsync(Guid userId, CancellationToken cancellationToken) =>
        await _store.SummarizeForUserAsync(userId, cancellationToken);

    public Task<InvestigationResponse> ReanalyzeAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        ReanalyzeOwnedAsync(FindOwnedAsync(userId, id, cancellationToken), cancellationToken);

    public Task<InvestigationResponse> ReanalyzeForAdminAsync(Guid id, CancellationToken cancellationToken) =>
        ReanalyzeOwnedAsync(FindAnyAsync(id, cancellationToken), cancellationToken);

    private async Task<InvestigationResponse> ReanalyzeOwnedAsync(Task<Investigation> lookup, CancellationToken cancellationToken)
    {
        var investigation = await lookup;
        await _store.RemoveClaimsAsync(investigation.Id, cancellationToken);
        var now = _clock.GetUtcNow();
        investigation.ProcessingStatus = ProcessingStatus.Pending;
        investigation.Verdict = null;
        investigation.ConfidenceScore = null;
        investigation.Summary = null;
        investigation.FailureCode = null;
        investigation.EvidenceProvider = null;
        investigation.CompletedAt = null;
        investigation.ApprovedAt = null;
        investigation.ApprovedByUserId = null;
        investigation.AttemptCount = 0;
        investigation.UpdatedAt = now;
        await _store.SaveChangesAsync(cancellationToken);
        return await RespondAsync(investigation, cancellationToken);
    }

    public async Task<InvestigationResponse> CancelAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var investigation = await FindOwnedAsync(userId, id, cancellationToken);
        if (investigation.ProcessingStatus is ProcessingStatus.Completed or ProcessingStatus.Failed)
        {
            throw new AppException("INVESTIGATION_NOT_RUNNING", 400, "Only an in-progress investigation can be cancelled.");
        }

        investigation.ProcessingStatus = ProcessingStatus.Failed;
        investigation.FailureCode = WorkerLimits.CancelledCode;
        investigation.Verdict = null;
        investigation.ConfidenceScore = null;
        investigation.Summary = null;
        investigation.CompletedAt = null;
        investigation.ApprovedAt = null;
        investigation.ApprovedByUserId = null;
        investigation.UpdatedAt = _clock.GetUtcNow();
        await _store.SaveChangesAsync(cancellationToken);
        return await RespondAsync(investigation, cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var investigation = await FindOwnedAsync(userId, id, cancellationToken);
        _store.Remove(investigation);
        await _store.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteForAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var investigation = await FindAnyAsync(id, cancellationToken);
        _store.Remove(investigation);
        await _store.SaveChangesAsync(cancellationToken);
    }

    private async Task<Investigation> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var investigation = await _store.FindForUserAsync(id, userId, cancellationToken);
        if (investigation is null)
        {
            throw new AppException("INVESTIGATION_NOT_FOUND", 404, "Investigation could not be found.");
        }

        return investigation;
    }

    private async Task<Investigation> FindAnyAsync(Guid id, CancellationToken cancellationToken)
    {
        var investigation = await _store.FindByIdAsync(id, cancellationToken);
        if (investigation is null)
        {
            throw new AppException("INVESTIGATION_NOT_FOUND", 404, "Investigation could not be found.");
        }

        return investigation;
    }

    private static InvestigationListQuery Normalize(InvestigationListQuery query)
    {
        if (query.Page < 1)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page must be at least 1.");
        }

        if (query.PageSize is < 1 or > 50)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page size must be between 1 and 50.");
        }

        var sort = query.Sort is "createdAt" or "-createdAt" or "updatedAt" or "-updatedAt"
            ? query.Sort
            : "-createdAt";

        return query with { Sort = sort };
    }

    private async Task ValidateAsync(CreateInvestigationRequest request, CancellationToken cancellationToken)
    {
        var result = await _createValidator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        throw new ValidationAppException(result.Errors
            .Select(error => new FieldError(error.PropertyName, error.ErrorMessage))
            .ToArray());
    }

    private static string TitleFrom(string text)
    {
        var line = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Investigation";
        return line.Length <= 120 ? line : line[..120];
    }

    private async Task<InvestigationResponse> RespondAsync(Investigation investigation, CancellationToken cancellationToken)
    {
        var claims = await _store.ListClaimsAsync(investigation.Id, cancellationToken);
        var evidence = await _store.ListEvidenceForClaimsAsync(claims.Select(claim => claim.Id).ToArray(), cancellationToken);
        var audits = await _store.ListAuditsAsync(investigation.Id, cancellationToken);
        return ToResponse(investigation, claims, evidence, audits);
    }

    private static InvestigationResponse ToResponse(
        Investigation investigation,
        IReadOnlyList<Claim> claims,
        IReadOnlyList<Evidence>? evidence,
        IReadOnlyList<AuditEvent> audits) =>
        new(
            investigation.Id,
            investigation.Title,
            investigation.InputType.ToString(),
            investigation.ProcessingStatus.ToString(),
            investigation.Verdict?.ToString(),
            investigation.ConfidenceScore,
            investigation.Summary,
            investigation.FailureCode,
            investigation.OriginalInput,
            investigation.CreatedAt,
            investigation.UpdatedAt,
            investigation.CompletedAt,
            investigation.EvidenceProvider,
            claims.Select(claim => ToClaim(claim, evidence)).ToArray(),
            Timeline(investigation, claims, evidence, audits),
            investigation.UserId,
            investigation.ApprovedAt);

    private static ClaimResponse ToClaim(Claim claim, IReadOnlyList<Evidence>? evidence)
    {
        var items = evidence?.Where(item => item.ClaimId == claim.Id).Select(ToEvidence).ToArray() ?? [];
        return new(claim.Id, claim.Position, claim.ClaimText, claim.NormalizedClaim, claim.ClaimType.ToString(), claim.ImportanceScore, items);
    }

    private static EvidenceResponse ToEvidence(Evidence evidence)
    {
        var latest = (evidence.Reviews ?? []).OrderByDescending(review => review.CreatedAt).FirstOrDefault();
        return new(
            evidence.Id,
            evidence.Source.Title,
            evidence.Source.Domain,
            evidence.Source.Url,
            evidence.Source.PublishedAt,
            evidence.EvidenceText,
            evidence.Source.Quality?.Band.ToString(),
            evidence.AiStance?.ToString(),
            evidence.Stance?.ToString(),
            evidence.RelevanceScore,
            evidence.ConfidenceScore,
            evidence.Explanation,
            evidence.Source.Publisher,
            evidence.Source.FlaggedAt is not null,
            evidence.Source.FlagNote,
            latest?.Note,
            evidence.SourceId);
    }

    private static IReadOnlyList<TimelineEvent> Timeline(
        Investigation investigation,
        IReadOnlyList<Claim> claims,
        IReadOnlyList<Evidence>? evidence,
        IReadOnlyList<AuditEvent> audits)
    {
        var events = new List<TimelineEvent>
        {
            new("Created", "Investigation created.", investigation.CreatedAt)
        };

        if (claims.Count > 0)
        {
            events.Add(new("ClaimsExtracted", $"{claims.Count} claim(s) extracted.", claims.Min(claim => claim.CreatedAt)));
        }

        var items = evidence ?? [];
        if (items.Count > 0)
        {
            events.Add(new("EvidenceGathered", $"{items.Count} source excerpt(s) saved.", investigation.UpdatedAt));
        }

        if (items.Any(item => item.Stance is not null))
        {
            events.Add(new("EvidenceClassified", "Excerpts were classified. The model did not choose the verdict.", investigation.UpdatedAt));
        }

        if (investigation.ProcessingStatus == ProcessingStatus.Completed && investigation.CompletedAt is not null)
        {
            events.Add(new("ConclusionCalculated", $"Verdict {investigation.Verdict}. System confidence is not the probability the claim is true.", investigation.CompletedAt.Value));
        }

        if (investigation.ProcessingStatus == ProcessingStatus.Failed)
        {
            events.Add(new("Failed", "The run failed. No missing result was invented.", investigation.UpdatedAt));
        }

        events.AddRange(audits.Select(audit => new TimelineEvent(audit.EventType, audit.Message, audit.CreatedAt)));
        return events.OrderBy(timelineEvent => timelineEvent.At).ToArray();
    }

    private static InvestigationListItem ToListItem(Investigation investigation) =>
        new(
            investigation.Id,
            investigation.Title,
            investigation.InputType.ToString(),
            investigation.ProcessingStatus.ToString(),
            investigation.Verdict?.ToString(),
            investigation.ConfidenceScore,
            investigation.CreatedAt,
            investigation.CompletedAt);
}
