using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct JuryAssignmentId(Guid Value) : IIdentifier
{
    public static JuryAssignmentId New() => new(Guid.NewGuid());
    public static JuryAssignmentId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
