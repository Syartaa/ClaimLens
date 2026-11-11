using ClaimLens.Application.Common;
using ClaimLens.Application.Investigations;
using ClaimLens.Domain.Investigations;
using FluentValidation;

namespace ClaimLens.Application.Reviews;

public sealed class ReviewService
{
    public const string EvidenceConfirmed = "EvidenceConfirmed";
    public const string EvidenceOverridden = "EvidenceOverridden";
    public const string SourceFlagged = "SourceFlagged";
    public const string InvestigationApproved = "InvestigationApproved";

    private readonly IReviewStore _store;
    private readonly InvestigationService _investigations;
    private readonly IInvestigationProgress _progress;
    private readonly IValidator<ReviewEvidenceRequest> _evidenceValidator;
    private readonly IValidator<FlagSourceRequest> _flagValidator;
    private readonly IValidator<ApproveInvestigationRequest> _approveValidator;
    private readonly TimeProvider _clock;

    public ReviewService(
        IReviewStore store,
        InvestigationService investigations,
        IInvestigationProgress progress,
        IValidator<ReviewEvidenceRequest> evidenceValidator,
        IValidator<FlagSourceRequest> flagValidator,
        IValidator<ApproveInvestigationRequest> approveValidator,
        TimeProvider clock)
    {
        _store = store;
        _investigations = investigations;
        _progress = progress;
        _evidenceValidator = evidenceValidator;
        _flagValidator = flagValidator;
        _approveValidator = approveValidator;
        _clock = clock;
    }

    public async Task<PagedResponse<ReviewQueueItem>> QueueAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page must be at least 1.");
        }

        if (pageSize is < 1 or > 50)
        {
            throw new AppException("VALIDATION_FAILED", 400, "Page size must be between 1 and 50.");
        }

        var (items, total) = await _store.ListCompletedAsync(page, pageSize, cancellationToken);
        return new PagedResponse<ReviewQueueItem>(items, page, pageSize, total);
    }

    public async Task<InvestigationResponse> ReviewEvidenceAsync(
        Guid reviewerId,
        Guid evidenceId,
        ReviewEvidenceRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_evidenceValidator, request, cancellationToken);
        var evidence = await _store.FindEvidenceAsync(evidenceId, cancellationToken);
        if (evidence is null || evidence.Stance is null)
        {
            throw new AppException("EVIDENCE_NOT_FOUND", 404, "Evidence could not be found.");
        }

        var investigation = await RequireCompletedAsync(evidence.Claim.InvestigationId, cancellationToken);
        var next = Enum.Parse<Stance>(request.Stance);
        var previous = evidence.Stance.Value;
        var now = _clock.GetUtcNow();
        var note = request.Note.Trim();
        var confirmed = previous == next;

        await _store.AddReviewAsync(new EvidenceReview
        {
            Id = Guid.CreateVersion7(),
            EvidenceId = evidence.Id,
            ReviewerId = reviewerId,
            PreviousStance = previous,
            NewStance = next,
            Note = note,
            CreatedAt = now
        }, cancellationToken);

        evidence.Stance = next;
        var message = confirmed
            ? $"Stance {next} confirmed. {note}"
            : $"Stance changed from {previous} to {next}. {note}";
        await _store.AddAuditAsync(Audit(investigation.Id, reviewerId, confirmed ? EvidenceConfirmed : EvidenceOverridden, message, now), cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishAsync(investigation, cancellationToken);
        return await _investigations.GetForStaffAsync(investigation.Id, cancellationToken);
    }

    public async Task<InvestigationResponse> FlagSourceAsync(
        Guid reviewerId,
        Guid sourceId,
        FlagSourceRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_flagValidator, request, cancellationToken);
        var investigation = await RequireCompletedAsync(request.InvestigationId, cancellationToken);
        if (!await _store.SourceUsedByInvestigationAsync(sourceId, investigation.Id, cancellationToken))
        {
            throw new AppException("SOURCE_NOT_FOUND", 404, "Source could not be found.");
        }

        var source = await _store.FindSourceAsync(sourceId, cancellationToken);
        if (source is null)
        {
            throw new AppException("SOURCE_NOT_FOUND", 404, "Source could not be found.");
        }

        var now = _clock.GetUtcNow();
        var note = request.Note.Trim();
        source.FlaggedAt = now;
        source.FlagNote = note;
        source.FlaggedByUserId = reviewerId;
        await _store.AddAuditAsync(Audit(
            investigation.Id,
            reviewerId,
            SourceFlagged,
            $"Source flagged: {source.Title}. {note}",
            now), cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishAsync(investigation, cancellationToken);
        return await _investigations.GetForStaffAsync(investigation.Id, cancellationToken);
    }

    public async Task<InvestigationResponse> ApproveAsync(
        Guid reviewerId,
        Guid investigationId,
        ApproveInvestigationRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(_approveValidator, request, cancellationToken);
        var investigation = await RequireCompletedAsync(investigationId, cancellationToken);
        if (investigation.ApprovedAt is null)
        {
            var now = _clock.GetUtcNow();
            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            investigation.ApprovedAt = now;
            investigation.ApprovedByUserId = reviewerId;
            var message = note is null ? "Investigation approved." : $"Investigation approved. {note}";
            await _store.AddAuditAsync(Audit(investigation.Id, reviewerId, InvestigationApproved, message, now), cancellationToken);
            await _store.SaveChangesAsync(cancellationToken);
            await PublishAsync(investigation, cancellationToken);
        }

        return await _investigations.GetForStaffAsync(investigation.Id, cancellationToken);
    }

    private async Task<Investigation> RequireCompletedAsync(Guid investigationId, CancellationToken cancellationToken)
    {
        var investigation = await _store.FindInvestigationAsync(investigationId, cancellationToken);
        if (investigation is null)
        {
            throw new AppException("INVESTIGATION_NOT_FOUND", 404, "Investigation could not be found.");
        }

        if (investigation.ProcessingStatus != ProcessingStatus.Completed)
        {
            throw new AppException("INVESTIGATION_NOT_READY", 400, "Reviews are available after the investigation completes.");
        }

        return investigation;
    }

    private Task PublishAsync(Investigation investigation, CancellationToken cancellationToken) =>
        _progress.PublishAsync(
            investigation.Id,
            investigation.ProcessingStatus.ToString(),
            investigation.Verdict?.ToString(),
            investigation.FailureCode,
            cancellationToken);

    private static AuditEvent Audit(Guid investigationId, Guid actorUserId, string eventType, string message, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            InvestigationId = investigationId,
            ActorUserId = actorUserId,
            EventType = eventType,
            Message = message,
            Metadata = "{}",
            CreatedAt = createdAt
        };

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        throw new ValidationAppException(result.Errors
            .Select(error => new FieldError(error.PropertyName, error.ErrorMessage))
            .ToArray());
    }
}
