namespace PedagoraPilot.Domain.Certification;
public enum CertificationSchemeStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum CertificationExamSessionStatus
{
    Draft = 0,
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Published = 4,
    Cancelled = 5
}

public enum CertificationCandidateStatus
{
    Registered = 0,
    Eligible = 1,
    Ineligible = 2,
    InProgress = 3,
    Completed = 4,
    Published = 5
}

public enum CertificationDecision
{
    Pending = 0,
    Obtained = 1,
    Failed = 2,
    Partial = 3,
    Absent = 4
}

public enum CertificationAssessmentOutcome
{
    Pending = 0,
    Passed = 1,
    Failed = 2,
    Absent = 3,
    NotApplicable = 4
}

public enum CertificationStepKind
{
    Practical = 0,
    TechnicalInterview = 1,
    ProfessionalQuestionnaire = 2,
    Presentation = 3,
    ProductionQuestioning = 4,
    FinalInterview = 5,
    Other = 99
}
