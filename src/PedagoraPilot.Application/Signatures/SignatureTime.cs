using PedagoraPilot.Application.Common.Errors;

namespace PedagoraPilot.Application.Signatures;

internal static class SignatureTime
{
    internal static TimeZoneInfo ResolveOrganizationZone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ValidationApplicationException("SIGNATURE_TIMEZONE_INVALID");
        try { return TimeZoneInfo.FindSystemTimeZoneById(timezone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ValidationApplicationException("SIGNATURE_TIMEZONE_INVALID");
        }
    }
}
