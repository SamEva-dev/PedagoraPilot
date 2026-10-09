using PedagoraPilot.Application.Abstractions.Security;
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
public sealed class EnrollLearnerCommandHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, IPersonRepository persons, ILearnerProfileRepository learnerProfiles, IEnrollmentRepository enrollments, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<EnrollLearnerCommand, LearnerDto>
{
    public async Task<LearnerDto> Handle(EnrollLearnerCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        await LearnerContextualAccess.EnsureCohortAsync(cohort, offerings, current, manage: true, ct);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);
        // UnitOfWorkBehavior opens the transaction. Serialize all seat consumers against the cohort row.
        await cohorts.LockForEnrollmentCapacityAsync(cohort.Id, ct);
        cohort = await cohorts.GetByIdAsync(cohort.Id, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);
        var person = await persons.GetByEmailAsync(request.Email, true, ct);
        // Person and learner profiles are global rows. Reusing an email must not
        // disclose or overwrite another tenant's personal data.
        if (person is not null && !TenantScope.IsPlatformAdministrator(current))
        {
            var existingProfile = await learnerProfiles.GetByPersonIdAsync(person.Id, false, ct);
            var alreadyKnownToTenant = existingProfile is not null && await enrollments.Query(false)
                .AnyAsync(x => x.LearnerProfileId == existingProfile.Id && x.OrganizationId == cohort.OrganizationId, ct);
            if (!alreadyKnownToTenant)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        }
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
        var occupied = await enrollments.CountSeatOccupyingByCohortAsync(cohort.Id, ct);
        if (occupied >= cohort.Capacity)
            throw new ConflictApplicationException(ErrorKeys.CohortCapacityReached);
        var enrollment = Enrollment.Create(cohort.OrganizationId, learner.Id, cohort.Id, request.EnrolledOn ?? DateOnly.FromDateTime(DateTime.UtcNow), request.EnrollmentExternalKey);
        await enrollments.AddAsync(enrollment, ct);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}

public sealed class GetLearnerDirectoryQueryHandler(
    IOrganizationMemberRepository members,
    IEnrollmentRepository enrollments,
    ILearnerProfileRepository learnerProfiles,
    IPersonRepository persons,
    ICohortRepository cohorts,
    ICurrentUser current) : IRequestHandler<GetLearnerDirectoryQuery, IReadOnlyCollection<LearnerDirectoryDto>>
{
    public async Task<IReadOnlyCollection<LearnerDirectoryDto>> Handle(GetLearnerDirectoryQuery request, CancellationToken ct)
    {
        var organizationId = current.OrganizationId
            ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var studentMembers = await members.GetStudentDirectoryAsync(organizationId, ct);
        var studentAuthGateIds = studentMembers.Select(x => x.AuthGateUserId).Distinct().ToArray();

        var orgEnrollments = await enrollments.Query(false)
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.Status == EnrollmentStatus.Active)
            .ThenByDescending(x => x.EnrolledOn)
            .ToListAsync(ct);
        var enrolledLearnerIds = orgEnrollments.Select(x => x.LearnerProfileId).Distinct().ToArray();

        var profiles = await learnerProfiles.Query(false)
            .Where(x => enrolledLearnerIds.Contains(x.Id)
                        || (x.AuthGateUserId.HasValue && studentAuthGateIds.Contains(x.AuthGateUserId.Value)))
            .ToListAsync(ct);
        if (profiles.Count == 0) return Array.Empty<LearnerDirectoryDto>();

        var personIds = profiles.Select(x => x.PersonId).Distinct().ToArray();
        var people = await persons.Query(false).Where(x => personIds.Contains(x.Id)).ToListAsync(ct);
        var peopleById = people.ToDictionary(x => x.Id);

        var cohortIds = orgEnrollments.Select(x => x.CohortId).Distinct().ToArray();
        var cohortRows = cohortIds.Length == 0
            ? new List<Cohort>()
            : await cohorts.Query(false).Where(x => cohortIds.Contains(x.Id)).ToListAsync(ct);
        var cohortById = cohortRows.ToDictionary(x => x.Id);
        var latestEnrollmentByLearner = orgEnrollments
            .GroupBy(x => x.LearnerProfileId)
            .ToDictionary(x => x.Key, x => x.First());

        var activeStudentAuthGateIds = studentAuthGateIds.ToHashSet();

        return profiles
            .Where(x => peopleById.ContainsKey(x.PersonId))
            .Where(profile => !profile.AuthGateUserId.HasValue
                ? latestEnrollmentByLearner.ContainsKey(profile.Id)
                : activeStudentAuthGateIds.Contains(profile.AuthGateUserId.Value))
            .Select(profile =>
            {
                var person = peopleById[profile.PersonId];
                latestEnrollmentByLearner.TryGetValue(profile.Id, out var enrollment);
                Cohort? cohort = null;
                if (enrollment is not null) cohortById.TryGetValue(enrollment.CohortId, out cohort);
                return new LearnerDirectoryDto(
                    profile.Id.Value,
                    person.Id.Value,
                    profile.AuthGateUserId,
                    person.FirstName,
                    person.LastName,
                    person.DisplayName,
                    person.Email,
                    person.Phone,
                    person.BirthDate,
                    enrollment?.Id.Value,
                    enrollment?.CohortId.Value,
                    cohort?.Name,
                    enrollment?.Status.ToString(),
                    enrollment?.EnrolledOn);
            })
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToArray();
    }
}

public sealed class GetCohortLearnersQueryHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments, ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IOrganizationMemberRepository members, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetCohortLearnersQuery, IReadOnlyCollection<LearnerDto>>
{
    public async Task<IReadOnlyCollection<LearnerDto>> Handle(GetCohortLearnersQuery request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        await LearnerContextualAccess.EnsureCohortAsync(cohort, offerings, current, manage: false, ct);
        var enrollmentQuery = enrollments.Query(false).Where(x => x.CohortId == request.CohortId);
        if (LearnerSelfAccess.Applies(current))
        {
            var self = current.UserId.HasValue
                ? await learnerProfiles.Query(false).FirstOrDefaultAsync(x => x.AuthGateUserId == current.UserId.Value, ct)
                : null;
            if (self is null && !string.IsNullOrWhiteSpace(current.Email))
            {
                var ownPerson = await persons.GetByEmailAsync(current.Email, false, ct);
                if (ownPerson is not null)
                    self = await learnerProfiles.GetByPersonIdAsync(ownPerson.Id, false, ct);
            }
            if (self is null || (self.AuthGateUserId.HasValue && self.AuthGateUserId != current.UserId))
                return Array.Empty<LearnerDto>();
            enrollmentQuery = enrollmentQuery.Where(x => x.LearnerProfileId == self.Id);
        }
        var enrollmentList = await enrollmentQuery.OrderBy(x => x.EnrolledOn).ToListAsync(ct);
        if (enrollmentList.Count == 0)
            return Array.Empty<LearnerDto>();

        var learnerIds = enrollmentList.Select(x => x.LearnerProfileId).Distinct().ToArray();
        var learners = await learnerProfiles.Query(false).Where(x => learnerIds.Contains(x.Id)).ToListAsync(ct);
        if (learners.Count == 0)
            return Array.Empty<LearnerDto>();

        if (!LearnerSelfAccess.Applies(current))
        {
            var activeStudentAuthGateIds = (await members.GetStudentDirectoryAsync(cohort.OrganizationId, ct))
                .Select(x => x.AuthGateUserId)
                .ToHashSet();

            learners = learners
                .Where(x => !x.AuthGateUserId.HasValue || activeStudentAuthGateIds.Contains(x.AuthGateUserId.Value))
                .ToList();
            if (learners.Count == 0)
                return Array.Empty<LearnerDto>();

            var visibleLearnerIds = learners.Select(x => x.Id).ToHashSet();
            enrollmentList = enrollmentList.Where(x => visibleLearnerIds.Contains(x.LearnerProfileId)).ToList();
            if (enrollmentList.Count == 0)
                return Array.Empty<LearnerDto>();
        }

        var personIds = learners.Select(x => x.PersonId).Distinct().ToArray();
        var people = await persons.Query(false).Where(x => personIds.Contains(x.Id)).ToListAsync(ct);
        var learnerById = learners.ToDictionary(x => x.Id);
        var personById = people.ToDictionary(x => x.Id);
        return enrollmentList
            .Where(enrollment => learnerById.ContainsKey(enrollment.LearnerProfileId) && personById.ContainsKey(learnerById[enrollment.LearnerProfileId].PersonId))
            .Select(enrollment =>
            {
                var learner = learnerById[enrollment.LearnerProfileId];
                var person = personById[learner.PersonId];
                return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
            })
            .ToArray();
    }
}

public sealed class GetLearnerQueryHandler(ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<GetLearnerQuery, LearnerDto>
{
    public async Task<LearnerDto> Handle(GetLearnerQuery request, CancellationToken ct)
    {
        var learner = await learnerProfiles.GetByIdAsync(request.LearnerProfileId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var organizationId = TenantScope.Organization(current);
        var person = await persons.GetByIdAsync(learner.PersonId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        var enrollment = await enrollments.Query(false).Where(x => x.LearnerProfileId == learner.Id && (!organizationId.HasValue || x.OrganizationId == organizationId)).OrderByDescending(x => x.EnrolledOn).FirstOrDefaultAsync(ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerContextualAccess.EnsureEnrollmentAsync(enrollment, cohorts, offerings, current, manage: false, ct);
        await LearnerSelfAccess.EnsureAsync(enrollment, learnerProfiles, persons, current, ct);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}

public sealed class GetEnrollmentLearnerQueryHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ILearnerProfileRepository profiles, IPersonRepository people, IObjectMapper mapper, ICurrentUser current)
    : IRequestHandler<GetEnrollmentLearnerQuery, LearnerDto>
{
    public async Task<LearnerDto> Handle(GetEnrollmentLearnerQuery request, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerContextualAccess.EnsureEnrollmentAsync(enrollment, cohorts, offerings, current, manage: false, ct);
        await LearnerSelfAccess.EnsureAsync(enrollment, profiles, people, current, ct);
        var profile = await profiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var person = await people.GetByIdAsync(profile.PersonId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        return LearnerDtoFactory.Create(enrollment, profile, person, mapper);
    }
}

public sealed class GetSelfLearnerQueryHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings,
    ILearnerProfileRepository profiles, IPersonRepository people, IObjectMapper mapper, ICurrentUser current)
    : IRequestHandler<GetSelfLearnerQuery, LearnerDto>
{
    public async Task<LearnerDto> Handle(GetSelfLearnerQuery request, CancellationToken ct)
    {
        var orgId = TenantScope.Organization(current)
            ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var identityId = current.UserId;
        var person = !string.IsNullOrWhiteSpace(current.Email)
            ? await people.GetByEmailAsync(current.Email, false, ct) : null;
        var profile = identityId.HasValue
            ? await profiles.Query(false).FirstOrDefaultAsync(x => x.AuthGateUserId == identityId.Value, ct)
            : null;
        if (profile is null && person is not null)
            profile = await profiles.GetByPersonIdAsync(person.Id, false, ct);
        if (profile is null) throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        // A linked profile must never be selected through an email belonging to another AuthGate user.
        if (profile.AuthGateUserId.HasValue && profile.AuthGateUserId != identityId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        person = await people.GetByIdAsync(profile.PersonId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        var enrollment = await enrollments.Query(false)
            .Where(x => x.LearnerProfileId == profile.Id && x.OrganizationId == orgId)
            .OrderByDescending(x => x.EnrolledOn).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        await LearnerContextualAccess.EnsureEnrollmentAsync(enrollment, cohorts, offerings, current, manage: false, ct);
        return LearnerDtoFactory.Create(enrollment, profile, person, mapper);
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

public sealed class ChangeEnrollmentStatusCommandHandler(IEnrollmentRepository enrollments, ICohortRepository cohorts, IProgramOfferingRepository offerings, ILearnerProfileRepository learnerProfiles, IPersonRepository persons, IObjectMapper mapper, ICurrentUser current) : IRequestHandler<ChangeEnrollmentStatusCommand, LearnerDto>
{
    public async Task<LearnerDto> Handle(ChangeEnrollmentStatusCommand request, CancellationToken ct)
    {
        var enrollment = await enrollments.GetByIdAsync(request.EnrollmentId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
        TenantScope.Ensure(current, enrollment.OrganizationId);
        await LearnerContextualAccess.EnsureEnrollmentAsync(enrollment, cohorts, offerings, current, manage: true, ct);
        if (!Enum.TryParse<EnrollmentStatus>(request.Status, true, out var status))
            throw new ValidationApplicationException(ErrorKeys.EnrollmentStatusInvalid);
        if (status == EnrollmentStatus.Active && enrollment.Status != EnrollmentStatus.Active)
        {
            var cohort = await cohorts.GetByIdAsync(enrollment.CohortId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
            if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
                throw new ConflictApplicationException(ErrorKeys.CohortClosed);
            await cohorts.LockForEnrollmentCapacityAsync(cohort.Id, ct);
            cohort = await cohorts.GetByIdAsync(cohort.Id, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
            if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
                throw new ConflictApplicationException(ErrorKeys.CohortClosed);
            var occupied = await enrollments.CountSeatOccupyingByCohortAsync(cohort.Id, ct);
            // Suspended already reserves a place; no capacity increase when resuming.
            if (enrollment.Status != EnrollmentStatus.Suspended && occupied >= cohort.Capacity)
                throw new ConflictApplicationException(ErrorKeys.CohortCapacityReached);
        }
        try
        {
            enrollment.ChangeStatus(status, request.EndedOn);
        }
        catch (PedagoraPilot.Domain.Common.DomainException ex) when (ex.ErrorKey == ErrorKeys.EnrollmentStatusTransitionForbidden)
        {
            throw new ConflictApplicationException(ex.ErrorKey);
        }
        var learner = await learnerProfiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        var person = await persons.GetByIdAsync(learner.PersonId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
        return LearnerDtoFactory.Create(enrollment, learner, person, mapper);
    }
}
