using PedagoraPilot.Api.Middleware;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class EnrollmentJuryUniqueConflictTests
{
    [Theory]
    [InlineData("IX_jury_assignments_ExamSessionId_AuthGateUserId", "CERTIFICATION_JURY_ALREADY_ASSIGNED")]
    [InlineData("IX_enrollments_LearnerProfileId_CohortId", "LEARNER_ALREADY_ENROLLED")]
    public void Known_unique_constraints_have_business_conflict_keys(string constraint, string expected)
    {
        Assert.True(EnrollmentJuryUniqueConflict.TryResolve(constraint, out var key));
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("IX_other_table_Unique")]
    public void Unknown_constraints_are_not_masked(string? constraint)
    {
        Assert.False(EnrollmentJuryUniqueConflict.TryResolve(constraint, out var key));
        Assert.Equal(string.Empty, key);
    }
}
