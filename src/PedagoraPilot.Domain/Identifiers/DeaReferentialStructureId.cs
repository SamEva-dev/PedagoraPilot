using PedagoraPilot.Domain.Common;
namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DeaReferentialStructureId(Guid Value) : IIdentifier
{
    public static DeaReferentialStructureId New() => new(Guid.NewGuid());
    public static DeaReferentialStructureId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}
