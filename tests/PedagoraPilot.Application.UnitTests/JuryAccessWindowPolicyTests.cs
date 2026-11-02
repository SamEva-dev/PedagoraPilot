using System.Reflection;
using Microsoft.Extensions.Configuration;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class JuryAccessWindowPolicyTests
{
    [Fact]
    public void DraftSession_DoesNotAuthorizeJury_EvenInsideDateWindow()
    {
        var session = NewSession();
        Assert.Equal(CertificationExamSessionStatus.Draft, session.Status);
        Assert.False(Allows(session, session.StartsAtUtc));
    }

    [Fact]
    public void PlannedSession_RequiresWindowAndHonorsInclusiveBoundaries()
    {
        var session = NewSession();
        session.Plan();
        var start = session.StartsAtUtc.AddDays(-7);
        var end = session.EndsAtUtc.AddDays(2);
        Assert.False(Allows(session, start.AddTicks(-1)));
        Assert.True(Allows(session, start));
        Assert.True(Allows(session, end));
        Assert.False(Allows(session, end.AddTicks(1)));
    }

    [Fact]
    public void CancelledSession_ImmediatelyRevokesJuryWindow()
    {
        var session = NewSession();
        session.Plan();
        session.Cancel();
        Assert.False(Allows(session, session.StartsAtUtc));
    }

    private static CertificationExamSession NewSession()
    {
        var start = new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
        return CertificationExamSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            CohortId.New(), CertificationSchemeId.New(), "Examen", start, start.AddHours(8), null);
    }

    // Invoke the internal policy without changing the production assembly public API.
    private static bool Allows(CertificationExamSession session, DateTimeOffset time)
    {
        var assembly = typeof(PedagoraPilot.Application.Certification.GetCertificationExamSessionsQueryHandler).Assembly;
        var type = assembly.GetType("PedagoraPilot.Application.Certification.JuryAccessWindowPolicy", throwOnError: true)!;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Certification:JuryAccess:LeadDays"] = "7",
            ["Certification:JuryAccess:GraceDays"] = "2"
        }).Build();
        var policy = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null, args: new object[] { config }, culture: null)!;
        return (bool)type.GetMethod("Allows")!.Invoke(policy, new object[] { session, time })!;
    }
}
