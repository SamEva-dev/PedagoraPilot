namespace PedagoraPilot.Api.Middleware;

/// <summary>Maps only known PostgreSQL unique constraints to business conflicts.
/// Unknown constraints are deliberately left to the normal error pipeline.</summary>
public static class EnrollmentJuryUniqueConflict
{
    public static bool TryResolve(string? constraintName, out string errorKey)
    {
        switch (constraintName)
        {
            case "IX_jury_assignments_ExamSessionId_AuthGateUserId":
                errorKey = "CERTIFICATION_JURY_ALREADY_ASSIGNED";
                return true;
            case "IX_enrollments_LearnerProfileId_CohortId":
                errorKey = "LEARNER_ALREADY_ENROLLED";
                return true;
            default:
                errorKey = string.Empty;
                return false;
        }
    }
}
