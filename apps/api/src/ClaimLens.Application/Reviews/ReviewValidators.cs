using FluentValidation;

namespace ClaimLens.Application.Reviews;

public sealed class ReviewEvidenceRequestValidator : AbstractValidator<ReviewEvidenceRequest>
{
    public ReviewEvidenceRequestValidator()
    {
        RuleFor(request => request.Stance)
            .Must(stance => stance is "Supporting" or "Contradicting" or "Neutral")
            .WithMessage("Stance must be Supporting, Contradicting, or Neutral.");
        RuleFor(request => request.Note).NotEmpty().MaximumLength(1000);
    }
}

public sealed class FlagSourceRequestValidator : AbstractValidator<FlagSourceRequest>
{
    public FlagSourceRequestValidator()
    {
        RuleFor(request => request.InvestigationId).NotEmpty();
        RuleFor(request => request.Note).NotEmpty().MaximumLength(1000);
    }
}

public sealed class ApproveInvestigationRequestValidator : AbstractValidator<ApproveInvestigationRequest>
{
    public ApproveInvestigationRequestValidator()
    {
        RuleFor(request => request.Note).MaximumLength(1000);
    }
}
