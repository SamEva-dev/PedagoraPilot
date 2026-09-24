using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct AsyncLearningModuleId(Guid Value) : IIdentifier
{
    public static AsyncLearningModuleId New() => new(Guid.NewGuid());
    public static AsyncLearningModuleId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
