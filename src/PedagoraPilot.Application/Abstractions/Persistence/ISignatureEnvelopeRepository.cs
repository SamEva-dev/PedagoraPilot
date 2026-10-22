using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ISignatureEnvelopeRepository : IRepository<SignatureEnvelope, SignatureEnvelopeId>
{
    Task<SignatureEnvelope?> FindBySourceAsync(Guid organizationId, SignatureSourceType sourceType,
        Guid sourceId, long sourceVersion, EnrollmentId enrollmentId, CancellationToken ct = default);
}
