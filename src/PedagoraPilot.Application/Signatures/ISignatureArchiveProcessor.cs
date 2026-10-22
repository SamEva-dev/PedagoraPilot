namespace PedagoraPilot.Application.Signatures;

/// <summary>Post-commit, idempotent evidence storage. A failure must fail the outbox delivery so it can be retried.</summary>
public interface ISignatureArchiveProcessor
{
    Task ArchiveAsync(Guid envelopeId, CancellationToken cancellationToken);
}
