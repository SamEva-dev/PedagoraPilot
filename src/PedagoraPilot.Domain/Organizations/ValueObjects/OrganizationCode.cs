using System.Text.RegularExpressions;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.ValueObjects;
public sealed partial class OrganizationCode : ValueObject
{
    private OrganizationCode(string value) => Value = value;
    public string Value { get; }

    public static OrganizationCode Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 32 || !ValidCode().IsMatch(normalized))
            throw new DomainException("ORGANIZATION_CODE_INVALID");
        return new OrganizationCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]*[A-Z0-9]$|^[A-Z0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCode();
}
