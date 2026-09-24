using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct AsyncModuleStepId(Guid Value) : IIdentifier
{
    public static AsyncModuleStepId New() => new(Guid.NewGuid());
    public static AsyncModuleStepId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
