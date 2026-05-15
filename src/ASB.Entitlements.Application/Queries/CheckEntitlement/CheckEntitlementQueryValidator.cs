using FluentValidation;

namespace ASB.Entitlements.Application.Queries.CheckEntitlement;

public sealed class CheckEntitlementQueryValidator : AbstractValidator<CheckEntitlementQuery>
{
    public CheckEntitlementQueryValidator()
    {
        RuleFor(x => x.IdentityId)
            .NotEmpty().WithMessage("Identity ID is required")
            .MaximumLength(100).WithMessage("Identity ID cannot exceed 100 characters");

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required")
            .MaximumLength(100).WithMessage("Resource ID cannot exceed 100 characters");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required")
            .MaximumLength(50).WithMessage("Action cannot exceed 50 characters")
            .Matches("^[a-zA-Z]+$").WithMessage("Action must contain only letters");
    }
}
