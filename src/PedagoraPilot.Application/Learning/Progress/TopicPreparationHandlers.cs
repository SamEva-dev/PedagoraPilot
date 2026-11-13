using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Learning.Progress;

public sealed record SaveTopicPreparationCommand(EnrollmentId EnrollmentId, PedagogicalTopicId TopicId,
    string? Content, bool Submit) : ICommand<LearnerTopicProgressDto>;

public sealed class SaveTopicPreparationCommandHandler(
    IEnrollmentRepository enrollments,
    ICohortRepository cohorts,
    IPedagogicalTopicRepository topics,
    ILearnerTopicProgressRepository progress,
    ILearnerProfileRepository profiles,
    IPersonRepository people,
    IObjectMapper mapper,
    ICurrentUser current) : IRequestHandler<SaveTopicPreparationCommand, LearnerTopicProgressDto>
{
    public async Task<LearnerTopicProgressDto> Handle(SaveTopicPreparationCommand r, CancellationToken ct)
    {
        // Explicit student-only ownership check; a generic Manage permission cannot replace it.
        if (!LearnerSelfAccess.Applies(current))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var enrollment = await enrollments.GetByIdAsync(r.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        var profile = await LearnerSelfAccess.ResolveProfileAsync(profiles, people, current, ct);
        if (profile is null || profile.Id != enrollment.LearnerProfileId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        var topic = await topics.GetByIdAsync(r.TopicId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.PedagogicalTopicNotFound);
        if (topic.ReferentialVersionId != cohort.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.TopicReferentialMismatch);
        if (r.Content?.Length > 8000)
            throw new ValidationApplicationException("TOPIC_PREPARATION_TOO_LONG");
        var entity = await progress.FindAsync(r.EnrollmentId, r.TopicId, true, ct);
        if (entity is null)
        {
            entity = LearnerTopicProgress.Create(enrollment.OrganizationId, enrollment.Id, topic.Id);
            await progress.AddAsync(entity, ct);
        }
        entity.SavePreparation(r.Content, r.Submit);
        return LearningProgressDtoFactory.TopicProgress(entity, topic, mapper);
    }
}
