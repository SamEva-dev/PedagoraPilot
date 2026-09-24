using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Certification.Events;

namespace PedagoraPilot.Domain.Certification;
public sealed class CertificationExamSession : AggregateRoot<CertificationExamSessionId>
{
    private CertificationExamSession()
    {
    }

    private CertificationExamSession(CertificationExamSessionId id, Guid organizationId, Guid siteId, CohortId cohortId, CertificationSchemeId schemeId, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? venue) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("CERTIFICATION_ORGANIZATION_REQUIRED");
        if (siteId == Guid.Empty)
            throw new DomainException("CERTIFICATION_SITE_REQUIRED");
        if (cohortId.IsEmpty)
            throw new DomainException("CERTIFICATION_COHORT_REQUIRED");
        if (schemeId.IsEmpty)
            throw new DomainException("CERTIFICATION_SCHEME_REQUIRED");
        if (endsAtUtc <= startsAtUtc)
            throw new DomainException("CERTIFICATION_SESSION_RANGE_INVALID");
        OrganizationId = organizationId;
        SiteId = siteId;
        CohortId = cohortId;
        SchemeId = schemeId;
        Title = Req(title, "CERTIFICATION_SESSION_TITLE_REQUIRED", 240);
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Venue = Opt(venue, 240);
        Status = CertificationExamSessionStatus.Draft;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public CohortId CohortId { get; private set; }
    public CertificationSchemeId SchemeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public string? Venue { get; private set; }
    public CertificationExamSessionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static CertificationExamSession Create(Guid organizationId, Guid siteId, CohortId cohortId, CertificationSchemeId schemeId, string title, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? venue)
    {
        var x = new CertificationExamSession(CertificationExamSessionId.New(), organizationId, siteId, cohortId, schemeId, title, startsAtUtc, endsAtUtc, venue);
        x.RaiseDomainEvent(new CertificationExamSessionCreatedDomainEvent(x.Id, organizationId, cohortId));
        return x;
    }

    public void Plan()
    {
        if (Status != CertificationExamSessionStatus.Draft)
            throw new DomainException("CERTIFICATION_SESSION_LOCKED");
        Status = CertificationExamSessionStatus.Planned;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationExamSessionPlannedDomainEvent(Id, OrganizationId, CohortId));
    }

    public void Start()
    {
        if (Status != CertificationExamSessionStatus.Planned)
            throw new DomainException("CERTIFICATION_SESSION_CANNOT_START");
        Status = CertificationExamSessionStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != CertificationExamSessionStatus.InProgress)
            throw new DomainException("CERTIFICATION_SESSION_CANNOT_COMPLETE");
        Status = CertificationExamSessionStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status != CertificationExamSessionStatus.Completed)
            throw new DomainException("CERTIFICATION_SESSION_CANNOT_PUBLISH");
        Status = CertificationExamSessionStatus.Published;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationResultsPublishedDomainEvent(Id, OrganizationId, CohortId));
    }

    public void Cancel()
    {
        if (Status == CertificationExamSessionStatus.Published)
            throw new DomainException("CERTIFICATION_SESSION_PUBLISHED_LOCKED");
        Status = CertificationExamSessionStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Req(string? v, string k, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(k);
        return x;
    }

    private static string? Opt(string? v, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var x = v.Trim();
        return x.Length <= max ? x : x[..max];
    }
}
