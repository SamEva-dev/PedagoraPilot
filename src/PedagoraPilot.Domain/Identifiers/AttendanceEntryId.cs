using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct AttendanceEntryId(Guid Value) : IIdentifier
{
    public static AttendanceEntryId New() => new(Guid.NewGuid());
    public static AttendanceEntryId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
