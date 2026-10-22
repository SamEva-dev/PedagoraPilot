using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;
using Xunit;

namespace PedagoraPilot.IntegrationTests;

/// <summary>
/// PostgreSQL integration tests. Set PEDAGORA_TEST_DB to a disposable database.
/// The shared fixture recreates that database before the collection runs.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class JuryConcurrencySprintS05IntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Concurrent_assignments_of_the_same_jury_to_one_session_persist_only_once()
    {
        var organizationId = Guid.NewGuid();
        var sessionId = CertificationExamSessionId.New();
        var authGateUserId = Guid.NewGuid();

        async Task<bool> InsertAssignment()
        {
            await using var db = fixture.CreateContext();
            db.JuryAssignments.Add(JuryAssignment.Create(organizationId, sessionId,
                authGateUserId, "Examiner", "jury"));
            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                // The unique index is the final protection against the concurrent insert.
                return false;
            }
        }

        var result = await Task.WhenAll(InsertAssignment(), InsertAssignment());
        result.Count(x => x).Should().Be(1);
        await using var verify = fixture.CreateContext();
        (await verify.JuryAssignments.CountAsync(x =>
            x.ExamSessionId == sessionId && x.AuthGateUserId == authGateUserId))
            .Should().Be(1);
    }

    [Fact]
    public async Task Same_jury_can_be_assigned_to_two_different_sessions()
    {
        var organizationId = Guid.NewGuid();
        var authGateUserId = Guid.NewGuid();
        var first = CertificationExamSessionId.New();
        var second = CertificationExamSessionId.New();
        await using var db = fixture.CreateContext();
        db.JuryAssignments.Add(JuryAssignment.Create(organizationId, first,
            authGateUserId, "Examiner", "jury"));
        db.JuryAssignments.Add(JuryAssignment.Create(organizationId, second,
            authGateUserId, "Examiner", "jury"));
        await db.SaveChangesAsync();
        (await db.JuryAssignments.CountAsync(x => x.AuthGateUserId == authGateUserId
            && (x.ExamSessionId == first || x.ExamSessionId == second))).Should().Be(2);
    }
}
