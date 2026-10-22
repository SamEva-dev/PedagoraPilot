namespace PedagoraPilot.Application.Signatures;
public interface ISignatureDeliveryProcessor
{
    Task DeliverRequestAsync(Guid envelopeId, Guid eventId, CancellationToken ct);
    Task DeliverReminderAsync(Guid envelopeId, Guid eventId, CancellationToken ct);
}
