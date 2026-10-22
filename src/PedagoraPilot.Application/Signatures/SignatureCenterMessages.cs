using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Signatures;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Signatures;

public sealed record ListSignatureCenterQuery(int Page, int PageSize, string? Status, Guid? CohortId, bool Team)
    : IQuery<SignatureCenterPageDto>;
public sealed record RemindSignatureCommand(SignatureEnvelopeId Id) : ICommand<SignatureEnvelopeDto>;
public sealed record RequestSignatureBatchCommand(IReadOnlyCollection<SignatureEnvelopeId> Ids)
    : ICommand<SignatureBatchResultDto>;
public sealed record CreateDailyObservationSummaryCommand(EnrollmentId EnrollmentId, DateOnly Date,
    IReadOnlyCollection<Guid> EvaluationIds) : ICommand<SignatureEnvelopeDto>;

public sealed class RequestSignatureBatchCommandValidator : AbstractValidator<RequestSignatureBatchCommand>
{
    public RequestSignatureBatchCommandValidator() => RuleFor(x => x.Ids)
        .Must(ids => ids is { Count: > 0 and <= 50 } && ids.All(id => !id.IsEmpty) && ids.Distinct().Count() == ids.Count)
        .WithMessage("SIGNATURE_BATCH_INVALID");
}
public sealed class CreateDailyObservationSummaryCommandValidator : AbstractValidator<CreateDailyObservationSummaryCommand>
{
    public CreateDailyObservationSummaryCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.EvaluationIds).Must(x => x is { Count: > 0 and <= 20 } &&
            x.All(id => id != Guid.Empty) && x.Distinct().Count() == x.Count);
    }
}
