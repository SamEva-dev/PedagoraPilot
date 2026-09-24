namespace PedagoraPilot.Domain.Common;
/// <summary>
/// Marker contract for strongly typed aggregate identifiers.
/// Every future aggregate root must expose its own identifier type.
/// </summary>
public interface IIdentifier
{
    Guid Value { get; }

    bool IsEmpty => Value == Guid.Empty;
}
