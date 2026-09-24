using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class CertificationSchemeRepository(PedagoraPilotDbContext db) : Repository<CertificationScheme, CertificationSchemeId>(db), ICertificationSchemeRepository;
public sealed class CertificationExamSessionRepository(PedagoraPilotDbContext db) : Repository<CertificationExamSession, CertificationExamSessionId>(db), ICertificationExamSessionRepository;
public sealed class CertificationCandidateRepository(PedagoraPilotDbContext db) : Repository<CertificationCandidate, CertificationCandidateId>(db), ICertificationCandidateRepository
{
    public Task<bool> ExistsAsync(CertificationExamSessionId sessionId, EnrollmentId enrollmentId, CancellationToken ct = default) => Query(false).AnyAsync(x => x.ExamSessionId == sessionId && x.EnrollmentId == enrollmentId, ct);
}

public sealed class JuryAssignmentRepository(PedagoraPilotDbContext db) : Repository<JuryAssignment, JuryAssignmentId>(db), IJuryAssignmentRepository;
