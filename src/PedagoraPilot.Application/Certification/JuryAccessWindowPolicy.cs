using Microsoft.Extensions.Configuration;
using PedagoraPilot.Domain.Certification;

namespace PedagoraPilot.Application.Certification;

/// <summary>Server-side policy shared by jury access checks and DTO projection.</summary>
internal sealed class JuryAccessWindowPolicy
{
    public int LeadDays { get; }
    public int GraceDays { get; }

    public JuryAccessWindowPolicy(IConfiguration configuration)
    {
        LeadDays = ReadDays(configuration, "Certification:JuryAccess:LeadDays", 7);
        GraceDays = ReadDays(configuration, "Certification:JuryAccess:GraceDays", 2);
    }

    private static int ReadDays(IConfiguration config, string key, int fallback)
    {
        var value = config[key];
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (!int.TryParse(value, out var result) || result < 0 || result > 90)
            throw new InvalidOperationException($"Invalid configuration {key}: expected integer between 0 and 90.");
        return result;
    }

    public DateTimeOffset ValidFrom(CertificationExamSession session) => session.StartsAtUtc.AddDays(-LeadDays);
    public DateTimeOffset ValidUntil(CertificationExamSession session) => session.EndsAtUtc.AddDays(GraceDays);
    public bool Allows(CertificationExamSession session, DateTimeOffset nowUtc) =>
        session.Status is (CertificationExamSessionStatus.Planned
            or CertificationExamSessionStatus.InProgress
            or CertificationExamSessionStatus.Completed
            or CertificationExamSessionStatus.Published)
        && nowUtc >= ValidFrom(session)
        && nowUtc <= ValidUntil(session);
}
