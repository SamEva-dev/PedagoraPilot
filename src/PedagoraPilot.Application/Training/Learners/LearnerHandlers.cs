using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Training.Learners;
public sealed class EnrollLearnerCommandHandler(ICohortRepository cohorts, IPersonRepository persons, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<EnrollLearnerCommand, LearnerDto>
{
    public async Task<LearnerDto> Handle(EnrollLearnerCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);
        var activeCount = await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
        if (activeCount >= cohort.Capacity)
            throw new ConflictApplicationException(ErrorKeys.CohortCapacityReached);
        var person = await persons.GetByEmailAsync(request.Email, true, ct);
        if (person is null)
        {
            person = Person.Create(request.FirstName, request.LastName, request.Email, request.Phone, request.BirthDate, request.PersonExternalKey);
            await persons.AddAsync(person, ct);
        }
        else
        {
            person.UpdateContact(request.FirstName, request.LastName, request.Email, request.Phone, request.BirthDate);
        }

        var learner = await learnerProfiles.GetByPersonIdAsync(person.Id, true, ct);
        if (learner is null)
        {
            learner = LearnerProfile.Create(person.Id, request.AuthGateUserId, request.LearnerExternalKey);
            await learnerProfiles.AddAsync(learner, ct);
        }
        else if (request.AuthGateUserId.HasValue && learner.AuthGateUserId is null)
        {
            learner.LinkAuthGateUser(request.AuthGateUserId.Value);
        }

        var existing = await enrollments.GetByLearnerAndCohortAsync(learner.Id, cohort.Id, false, ct);
        if (existing is not null)
            throw new ConflictApplicationException(ErrorKeys.LearnerAlreadyEnrolled);
        var enrollment = Enrollment.Create(cohort.OrganizationId, learner.Id, cohort.Id, request.EnrolledOn ?? DateOnly.FromDateTime(DateTime.UtcNow), request.EnrollmentExternalKey);
        await enrollments.AddAsync(enrollment, ct);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}

public sealed class GetCohortLearnersQueryHandler(ICohortRepository cohorts, IEnrollmentRepository enrollments, ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IObjectMapper mapper) : IRequestHandler<GetCohortLearnersQuery, IReadOnlyCollection<LearnerDto>>
{
    public async Task<IReadOnlyCollection<LearnerDto>> Handle(GetCohortLearnersQuery request, CancellationToken ct)
    {
        if (await cohorts.GetByIdAsync(request.CohortId, false, ct)is null)
            throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        var enrollmentList = await enrollments.Query(false).Where(x => x.CohortId == request.CohortId).OrderBy(x => x.EnrolledOn).ToListAsync(ct);
        if (enrollmentList.Count == 0)
            return Array.Empty<LearnerDto>();
        var learnerIds = enrollmentList.Select(x => x.LearnerProfileId).Distinct().ToArray();
        var learners = await learnerProfiles.Query(false).Where(x => learnerIds.Contains(x.Id)).ToListAsync(ct);
        var personIds = learners.Select(x => x.PersonId).Distinct().ToArray();
        var people = await persons.Query(false).Where(x => personIds.Contains(x.Id)).ToListAsync(ct);
        return enrollmentList.Select(enrollment =>
        {
            var learner = learners.Single(x => x.Id == enrollment.LearnerProfileId);
            var person = people.Single(x => x.Id == learner.PersonId);
            return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
        }).ToArray();
    }
}

public sealed class GetLearnerQueryHandler(ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IEnrollmentRepository enrollments, IObjectMapper mapper) : IRequestHandler<GetLearnerQuery, LearnerDto>
{
    public async Task<LearnerDto> Handle(GetLearnerQuery request, CancellationToken ct)
    {
        var learner = await learnerProfiles.GetByIdAsync(request.LearnerProfileId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var person = await persons.GetByIdAsync(learner.PersonId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        var enrollment = await enrollments.Query(false).Where(x => x.LearnerProfileId == learner.Id).OrderByDescending(x => x.EnrolledOn).FirstOrDefaultAsync(ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}

internal static class LearnerDtoFactory
{
    public static LearnerDto Create(Enrollment enrollment, LearnerProfile learner, Person person, IObjectMapper mapper)
    {
        var p = mapper.Map<Person, PersonReadModel>(person);
        var l = mapper.Map<LearnerProfile, LearnerProfileReadModel>(learner);
        var e = mapper.Map<Enrollment, EnrollmentReadModel>(enrollment);
        return new LearnerDto(e.Id.Value, l.Id.Value, p.Id.Value, e.CohortId.Value, p.FirstName, p.LastName, $"{p.FirstName} {p.LastName}".Trim(), p.Email, p.Phone, p.BirthDate, e.Status.ToString().ToLowerInvariant(), e.EnrolledOn, e.ExternalKey ?? l.ExternalKey ?? p.ExternalKey);
    }
}

public sealed class ChangeEnrollmentStatusCommandHandler(IEnrollmentRepository enrollments, ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IObjectMapper mapper) : IRequestHandler<ChangeEnrollmentStatusCommand, LearnerDto>
{
    public async Task<LearnerDto> Handle(ChangeEnrollmentStatusCommand request, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        if (!Enum.TryParse<EnrollmentStatus>(request.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.EnrollmentStatusInvalid);
        enrollment.ChangeStatus(status, request.EndedOn);
        var learner = await learnerProfiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var person = await persons.GetByIdAsync(learner.PersonId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}
