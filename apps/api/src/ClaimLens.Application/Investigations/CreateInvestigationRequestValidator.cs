using FluentValidation;

namespace ClaimLens.Application.Investigations;

public sealed class CreateInvestigationRequestValidator : AbstractValidator<CreateInvestigationRequest>
{
    public const int ClaimMaxLength = 2000;
    public const int ArticleMaxLength = 50000;

    public CreateInvestigationRequestValidator()
    {
        RuleFor(request => request.InputType)
            .NotEmpty()
            .Must(type => type is "Claim" or "ArticleText" or "Url")
            .WithMessage("Input type must be Claim, ArticleText, or Url.");

        RuleFor(request => request.Text).NotEmpty();

        RuleFor(request => request.Text)
            .MaximumLength(ClaimMaxLength)
            .When(request => request.InputType == "Claim")
            .WithMessage($"A claim can be at most {ClaimMaxLength} characters.");

        RuleFor(request => request.Text)
            .MaximumLength(ArticleMaxLength)
            .When(request => request.InputType == "ArticleText")
            .WithMessage($"Article text can be at most {ArticleMaxLength} characters.");

        RuleFor(request => request.Title).MaximumLength(200);
    }
}
