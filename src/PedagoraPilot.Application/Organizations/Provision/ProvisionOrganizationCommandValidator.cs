using FluentValidation;

namespace PedagoraPilot.Application.Organizations.Provision;
public sealed class ProvisionOrganizationCommandValidator : AbstractValidator<ProvisionOrganizationCommand>
{
    public ProvisionOrganizationCommandValidator()
    {
        RuleFor(x => x.ExternalUserId).NotEmpty().WithErrorCode("VALIDATION_REQUIRED");
        RuleFor(x => x.LegalName).NotEmpty().WithErrorCode("VALIDATION_REQUIRED").MaximumLength(200).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.CountryCode).Length(2).WithErrorCode("VALIDATION_EXACT_LENGTH").When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
        RuleFor(x => x.FirstName).NotEmpty().WithErrorCode("VALIDATION_REQUIRED").MaximumLength(100).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.LastName).NotEmpty().WithErrorCode("VALIDATION_REQUIRED").MaximumLength(100).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Email).NotEmpty().WithErrorCode("VALIDATION_REQUIRED").EmailAddress().WithErrorCode("VALIDATION_EMAIL").MaximumLength(256).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Phone).MaximumLength(40).WithErrorCode("VALIDATION_MAX_LENGTH").When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
