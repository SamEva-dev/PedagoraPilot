using System.Text.RegularExpressions;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.ValueObjects;
public sealed partial class SiteCode : ValueObject
{
    private SiteCode(string value) => Value = value;
    public string Value { get; }

    public static SiteCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("SITE_CODE_REQUIRED");
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 32 || !CodeRegex().IsMatch(normalized))
            throw new DomainException("SITE_CODE_INVALID");
        return new SiteCode(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]*$")]
    private static partial Regex CodeRegex();
}
