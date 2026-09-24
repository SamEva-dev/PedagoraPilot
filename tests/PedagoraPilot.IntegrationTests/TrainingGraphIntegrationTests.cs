using FluentAssertions;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class TrainingGraphIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Person_learner_cohort_enrollment_graph_persists_with_strong_ids()
    {
        await using var db = fixture.CreateContext();
        var cohort = Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), $"QA-{Guid.NewGuid():N}"[..18], "QA Cohort", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 31), 20);
        var person = Person.Create("Sam", "QA", $"qa-{Guid.NewGuid():N}@example.com", null, null);
        var learner = LearnerProfile.Create(person.Id);
        var enrollment = Enrollment.Create(cohort.OrganizationId, learner.Id, cohort.Id, new DateOnly(2026, 9, 1));
        db.Cohorts.Add(cohort);
        db.People.Add(person);
        db.LearnerProfiles.Add(learner);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var stored = await db.Enrollments.FindAsync(enrollment.Id);
        stored.Should().NotBeNull();
        stored!.LearnerProfileId.Should().Be(learner.Id);
        stored.CohortId.Should().Be(cohort.Id);
    }
}
