using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;

public readonly record struct CohortTrainerAssignmentId(Guid Value) : IIdentifier
{
    public static CohortTrainerAssignmentId New() => new(Guid.NewGuid());
    public static CohortTrainerAssignmentId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public static implicit operator Guid(CohortTrainerAssignmentId id) => id.Value;
    public static explicit operator CohortTrainerAssignmentId(Guid value) => new(value);
}
