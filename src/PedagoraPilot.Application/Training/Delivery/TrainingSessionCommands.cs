using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Delivery;
public sealed record CreateTrainingSessionCommand(CohortId CohortId, string Type, string Modality, string Title, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string TimeZoneId, string? TrainerAuthGateUserId, string? TrainerDisplayName, string? Location, string? Objective, string? Supports, string? Comments, string AudienceMode, IReadOnlyCollection<Guid>? ParticipantEnrollmentIds, string? ExternalKey) : ICommand<TrainingSessionDto>;
public sealed record UpdateTrainingSessionCommand(TrainingSessionId Id, string Type, string Modality, string Title, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string TimeZoneId, string? TrainerAuthGateUserId, string? TrainerDisplayName, string? Location, string? Objective, string? Supports, string? Comments, string AudienceMode, IReadOnlyCollection<Guid>? ParticipantEnrollmentIds) : ICommand<TrainingSessionDto>;
public sealed record CancelTrainingSessionCommand(TrainingSessionId Id) : ICommand<TrainingSessionDto>;
public sealed record CompleteTrainingSessionCommand(TrainingSessionId Id) : ICommand<TrainingSessionDto>;
public sealed record SaveAttendanceCommand(TrainingSessionId SessionId, IReadOnlyCollection<AttendanceEntryUpdateRequest> Entries) : ICommand<AttendanceSheetDto>;
public sealed class CreateTrainingSessionCommandValidator : AbstractValidator<CreateTrainingSessionCommand>
{
    public CreateTrainingSessionCommandValidator()
    {
        RuleFor(x => x.CohortId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Modality).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(240);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AudienceMode).NotEmpty();
        RuleFor(x => x).Must(x => x.EndsAtUtc > x.StartsAtUtc).WithMessage("SESSION_DATE_RANGE_INVALID");
    }
}

public sealed class UpdateTrainingSessionCommandValidator : AbstractValidator<UpdateTrainingSessionCommand>
{
    public UpdateTrainingSessionCommandValidator()
    {
        RuleFor(x => x.Id).Must(x => !x.IsEmpty);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Modality).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(240);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AudienceMode).NotEmpty();
        RuleFor(x => x).Must(x => x.EndsAtUtc > x.StartsAtUtc).WithMessage("SESSION_DATE_RANGE_INVALID");
    }
}

public sealed class SaveAttendanceCommandValidator : AbstractValidator<SaveAttendanceCommand>
{
    public SaveAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Entries).NotNull();
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.EnrollmentId).NotEmpty();
            entry.RuleFor(x => x.Status).NotEmpty();
            entry.RuleFor(x => x.PresentMinutes).GreaterThanOrEqualTo(0).When(x => x.PresentMinutes.HasValue);
        });
    }
}
