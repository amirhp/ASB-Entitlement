using FluentValidation;

namespace ASB.Entitlements.Application.Commands.CreateIdentity;

public sealed class CreateIdentityCommandValidator : AbstractValidator<CreateIdentityCommand>
{
    public CreateIdentityCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Identity ID is required")
            .MaximumLength(100).WithMessage("Identity ID must not exceed 100 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Identity name is required")
            .MaximumLength(200).WithMessage("Identity name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid identity type");
    }
}
