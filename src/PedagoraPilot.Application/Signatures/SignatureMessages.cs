using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Signatures;

public sealed record CreateSignatureDraftCommand(string SourceType, Guid SourceId, EnrollmentId EnrollmentId)
    : ICommand<SignatureEnvelopeDto>;
public sealed record GetSignatureDraftQuery(SignatureEnvelopeId Id) : IQuery<SignatureEnvelopeDto>;
public sealed record ListMySignatureDraftsQuery(Guid? CohortId) : IQuery<IReadOnlyCollection<SignatureEnvelopeDto>>;

public sealed class CreateSignatureDraftCommandValidator : AbstractValidator<CreateSignatureDraftCommand>
{
    public CreateSignatureDraftCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty();
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
    }
}

public sealed record RequestObservationSignatureCommand(SignatureEnvelopeId Id) : ICommand<SignatureEnvelopeDto>;
public sealed record GetSignatureContentQuery(SignatureEnvelopeId Id) : IQuery<SignatureContentDto>;
public sealed record RespondObservationSignatureCommand(SignatureEnvelopeId Id, bool Acknowledge, string? Comment)
    : ICommand<SignatureEnvelopeDto>;
public sealed record ListEnrollmentObservationSignaturesQuery(EnrollmentId EnrollmentId)
    : IQuery<IReadOnlyCollection<SignatureEnvelopeDto>>;
public sealed record ListMyPendingObservationsQuery() : IQuery<IReadOnlyCollection<SignatureEnvelopeDto>>;
public sealed record ListMyDailyObservationsQuery(DateOnly Date) : IQuery<IReadOnlyCollection<DailyObservationSignatureCandidateDto>>;

public sealed record RequestAttendanceSignatureCommand(SignatureEnvelopeId Id) : ICommand<SignatureEnvelopeDto>;
public sealed record RespondAttendanceSignatureCommand(SignatureEnvelopeId Id, bool Acknowledge, string? Comment)
    : ICommand<SignatureEnvelopeDto>;
public sealed record GetAttendanceSignatureContentQuery(SignatureEnvelopeId Id) : IQuery<SignatureContentDto>;
public sealed record ListMyAttendanceSignaturesQuery() : IQuery<IReadOnlyCollection<SignatureEnvelopeDto>>;
public sealed record ListSessionAttendanceSignaturesQuery(TrainingSessionId SessionId)
    : IQuery<IReadOnlyCollection<SignatureEnvelopeDto>>;
