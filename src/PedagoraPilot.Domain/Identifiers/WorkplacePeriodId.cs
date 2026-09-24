using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct WorkplacePeriodId(Guid Value) : IIdentifier
{
    public static WorkplacePeriodId New() => new(Guid.NewGuid());
    public static WorkplacePeriodId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
