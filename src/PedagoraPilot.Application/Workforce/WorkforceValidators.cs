using FluentValidation;

namespace PedagoraPilot.Application.Workforce;
public sealed class CreateRemoteWorkCommandValidator : AbstractValidator<CreateRemoteWorkCommand>
{
    public CreateRemoteWorkCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Period).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleForEach(x => x.Activities).ChildRules(activity =>
        {
            activity.RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
            activity.RuleFor(x => x.Label).NotEmpty().MaximumLength(300);
        }).When(x => x.Activities is not null);
    }
}
