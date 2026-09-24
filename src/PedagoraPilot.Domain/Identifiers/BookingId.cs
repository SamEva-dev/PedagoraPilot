using PedagoraPilot.Domain.Common;

namespace DriveOS.SharedKernel.Identifiers;
public readonly record struct BookingId(Guid Value) : IIdentifier
{
    public static BookingId New() => new(Guid.NewGuid());
    public static BookingId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
