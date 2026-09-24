namespace PedagoraPilot.Domain.Documents;
public enum DocumentCategory
{
    Administrative,
    Pedagogical,
    Evaluation,
    Course,
    Internship,
    Student,
    Certification,
    Other
}

public enum DocumentVisibility
{
    All,
    Staff,
    Student
}

public enum DocumentOwnerType
{
    Organization,
    Site,
    Program,
    Cohort,
    Enrollment,
    WorkplacePeriod,
    CertificationCandidate,
    None
}

public enum DocumentStatus
{
    Active,
    Deleted
}

public enum DocumentSecurityStatus
{
    Pending,
    Clean,
    Rejected
}
