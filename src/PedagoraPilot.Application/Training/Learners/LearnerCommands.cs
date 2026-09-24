using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Learners;
public sealed record EnrollLearnerCommand(CohortId CohortId, string FirstName, string LastName, string Email, string? Phone, DateOnly? BirthDate, DateOnly? EnrolledOn, Guid? AuthGateUserId, string? PersonExternalKey, string? LearnerExternalKey, string? EnrollmentExternalKey) : ICommand<LearnerDto>;
public sealed record ChangeEnrollmentStatusCommand(EnrollmentId EnrollmentId, string Status, DateOnly? EndedOn) : ICommand<LearnerDto>;
public sealed class EnrollLearnerCommandValidator : AbstractValidator<EnrollLearnerCommand>
{
    public EnrollLearnerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(40);
    }
}
