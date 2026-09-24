using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct LearnerCompetencyRecordId(Guid Value) : IIdentifier
{
    public static LearnerCompetencyRecordId New() => new(Guid.NewGuid());
    public static LearnerCompetencyRecordId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(LearnerCompetencyRecordId id) => id.Value;
    public static explicit operator LearnerCompetencyRecordId(Guid value) => new(value);
}
