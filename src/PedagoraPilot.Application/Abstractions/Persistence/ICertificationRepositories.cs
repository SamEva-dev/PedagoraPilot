using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ICertificationSchemeRepository : IRepository<CertificationScheme, CertificationSchemeId>
{
}

public interface ICertificationExamSessionRepository : IRepository<CertificationExamSession, CertificationExamSessionId>
{
}

public interface ICertificationCandidateRepository : IRepository<CertificationCandidate, CertificationCandidateId>
{
    Task<bool> ExistsAsync(CertificationExamSessionId sessionId, EnrollmentId enrollmentId, CancellationToken ct = default);
}

public interface IJuryAssignmentRepository : IRepository<JuryAssignment, JuryAssignmentId>
{
}
