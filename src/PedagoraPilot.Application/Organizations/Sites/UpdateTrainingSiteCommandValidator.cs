using FluentValidation;

namespace PedagoraPilot.Application.Organizations.Sites;
public sealed class UpdateTrainingSiteCommandValidator : AbstractValidator<UpdateTrainingSiteCommand>
{
    public UpdateTrainingSiteCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithErrorCode("VALIDATION_REQUIRED");
        RuleFor(x => x.SiteId).NotEmpty().WithErrorCode("VALIDATION_REQUIRED");
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("SITE_CODE_REQUIRED").MaximumLength(32).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("SITE_NAME_REQUIRED").MaximumLength(160).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.City).NotEmpty().WithErrorCode("SITE_CITY_REQUIRED").MaximumLength(120).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Address).MaximumLength(240).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.PostalCode).MaximumLength(32).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Phone).MaximumLength(40).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Email).MaximumLength(240).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Manager).MaximumLength(160).WithErrorCode("VALIDATION_MAX_LENGTH");
        RuleFor(x => x.Status).Must(IsSupportedStatus).WithErrorCode("SITE_STATUS_INVALID");
    }

    private static bool IsSupportedStatus(string value)
        => value.Equals("active", StringComparison.OrdinalIgnoreCase)
           || value.Equals("attention", StringComparison.OrdinalIgnoreCase)
           || value.Equals("inactive", StringComparison.OrdinalIgnoreCase);
}
