using FluentValidation;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed class CreateTrainingSiteCommandValidator : AbstractValidator<CreateTrainingSiteCommand>
{
    public CreateTrainingSiteCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithErrorCode("VALIDATION_REQUIRED");
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("SITE_CODE_REQUIRED").MaximumLength(32).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("SITE_NAME_REQUIRED").MaximumLength(160).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.City).NotEmpty().WithErrorCode("SITE_CITY_REQUIRED").MaximumLength(120).WithErrorCode("VALIDATION_MAX_LENGTH");
    }
}
