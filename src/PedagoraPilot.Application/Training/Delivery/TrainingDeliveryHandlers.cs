using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Training.Delivery;
internal static class TrainingDeliveryMapper
{
    public static TrainingSessionDto ToDto(TrainingSession entity, int expectedLearners, int presentLearners, IObjectMapper mapper)
    {
        var model = mapper.Map<TrainingSession, TrainingSessionReadModel>(entity);
        return new TrainingSessionDto(model.Id.Value, model.OrganizationId, model.SiteId, model.CohortId.Value, ToWire(model.Type), ToWire(model.Modality), model.Title, model.StartsAtUtc, model.EndsAtUtc, model.TimeZoneId, model.TrainerAuthGateUserId, model.TrainerDisplayName, model.Location, model.Objective, model.Supports, model.Comments, model.Status.ToString().ToLowerInvariant(), ToWire(model.AudienceMode), entity.Participants.Select(x => x.EnrollmentId.Value).ToArray(), model.PlannedMinutes, expectedLearners, presentLearners, model.ExternalKey);
    }

    public static string ToWire(TrainingSessionType value) => value switch
    {
        TrainingSessionType.Classroom => "classroom",
        TrainingSessionType.Distance => "distance",
        TrainingSessionType.Driving => "driving",
        TrainingSessionType.Evaluation => "evaluation",
        TrainingSessionType.Internship => "internship",
        TrainingSessionType.Presentation => "presentation",
        TrainingSessionType.Catchup => "catchup",
        TrainingSessionType.Sensitization => "sensitization",
        _ => "event"
    };
    public static string ToWire(TrainingSessionModality value) => value switch
    {
        TrainingSessionModality.RemoteLive => "remote-live",
        TrainingSessionModality.RemoteAsync => "remote-async",
        TrainingSessionModality.Practical => "practical",
        _ => "onsite"
    };
    public static TrainingSessionType ParseType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "classroom" => TrainingSessionType.Classroom,
        "distance" => TrainingSessionType.Distance,
        "driving" => TrainingSessionType.Driving,
        "evaluation" => TrainingSessionType.Evaluation,
        "internship" => TrainingSessionType.Internship,
        "presentation" => TrainingSessionType.Presentation,
        "catchup" => TrainingSessionType.Catchup,
        "sensitization" => TrainingSessionType.Sensitization,
        "event" => TrainingSessionType.Event,
        _ => throw new ValidationApplicationException(ErrorKeys.SessionTypeInvalid)};
    public static TrainingSessionModality ParseModality(string value) => value.Trim().ToLowerInvariant() switch
    {
        "onsite" => TrainingSessionModality.Onsite,
        "remote-live" => TrainingSessionModality.RemoteLive,
        "remote-async" => TrainingSessionModality.RemoteAsync,
        "practical" => TrainingSessionModality.Practical,
        _ => throw new ValidationApplicationException(ErrorKeys.SessionModalityInvalid)};
    public static string ToWire(SessionAudienceMode value) => value switch
    {
        SessionAudienceMode.SelectedEnrollments => "selected-enrollments",
        _ => "whole-cohort"
    };
    public static SessionAudienceMode ParseAudienceMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        "whole-cohort" => SessionAudienceMode.WholeCohort,
        "selected-enrollments" => SessionAudienceMode.SelectedEnrollments,
        _ => throw new ValidationApplicationException(ErrorKeys.SessionAudienceModeInvalid)};
    public static AttendanceStatus ParseAttendanceStatus(string value) => value.Trim().ToLowerInvariant() switch
    {
        "present" => AttendanceStatus.Present,
        "late" => AttendanceStatus.Late,
        "absent" => AttendanceStatus.Absent,
        "excused" => AttendanceStatus.Excused,
        _ => throw new ValidationApplicationException(ErrorKeys.AttendanceStatusInvalid)};
}

public sealed class CreateTrainingSessionCommandHandler(ICohortRepository cohorts, IEnrollmentRepository enrollments, ITrainingSessionRepository sessions, ICurrentUser currentUser, IObjectMapper mapper) : IRequestHandler<CreateTrainingSessionCommand, TrainingSessionDto>
{
    public async Task<TrainingSessionDto> Handle(CreateTrainingSessionCommand request, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(request.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        EnsureOrganizationScope(cohort.OrganizationId, currentUser);
        if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.CohortClosed);
        var audienceMode = TrainingDeliveryMapper.ParseAudienceMode(request.AudienceMode);
        var participantIds = (request.ParticipantEnrollmentIds ?? Array.Empty<Guid>()).Select(x => new EnrollmentId(x)).ToArray();
        await ValidateParticipantsAsync(cohort.Id, audienceMode, participantIds, enrollments, ct);
        var trainer = VerifiedTrainer(currentUser, request.TrainerAuthGateUserId, request.TrainerDisplayName);
        var entity = TrainingSession.Create(cohort.OrganizationId, cohort.SiteId, cohort.Id, TrainingDeliveryMapper.ParseType(request.Type), TrainingDeliveryMapper.ParseModality(request.Modality), request.Title, request.StartsAtUtc, request.EndsAtUtc, request.TimeZoneId, trainer.Id, trainer.Name, request.Location, request.Objective, request.Supports, request.Comments, audienceMode, participantIds, request.ExternalKey);
        await sessions.AddAsync(entity, ct);
        var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : await enrollments.CountActiveByCohortAsync(cohort.Id, ct);
        return TrainingDeliveryMapper.ToDto(entity, expected, 0, mapper);
    }

    internal static async Task ValidateParticipantsAsync(CohortId cohortId, SessionAudienceMode audienceMode, IReadOnlyCollection<EnrollmentId> participantIds, IEnrollmentRepository enrollments, CancellationToken ct)
    {
        if (audienceMode == SessionAudienceMode.WholeCohort)
            return;
        if (participantIds.Count == 0)
            throw new ValidationApplicationException(ErrorKeys.SessionParticipantsRequired);
        var validCount = await enrollments.Query(false).CountAsync(x => participantIds.Contains(x.Id) && x.CohortId == cohortId && x.Status == EnrollmentStatus.Active, ct);
        if (validCount != participantIds.Distinct().Count())
            throw new ConflictApplicationException(ErrorKeys.SessionParticipantCohortMismatch);
    }

    internal static void EnsureOrganizationScope(Guid organizationId, ICurrentUser currentUser)
    {
        TenantScope.Ensure(currentUser, organizationId);
    }
    internal static (string? Id, string? Name) VerifiedTrainer(ICurrentUser current, string? requestedId, string? requestedName)
    {
        var ownId = current.UserId?.ToString("D");
        if (!string.IsNullOrWhiteSpace(requestedId) && !string.Equals(requestedId, ownId, StringComparison.OrdinalIgnoreCase))
            throw new ValidationApplicationException(ErrorKeys.SessionTrainerIdentityInvalid);
        if (current.Roles.Contains("PedagoraPilot.Trainer") || current.Roles.Contains("PedagoraPilot.PedagogicalManager")
            || !string.IsNullOrWhiteSpace(requestedId))
            return (ownId, current.DisplayName ?? current.Email ?? ownId);
        return (null, requestedName);
    }
    internal static void EnsureTrainerCanManage(TrainingSession session, ICurrentUser current)
    {
        if (current.Roles.Contains("PedagoraPilot.Trainer") && !string.IsNullOrWhiteSpace(session.TrainerAuthGateUserId)
            && !string.Equals(session.TrainerAuthGateUserId, current.UserId?.ToString("D"), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }
}

public sealed class UpdateTrainingSessionCommandHandler(ITrainingSessionRepository sessions, ICurrentUser currentUser, IObjectMapper mapper, IEnrollmentRepository enrollments) : IRequestHandler<UpdateTrainingSessionCommand, TrainingSessionDto>
{
    public async Task<TrainingSessionDto> Handle(UpdateTrainingSessionCommand request, CancellationToken ct)
    {
        var entity = await sessions.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(entity.OrganizationId, currentUser);
        CreateTrainingSessionCommandHandler.EnsureTrainerCanManage(entity, currentUser);
        var audienceMode = TrainingDeliveryMapper.ParseAudienceMode(request.AudienceMode);
        var participantIds = (request.ParticipantEnrollmentIds ?? Array.Empty<Guid>()).Select(x => new EnrollmentId(x)).ToArray();
        await CreateTrainingSessionCommandHandler.ValidateParticipantsAsync(entity.CohortId, audienceMode, participantIds, enrollments, ct);
        var trainer = CreateTrainingSessionCommandHandler.VerifiedTrainer(currentUser, request.TrainerAuthGateUserId, request.TrainerDisplayName);
        entity.Update(TrainingDeliveryMapper.ParseType(request.Type), TrainingDeliveryMapper.ParseModality(request.Modality), request.Title, request.StartsAtUtc, request.EndsAtUtc, request.TimeZoneId, trainer.Id, trainer.Name, request.Location, request.Objective, request.Supports, request.Comments, audienceMode, participantIds);
        var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : await enrollments.CountActiveByCohortAsync(entity.CohortId, ct);
        return TrainingDeliveryMapper.ToDto(entity, expected, 0, mapper);
    }
}

public sealed class CancelTrainingSessionCommandHandler(ITrainingSessionRepository sessions, ICurrentUser currentUser, IObjectMapper mapper, IEnrollmentRepository enrollments) : IRequestHandler<CancelTrainingSessionCommand, TrainingSessionDto>
{
    public async Task<TrainingSessionDto> Handle(CancelTrainingSessionCommand request, CancellationToken ct)
    {
        var entity = await sessions.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(entity.OrganizationId, currentUser);
        CreateTrainingSessionCommandHandler.EnsureTrainerCanManage(entity, currentUser);
        entity.Cancel();
        var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : await enrollments.CountActiveByCohortAsync(entity.CohortId, ct);
        return TrainingDeliveryMapper.ToDto(entity, expected, 0, mapper);
    }
}

public sealed class CompleteTrainingSessionCommandHandler(ITrainingSessionRepository sessions, ICurrentUser currentUser, IObjectMapper mapper, IEnrollmentRepository enrollments) : IRequestHandler<CompleteTrainingSessionCommand, TrainingSessionDto>
{
    public async Task<TrainingSessionDto> Handle(CompleteTrainingSessionCommand request, CancellationToken ct)
    {
        var entity = await sessions.GetByIdAsync(request.Id, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(entity.OrganizationId, currentUser);
        CreateTrainingSessionCommandHandler.EnsureTrainerCanManage(entity, currentUser);
        entity.Complete();
        var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : await enrollments.CountActiveByCohortAsync(entity.CohortId, ct);
        return TrainingDeliveryMapper.ToDto(entity, expected, 0, mapper);
    }
}

public sealed class GetTrainingSessionsQueryHandler(ITrainingSessionRepository sessions, IAttendanceSheetRepository attendance, IEnrollmentRepository enrollments, ILearnerProfileRepository profiles, IPersonRepository people, ICurrentUser currentUser, IObjectMapper mapper) : IRequestHandler<GetTrainingSessionsQuery, IReadOnlyCollection<TrainingSessionDto>>
{
    public async Task<IReadOnlyCollection<TrainingSessionDto>> Handle(GetTrainingSessionsQuery request, CancellationToken ct)
    {
        var query = sessions.Query(false).Include(x => x.Participants).AsQueryable();
        var organizationId = TenantScope.Organization(currentUser);
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        Enrollment[] own = Array.Empty<Enrollment>();
        if (LearnerSelfAccess.Applies(currentUser))
        {
            own = await StudentSessionAccess.EnrollmentsAsync(currentUser, profiles, people, enrollments, ct);
            if (own.Length == 0) return Array.Empty<TrainingSessionDto>();
            var ownCohortIds = own.Select(x => x.CohortId).Distinct().ToArray();
            query = query.Where(x => ownCohortIds.Contains(x.CohortId));
        }
        if (request.CohortId.HasValue)
            query = query.Where(x => x.CohortId == request.CohortId.Value);
        if (request.FromUtc.HasValue)
            query = query.Where(x => x.EndsAtUtc >= request.FromUtc.Value.ToUniversalTime());
        if (request.ToUtc.HasValue)
            query = query.Where(x => x.StartsAtUtc <= request.ToUtc.Value.ToUniversalTime());
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = TrainingDeliveryMapper.ParseType(request.Type);
            query = query.Where(x => x.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<TrainingSessionStatus>(request.Status, true, out var status))
                throw new ValidationApplicationException(ErrorKeys.SessionStatusInvalid);
            query = query.Where(x => x.Status == status);
        }

        var entities = await query.OrderBy(x => x.StartsAtUtc).ToListAsync(ct);
        if (LearnerSelfAccess.Applies(currentUser))
            entities = entities.Where(x => own.Any(e => StudentSessionAccess.IsParticipant(x, e))).ToList();
        if (entities.Count == 0)
            return Array.Empty<TrainingSessionDto>();
        var cohortIds = entities.Select(x => x.CohortId).Distinct().ToArray();
        var enrollmentCounts = LearnerSelfAccess.Applies(currentUser)
            ? new Dictionary<CohortId, int>()
            : await enrollments.Query(false).Where(x => cohortIds.Contains(x.CohortId) && x.Status == EnrollmentStatus.Active).GroupBy(x => x.CohortId).Select(x => new { CohortId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.CohortId, x => x.Count, ct);
        var sessionIds = entities.Select(x => x.Id).ToArray();
        var sheets = await attendance.GetBySessionIdsAsync(sessionIds, false, ct);
        var sheetsBySession = sheets.ToDictionary(x => x.SessionId);
        return entities.Select(entity =>
        {
            if (LearnerSelfAccess.Applies(currentUser))
            {
                var self = StudentSessionAccess.EnsureParticipant(entity, own);
                var ownPresent = sheetsBySession.TryGetValue(entity.Id, out var ownSheet)
                    && ownSheet.Entries.Any(x => x.EnrollmentId == self && x.Status is AttendanceStatus.Present or AttendanceStatus.Late);
                return StudentSessionAccess.Redact(TrainingDeliveryMapper.ToDto(entity, 1, ownPresent ? 1 : 0, mapper), self);
            }
            var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : enrollmentCounts.GetValueOrDefault(entity.CohortId, 0);
            var present = sheetsBySession.TryGetValue(entity.Id, out var sheet) ? sheet.Entries.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late) : 0;
            return TrainingDeliveryMapper.ToDto(entity, expected, present, mapper);
        }).ToArray();
    }
}

public sealed class GetTrainingSessionQueryHandler(ITrainingSessionRepository sessions, IAttendanceSheetRepository attendance, IEnrollmentRepository enrollments, ILearnerProfileRepository profiles, IPersonRepository people, ICurrentUser currentUser, IObjectMapper mapper) : IRequestHandler<GetTrainingSessionQuery, TrainingSessionDto>
{
    public async Task<TrainingSessionDto> Handle(GetTrainingSessionQuery request, CancellationToken ct)
    {
        var entity = await sessions.GetByIdAsync(request.Id, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(entity.OrganizationId, currentUser);
        EnrollmentId? self = null;
        if (LearnerSelfAccess.Applies(currentUser))
            self = StudentSessionAccess.EnsureParticipant(entity,
                await StudentSessionAccess.EnrollmentsAsync(currentUser, profiles, people, enrollments, ct));
        var sheet = await attendance.GetBySessionIdAsync(entity.Id, false, ct);
        if (self.HasValue)
        {
            var selfPresent = sheet?.Entries.Any(x => x.EnrollmentId == self.Value && x.Status is AttendanceStatus.Present or AttendanceStatus.Late) == true;
            return StudentSessionAccess.Redact(TrainingDeliveryMapper.ToDto(entity, 1, selfPresent ? 1 : 0, mapper), self.Value);
        }
        var expected = entity.AudienceMode == SessionAudienceMode.SelectedEnrollments ? entity.Participants.Count : await enrollments.CountActiveByCohortAsync(entity.CohortId, ct);
        var present = sheet?.Entries.Count(x => x.Status is AttendanceStatus.Present or AttendanceStatus.Late) ?? 0;
        var dto = TrainingDeliveryMapper.ToDto(entity, expected, present, mapper);
        return dto;
    }
}

public sealed class GetAttendanceSheetQueryHandler(ITrainingSessionRepository sessions, IAttendanceSheetRepository sheets, IEnrollmentRepository enrollments, ILearnerProfileRepository learners, IPersonRepository people, ICurrentUser currentUser, IObjectMapper mapper) : IRequestHandler<GetAttendanceSheetQuery, AttendanceSheetDto>
{
    public async Task<AttendanceSheetDto> Handle(GetAttendanceSheetQuery request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(session.OrganizationId, currentUser);
        EnrollmentId? self = null;
        if (LearnerSelfAccess.Applies(currentUser))
            self = StudentSessionAccess.EnsureParticipant(session,
                await StudentSessionAccess.EnrollmentsAsync(currentUser, learners, people, enrollments, ct));
        var sheet = await sheets.GetBySessionIdAsync(session.Id, false, ct);
        if (sheet is null)
        {
            // GET remains side-effect free. The sheet is persisted on the first PUT.
            var enrollmentIds = await ResolveEligibleEnrollmentIdsAsync(session, enrollments, ct);
            sheet = AttendanceSheet.Preview(session.OrganizationId, session.Id, session.CohortId, session.PlannedMinutes, enrollmentIds);
        }

        return await BuildAttendanceDto(sheet, enrollments, learners, people, mapper, ct, self);
    }

    internal static async Task<EnrollmentId[]> ResolveEligibleEnrollmentIdsAsync(TrainingSession session, IEnrollmentRepository enrollments, CancellationToken ct)
    {
        if (session.AudienceMode == SessionAudienceMode.SelectedEnrollments)
            return session.Participants.Select(x => x.EnrollmentId).Distinct().ToArray();
        return await enrollments.Query(false).Where(x => x.CohortId == session.CohortId && x.Status == EnrollmentStatus.Active).Select(x => x.Id).ToArrayAsync(ct);
    }

    internal static async Task<AttendanceSheetDto> BuildAttendanceDto(AttendanceSheet sheet, IEnrollmentRepository enrollments, ILearnerProfileRepository learners, IPersonRepository people, IObjectMapper mapper, CancellationToken ct, EnrollmentId? onlyEnrollmentId = null)
    {
        var selectedEntries = onlyEnrollmentId.HasValue
            ? sheet.Entries.Where(x => x.EnrollmentId == onlyEnrollmentId.Value).ToArray()
            : sheet.Entries.ToArray();
        var entryIds = selectedEntries.Select(x => x.EnrollmentId).Distinct().ToArray();
        var enrollmentRows = await enrollments.Query(false).Where(x => entryIds.Contains(x.Id)).ToListAsync(ct);
        var enrollmentById = enrollmentRows.ToDictionary(x => x.Id);
        var learnerIds = enrollmentRows.Select(x => x.LearnerProfileId).Distinct().ToArray();
        var learnerRows = await learners.Query(false).Where(x => learnerIds.Contains(x.Id)).ToListAsync(ct);
        var learnerById = learnerRows.ToDictionary(x => x.Id);
        var personIds = learnerRows.Select(x => x.PersonId).Distinct().ToArray();
        var personRows = await people.Query(false).Where(x => personIds.Contains(x.Id)).ToListAsync(ct);
        var personById = personRows.ToDictionary(x => x.Id);
        var entries = new List<AttendanceEntryDto>(selectedEntries.Length);
        foreach (var entry in selectedEntries.OrderBy(x => x.EnrollmentId.Value))
        {
            if (!enrollmentById.TryGetValue(entry.EnrollmentId, out var enrollment))
                throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
            if (!learnerById.TryGetValue(enrollment.LearnerProfileId, out var learner))
                throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
            if (!personById.TryGetValue(learner.PersonId, out var person))
                throw new NotFoundApplicationException(ErrorKeys.PersonNotFound);
            var personModel = mapper.Map<Person, PersonReadModel>(person);
            var entryModel = mapper.Map<AttendanceEntry, AttendanceEntryReadModel>(entry);
            entries.Add(new AttendanceEntryDto(entryModel.EnrollmentId.Value, learner.Id.Value, person.Id.Value, personModel.FirstName, personModel.LastName, $"{personModel.FirstName} {personModel.LastName}".Trim(), entryModel.Status.ToString().ToLowerInvariant(), entryModel.ArrivalAtUtc, entryModel.DepartureAtUtc, entryModel.ExpectedMinutes, entryModel.PresentMinutes, entryModel.MissedMinutes, entryModel.CatchupMinutes, entryModel.AddToCatchup, entryModel.Comment));
        }

        return new AttendanceSheetDto(sheet.Id.Value, sheet.SessionId.Value, sheet.CohortId.Value, sheet.ExpectedMinutes, entries.Count(x => x.Status == "present"), entries.Count(x => x.Status == "late"), entries.Count(x => x.Status == "absent"), entries.Count(x => x.Status == "excused"), entries.Sum(x => x.MissedMinutes), entries);
    }
}

public sealed class SaveAttendanceCommandHandler(ITrainingSessionRepository sessions, IAttendanceSheetRepository sheets, IEnrollmentRepository enrollments, ILearnerProfileRepository learners, IPersonRepository people, ICurrentUser currentUser, IObjectMapper mapper) : IRequestHandler<SaveAttendanceCommand, AttendanceSheetDto>
{
    public async Task<AttendanceSheetDto> Handle(SaveAttendanceCommand request, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(request.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.SessionNotFound);
        CreateTrainingSessionCommandHandler.EnsureOrganizationScope(session.OrganizationId, currentUser);
        if (session.Status == TrainingSessionStatus.Cancelled)
            throw new ConflictApplicationException(ErrorKeys.SessionCancelled);
        var sheet = await sheets.GetBySessionIdAsync(session.Id, true, ct);
        var active = await GetAttendanceSheetQueryHandler.ResolveEligibleEnrollmentIdsAsync(session, enrollments, ct);
        if (sheet is null)
        {
            sheet = AttendanceSheet.Create(session.OrganizationId, session.Id, session.CohortId, session.PlannedMinutes, active);
            await sheets.AddAsync(sheet, ct);
        }
        else
        {
            sheet.SynchronizeEnrollments(active);
        }

        foreach (var update in request.Entries)
        {
            var enrollmentId = new EnrollmentId(update.EnrollmentId);
            var enrollment = await enrollments.GetByIdAsync(enrollmentId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.EnrollmentNotFound);
            if (enrollment.CohortId != session.CohortId)
                throw new ConflictApplicationException(ErrorKeys.AttendanceEnrollmentCohortMismatch);
            sheet.Record(enrollmentId, TrainingDeliveryMapper.ParseAttendanceStatus(update.Status), update.ArrivalAtUtc, update.DepartureAtUtc, update.PresentMinutes, update.AddToCatchup, update.Comment);
        }

        sheet.MarkRecorded();
        return await GetAttendanceSheetQueryHandler.BuildAttendanceDto(sheet, enrollments, learners, people, mapper, ct);
    }
}
