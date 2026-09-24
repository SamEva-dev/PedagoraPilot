using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog.ValueObjects;
public sealed class ProgramCode : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private ProgramCode()
    {
    }

    private ProgramCode(string value) => Value = value;
    public static ProgramCode Create(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 64)
            throw new DomainException("PROGRAM_CODE_INVALID");
        return new ProgramCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
