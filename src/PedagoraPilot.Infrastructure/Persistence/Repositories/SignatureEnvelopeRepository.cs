using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;

public sealed class SignatureEnvelopeRepository(PedagoraPilotDbContext db)
    : Repository<SignatureEnvelope, SignatureEnvelopeId>(db), ISignatureEnvelopeRepository
{
    public Task<SignatureEnvelope?> FindBySourceAsync(Guid organizationId, SignatureSourceType sourceType,
        Guid sourceId, long sourceVersion, EnrollmentId enrollmentId, CancellationToken ct = default)
        => Query(false).SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SourceType == sourceType
            && x.SourceId == sourceId && x.SourceVersion == sourceVersion && x.EnrollmentId == enrollmentId, ct);
}
