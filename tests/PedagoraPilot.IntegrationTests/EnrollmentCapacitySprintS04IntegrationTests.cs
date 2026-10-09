using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PedagoraPilot.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class EnrollmentCapacitySprintS04IntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Two_parallel_enrollments_for_the_last_seat_never_exceed_capacity()
    {
        CohortId cohortId;
        LearnerProfileId firstId;
        LearnerProfileId secondId;
        Guid organizationId;
        await using (var setup = fixture.CreateContext())
        {
            var cohort = Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                $"S04-{Guid.NewGuid():N}"[..18], "Last seat", new DateOnly(2026, 9, 1), new DateOnly(2027, 3, 1),
                1, Guid.NewGuid(), "Responsable");
            organizationId = cohort.OrganizationId;
            var first = Person.Create("Test", "One", $"one-{Guid.NewGuid():N}@example.org", null, null);
            var second = Person.Create("Test", "Two", $"two-{Guid.NewGuid():N}@example.org", null, null);
            var learner1 = LearnerProfile.Create(first.Id);
            var learner2 = LearnerProfile.Create(second.Id);
            setup.Cohorts.Add(cohort);
            setup.People.AddRange(first, second);
            setup.LearnerProfiles.AddRange(learner1, learner2);
            await setup.SaveChangesAsync();
            cohortId = cohort.Id;
            firstId = learner1.Id;
            secondId = learner2.Id;
        }

        async Task<bool> TryTakeSeat(LearnerProfileId learnerId)
        {
            await using var db = fixture.CreateContext();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var cohortRepository = new CohortRepository(db);
            var enrollmentRepository = new EnrollmentRepository(db);
            await cohortRepository.LockForEnrollmentCapacityAsync(cohortId);
            var cohort = await cohortRepository.GetByIdAsync(cohortId);
            var count = await enrollmentRepository.CountSeatOccupyingByCohortAsync(cohortId);
            if (count >= cohort!.Capacity)
            {
                await transaction.RollbackAsync();
                return false;
            }
            db.Enrollments.Add(Enrollment.Create(organizationId, learnerId, cohortId, new DateOnly(2026, 9, 1)));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        var results = await Task.WhenAll(TryTakeSeat(firstId), TryTakeSeat(secondId));
        results.Count(x => x).Should().Be(1);
        await using var verification = fixture.CreateContext();
        (await verification.Enrollments.CountAsync(x => x.CohortId == cohortId && x.Status == EnrollmentStatus.Active))
            .Should().Be(1);
    }

    [Fact]
    public async Task Suspended_enrollments_reserve_capacity_but_do_not_count_as_active()
    {
        await using var db = fixture.CreateContext();
        var cohort = Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            $"S04-{Guid.NewGuid():N}"[..18], "Suspended reservation", new DateOnly(2026, 9, 1),
            new DateOnly(2027, 3, 1), 1, Guid.NewGuid(), "Responsable");
        var person = Person.Create("Test", "Suspended", $"suspended-{Guid.NewGuid():N}@example.org", null, null);
        var learner = LearnerProfile.Create(person.Id);
        var enrollment = Enrollment.Create(cohort.OrganizationId, learner.Id, cohort.Id, cohort.StartDate);
        enrollment.ChangeStatus(EnrollmentStatus.Suspended);
        db.Cohorts.Add(cohort);
        db.People.Add(person);
        db.LearnerProfiles.Add(learner);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        var repository = new EnrollmentRepository(db);
        (await repository.CountActiveByCohortAsync(cohort.Id)).Should().Be(0);
        (await repository.CountSeatOccupyingByCohortAsync(cohort.Id)).Should().Be(1);
    }
}
