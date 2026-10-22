using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures;

namespace PedagoraPilot.Application.Mapping;
public sealed class SignatureMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration) =>
        configuration.CreateMap<SignatureEnvelope, SignatureEnvelopeReadModel>();
}

public sealed class SignatureEnvelopeReadModel
{
    public SignatureEnvelopeId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CohortId { get; set; }
    public EnrollmentId EnrollmentId { get; set; }
    public SignatureSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public long SourceVersion { get; set; }
    public SignatureEnvelopeStatus Status { get; set; }
    public string ContentSha256 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTimeOffset? RequestedAtUtc { get; set; }
    public DateTimeOffset? SignedAtUtc { get; set; }
    public long Version { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
}
