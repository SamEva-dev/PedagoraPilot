using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct AttendanceSheetId(Guid Value) : IIdentifier
{
    public static AttendanceSheetId New() => new(Guid.NewGuid());
    public static AttendanceSheetId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
