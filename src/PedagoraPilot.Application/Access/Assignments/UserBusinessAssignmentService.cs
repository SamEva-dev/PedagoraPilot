using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Access.Assignments;

public sealed record UserBusinessAssignmentDto(
    Guid Id,
    Guid UserId,
    string Role,
    string Scope,
    Guid OrganizationId,
    Guid SiteId,
    Guid ProgramId,
    Guid CohortId,
    bool Active,
    bool IsPedagogicalManager,
    string? WarningKey = null);

public interface IUserBusinessAssignmentService
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<UserBusinessAssignmentDto>>> ListForOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<UserBusinessAssignmentDto> AddAsync(Guid organizationId, Guid authGateUserId, Guid cohortId, string role, bool makePedagogicalManager, CancellationToken ct = default);
    Task RemoveAsync(Guid organizationId, Guid authGateUserId, Guid assignmentId, CancellationToken ct = default);
}

public sealed class UserBusinessAssignmentService(
    IOrganizationMemberRepository members,
    IStudentMemberProvisioner studentProvisioner,
    ILearnerProfileRepository learnerProfiles,
    IEnrollmentRepository enrollments,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    ICohortTrainerAssignmentRepository trainerAssignments,
    IUnitOfWork unitOfWork) : IUserBusinessAssignmentService
{
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<UserBusinessAssignmentDto>>> ListForOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        var cohortRows = await cohorts.Query(false)
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync(ct);
        if (cohortRows.Count == 0)
            return new Dictionary<Guid, IReadOnlyCollection<UserBusinessAssignmentDto>>();

        var activeStudentIds = (await members.GetStudentDirectoryAsync(organizationId, ct))
            .Select(x => x.AuthGateUserId).ToHashSet();
        var activeTrainerIds = (await members.GetTrainerDirectoryAsync(organizationId, ct))
            .Select(x => x.AuthGateUserId).ToHashSet();

        var cohortIds = cohortRows.Select(x => x.Id).ToArray();
        var offeringIds = cohortRows.Select(x => x.ProgramOfferingId).Distinct().ToArray();
        var offeringRows = await offerings.Query(false)
            .Where(x => offeringIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var cohortById = cohortRows.ToDictionary(x => x.Id);

        var result = new Dictionary<Guid, List<UserBusinessAssignmentDto>>();

        var learnerRows = await learnerProfiles.Query(false)
            .Where(x => x.AuthGateUserId.HasValue)
            .ToListAsync(ct);
        var learnerById = learnerRows.ToDictionary(x => x.Id);
        var enrollmentRows = await enrollments.Query(false)
            .Where(x => x.OrganizationId == organizationId
                && cohortIds.Contains(x.CohortId)
                && (x.Status == EnrollmentStatus.Pending || x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Suspended))
            .ToListAsync(ct);

        foreach (var enrollment in enrollmentRows)
        {
            if (!learnerById.TryGetValue(enrollment.LearnerProfileId, out var learner) || !learner.AuthGateUserId.HasValue
                || !activeStudentIds.Contains(learner.AuthGateUserId.Value))
                continue;
            if (!cohortById.TryGetValue(enrollment.CohortId, out var cohort) || !offeringRows.TryGetValue(cohort.ProgramOfferingId, out var offering))
                continue;

            Add(result, learner.AuthGateUserId.Value, new UserBusinessAssignmentDto(
                enrollment.Id.Value,
                learner.AuthGateUserId.Value,
                "student",
                "cohort",
                organizationId,
                cohort.SiteId,
                offering.ProgramId,
                cohort.Id.Value,
                true,
                false));
        }

        var trainerRows = await trainerAssignments.Query(false)
            .Where(x => x.OrganizationId == organizationId && x.IsActive && cohortIds.Contains(x.CohortId))
            .ToListAsync(ct);
        foreach (var assignment in trainerRows)
        {
            if (!activeTrainerIds.Contains(assignment.AuthGateUserId))
                continue;
            if (!cohortById.TryGetValue(assignment.CohortId, out var cohort) || !offeringRows.TryGetValue(cohort.ProgramOfferingId, out var offering))
                continue;
            var isManager = cohort.PedagogicalManagerAuthGateUserId == assignment.AuthGateUserId;
            Add(result, assignment.AuthGateUserId, new UserBusinessAssignmentDto(
                assignment.Id.Value,
                assignment.AuthGateUserId,
                "trainer",
                "cohort",
                organizationId,
                cohort.SiteId,
                offering.ProgramId,
                cohort.Id.Value,
                true,
                isManager));
        }

        return result.ToDictionary(x => x.Key, x => (IReadOnlyCollection<UserBusinessAssignmentDto>)x.Value.OrderBy(y => y.CohortId).ToArray());
    }

    public async Task<UserBusinessAssignmentDto> AddAsync(Guid organizationId, Guid authGateUserId, Guid cohortId, string role, bool makePedagogicalManager, CancellationToken ct = default)
    {
        var member = await members.GetByAuthGateUserAsync(organizationId, authGateUserId, true, ct)
            ?? throw new NotFoundApplicationException("MEMBER_NOT_FOUND");
        if (!member.IsActive)
            throw new ConflictApplicationException("MEMBER_INACTIVE");

        var cohort = await cohorts.GetByIdAsync(new CohortId(cohortId), true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (cohort.OrganizationId != organizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);

        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        var actualRoles = await members.GetRoleCodesAsync(organizationId, authGateUserId, ct);
        var normalizedRole = NormalizeRole(role);

        if (normalizedRole == "student")
        {
            if (!actualRoles.Any(x => x.EndsWith(".Student", StringComparison.OrdinalIgnoreCase)))
                throw new ConflictApplicationException("ASSIGNMENT_ROLE_MISMATCH");

            return await unitOfWork.ExecuteInTransactionAsync(async txCt =>
            {
                var learnerId = await studentProvisioner.EnsureAsync(
                    organizationId, authGateUserId, member.Email, member.FirstName, member.LastName,
                    cohort.Id.Value, enrolledOn: cohort.StartDate, ct: txCt);
                await unitOfWork.SaveChangesAsync(txCt);
                var enrollment = await enrollments.GetByLearnerAndCohortAsync(learnerId, cohort.Id, false, txCt)
                    ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
                return new UserBusinessAssignmentDto(
                    enrollment.Id.Value, authGateUserId, "student", "cohort", organizationId,
                    cohort.SiteId, offering.ProgramId, cohort.Id.Value, true, false);
            }, ct);
        }

        if (normalizedRole != "trainer" && normalizedRole != "pedagogical_manager")
            throw new ConflictApplicationException("ASSIGNMENT_ROLE_NOT_SUPPORTED");
        if (!actualRoles.Any(x => x.EndsWith(".Trainer", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".PedagogicalManager", StringComparison.OrdinalIgnoreCase)))
            throw new ConflictApplicationException("ASSIGNMENT_ROLE_MISMATCH");

        var assignment = await trainerAssignments.GetByUserAndCohortAsync(organizationId, authGateUserId, cohort.Id, true, ct);
        if (assignment is null)
        {
            assignment = CohortTrainerAssignment.Create(organizationId, cohort.Id, member.Id, authGateUserId);
            await trainerAssignments.AddAsync(assignment, ct);
        }
        else
        {
            assignment.Activate();
        }

        string? warningKey = null;
        if (makePedagogicalManager)
        {
            var accepted = cohort.TryAssignPedagogicalManager(authGateUserId, member.DisplayName);
            if (!accepted)
                warningKey = "access.assignments.responsibleAlreadyExists";
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new UserBusinessAssignmentDto(
            assignment.Id.Value, authGateUserId, "trainer", "cohort", organizationId,
            cohort.SiteId, offering.ProgramId, cohort.Id.Value, true,
            cohort.PedagogicalManagerAuthGateUserId == authGateUserId,
            warningKey);
    }

    public async Task RemoveAsync(Guid organizationId, Guid authGateUserId, Guid assignmentId, CancellationToken ct = default)
    {
        var trainerAssignment = await trainerAssignments.GetByIdAsync(new CohortTrainerAssignmentId(assignmentId), true, ct);
        if (trainerAssignment is not null)
        {
            if (trainerAssignment.OrganizationId != organizationId || trainerAssignment.AuthGateUserId != authGateUserId)
                throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            var cohort = await cohorts.GetByIdAsync(trainerAssignment.CohortId, false, ct)
                ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
            if (cohort.PedagogicalManagerAuthGateUserId == authGateUserId)
                throw new ConflictApplicationException("TRAINER_ASSIGNMENT_IS_PEDAGOGICAL_MANAGER");
            trainerAssignment.Deactivate();
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var enrollment = await enrollments.GetByIdAsync(new EnrollmentId(assignmentId), true, ct)
            ?? throw new NotFoundApplicationException("ASSIGNMENT_NOT_FOUND");
        if (enrollment.OrganizationId != organizationId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        var learner = await learnerProfiles.GetByIdAsync(enrollment.LearnerProfileId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        if (learner.AuthGateUserId != authGateUserId)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        if (enrollment.Status == EnrollmentStatus.Completed)
            throw new ConflictApplicationException("COMPLETED_ENROLLMENT_ASSIGNMENT_LOCKED");

        enrollment.ChangeStatus(EnrollmentStatus.Withdrawn, DateOnly.FromDateTime(DateTime.UtcNow));
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static void Add(Dictionary<Guid, List<UserBusinessAssignmentDto>> result, Guid userId, UserBusinessAssignmentDto value)
    {
        if (!result.TryGetValue(userId, out var items)) result[userId] = items = [];
        items.Add(value);
    }

    private static string NormalizeRole(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "stagiaire" => "student",
            "formateur" => "trainer",
            "pedagorapilot.student" => "student",
            "pedagorapilot.trainer" => "trainer",
            "pedagorapilot.pedagogicalmanager" => "pedagogical_manager",
            _ => normalized
        };
    }
}
