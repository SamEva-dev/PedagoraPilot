namespace PedagoraPilot.Application.Abstractions.Security;

public enum ContextualScopeLevel
{
    Organization = 0,
    Site = 1,
    Program = 2,
    Cohort = 3,
    Exam = 4
}

public sealed record ContextualScopeAssignment(
    ContextualScopeLevel Level,
    Guid? SiteId = null,
    Guid? ProgramId = null,
    Guid? CohortId = null,
    Guid? ExamSessionId = null);
