using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;

public readonly record struct SignatureEnvelopeId(Guid Value) : IIdentifier
{
    public static SignatureEnvelopeId New() => new(Guid.NewGuid());
    public static SignatureEnvelopeId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}
